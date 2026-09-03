import { ForbiddenException, Injectable, NotFoundException } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { PaginationQueryDto, toFindPaging } from '../../common/dto/pagination-query.dto';
import { AuditService } from '../audit/audit.service';
import { AddMessageDto } from './dto/add-message.dto';
import { CreateFaqDto } from './dto/create-faq.dto';
import { CreateTicketDto } from './dto/create-ticket.dto';
import { UpdateFaqDto } from './dto/update-faq.dto';
import { UpdateTicketDto } from './dto/update-ticket.dto';
import { FaqEntry } from './entities/faq-entry.entity';
import { SupportMessage, SupportSenderType } from './entities/support-message.entity';
import { SupportTicket, TicketCategory, TicketStatus } from './entities/support-ticket.entity';

/** See getTicket: a safety bound on a thread, not a paging window. */
const MAX_THREAD_MESSAGES = 500;

export interface TicketWithThread {
  ticket: SupportTicket;
  messages: SupportMessage[];
}

@Injectable()
export class SupportService {
  constructor(
    @InjectRepository(SupportTicket) private readonly tickets: Repository<SupportTicket>,
    @InjectRepository(SupportMessage) private readonly messages: Repository<SupportMessage>,
    @InjectRepository(FaqEntry) private readonly faqs: Repository<FaqEntry>,
    private readonly auditService: AuditService,
  ) {}

  // --- Tickets: customer side ---

  /**
   * Opens a ticket and stores the customer's opening text as the thread's
   * first message, so a ticket is never an empty shell — every ticket has
   * at least one message and the thread reads chronologically from the top.
   */
  async createTicket(userId: string, dto: CreateTicketDto): Promise<TicketWithThread> {
    const ticket = await this.tickets.save(
      this.tickets.create({
        userId,
        subject: dto.subject,
        category: dto.category ?? TicketCategory.GENERAL,
        status: TicketStatus.OPEN,
      }),
    );

    const message = await this.messages.save(
      this.messages.create({
        ticketId: ticket.id,
        senderId: userId,
        senderType: SupportSenderType.USER,
        body: dto.message,
      }),
    );

    await this.auditService.record({
      action: 'support.ticket_created',
      actorId: userId,
      actorType: 'user',
      targetId: ticket.id,
      metadata: { category: ticket.category },
    });

    return { ticket, messages: [message] };
  }

  async listMyTickets(userId: string, paging?: PaginationQueryDto): Promise<SupportTicket[]> {
    return this.tickets.find({
      where: { userId },
      order: { updatedAt: 'DESC' },
      ...toFindPaging(paging),
    });
  }

  /**
   * Loads a ticket the caller is allowed to read. `userId` is null for an
   * admin (who may read any ticket); for a customer it must match the
   * ticket's owner. A mismatch is a 404, not a 403 — replying "this ticket
   * exists but isn't yours" would let anyone probe for valid ticket ids.
   */
  async getTicket(ticketId: string, userId: string | null): Promise<TicketWithThread> {
    const ticket = await this.tickets.findOneBy({ id: ticketId });
    if (!ticket) throw new NotFoundException('Ticket not found');
    if (userId !== null && ticket.userId !== userId) throw new NotFoundException('Ticket not found');

    // Bounded like every other list, but capped high and read newest-first
    // before being flipped back into reading order: a naive ASC + take
    // would truncate the *newest* messages, which is exactly the part of a
    // support thread nobody can afford to lose. No real thread approaches
    // this cap; it exists to stop a pathological one from being unbounded.
    const newestFirst = await this.messages.find({
      where: { ticketId },
      order: { createdAt: 'DESC' },
      take: MAX_THREAD_MESSAGES,
    });

    return { ticket, messages: newestFirst.reverse() };
  }

  /**
   * Appends a message to a ticket's thread, from either side of the
   * conversation. A customer may only post to their own ticket (enforced
   * via getTicket) and may not post to a closed one — reopening is an
   * operator decision, so the customer opens a new ticket instead. An
   * admin may always post: replying is exactly how a closed ticket gets a
   * final word.
   */
  async addMessage(
    ticketId: string,
    senderId: string,
    senderType: SupportSenderType,
    dto: AddMessageDto,
  ): Promise<SupportMessage> {
    const isAdmin = senderType === SupportSenderType.ADMIN;
    const { ticket } = await this.getTicket(ticketId, isAdmin ? null : senderId);

    if (!isAdmin && ticket.status === TicketStatus.CLOSED) {
      throw new ForbiddenException('This ticket is closed — please open a new one.');
    }

    const message = await this.messages.save(
      this.messages.create({ ticketId, senderId, senderType, body: dto.body }),
    );

    // An admin reply moves an untouched ticket into in_progress, and a
    // customer reply on a resolved ticket reopens it: in both cases the
    // status should reflect that the thread is live again without an
    // operator having to remember to flip it by hand.
    if (isAdmin && ticket.status === TicketStatus.OPEN) {
      ticket.status = TicketStatus.IN_PROGRESS;
      await this.tickets.save(ticket);
    } else if (!isAdmin && ticket.status === TicketStatus.RESOLVED) {
      ticket.status = TicketStatus.IN_PROGRESS;
      await this.tickets.save(ticket);
    } else {
      // Nothing else changed on the ticket row, but the thread did — touch
      // updatedAt so the operator queue (sorted by it) surfaces this
      // ticket as recently active.
      await this.tickets.update({ id: ticketId }, { updatedAt: new Date() });
    }

    await this.auditService.record({
      action: 'support.message_added',
      actorId: senderId,
      actorType: isAdmin ? 'admin' : 'user',
      targetId: ticketId,
      metadata: { senderType },
    });

    return message;
  }

  // --- Tickets: admin side ---

  async listAllTickets(status?: TicketStatus, paging?: PaginationQueryDto): Promise<SupportTicket[]> {
    return this.tickets.find({
      where: status ? { status } : {},
      order: { updatedAt: 'DESC' },
      ...toFindPaging(paging),
    });
  }

  async updateTicket(ticketId: string, adminId: string, dto: UpdateTicketDto): Promise<SupportTicket> {
    const ticket = await this.tickets.findOneBy({ id: ticketId });
    if (!ticket) throw new NotFoundException('Ticket not found');

    if (dto.status !== undefined) ticket.status = dto.status;
    if (dto.assignedTo !== undefined) ticket.assignedTo = dto.assignedTo;
    await this.tickets.save(ticket);

    await this.auditService.record({
      action: 'support.ticket_updated',
      actorId: adminId,
      actorType: 'admin',
      targetId: ticket.id,
      metadata: { status: ticket.status },
    });

    return ticket;
  }

  // --- FAQ ---

  /** What the mobile app's help section shows: published entries only. */
  async listPublishedFaqs(paging?: PaginationQueryDto): Promise<FaqEntry[]> {
    return this.faqs.find({
      where: { isPublished: true },
      order: { category: 'ASC', sortOrder: 'ASC' },
      ...toFindPaging(paging),
    });
  }

  /** The back-office view: drafts included, so an operator can stage content. */
  async listAllFaqs(paging?: PaginationQueryDto): Promise<FaqEntry[]> {
    return this.faqs.find({
      order: { category: 'ASC', sortOrder: 'ASC' },
      ...toFindPaging(paging),
    });
  }

  async createFaq(adminId: string, dto: CreateFaqDto): Promise<FaqEntry> {
    const faq = await this.faqs.save(
      this.faqs.create({
        category: dto.category,
        question: dto.question,
        answer: dto.answer,
        sortOrder: dto.sortOrder ?? 0,
        isPublished: dto.isPublished ?? true,
      }),
    );

    await this.auditService.record({
      action: 'support.faq_created',
      actorId: adminId,
      actorType: 'admin',
      targetId: faq.id,
    });

    return faq;
  }

  async updateFaq(faqId: string, adminId: string, dto: UpdateFaqDto): Promise<FaqEntry> {
    const faq = await this.faqs.findOneBy({ id: faqId });
    if (!faq) throw new NotFoundException('FAQ entry not found');

    if (dto.category !== undefined) faq.category = dto.category;
    if (dto.question !== undefined) faq.question = dto.question;
    if (dto.answer !== undefined) faq.answer = dto.answer;
    if (dto.sortOrder !== undefined) faq.sortOrder = dto.sortOrder;
    if (dto.isPublished !== undefined) faq.isPublished = dto.isPublished;
    await this.faqs.save(faq);

    await this.auditService.record({
      action: 'support.faq_updated',
      actorId: adminId,
      actorType: 'admin',
      targetId: faq.id,
    });

    return faq;
  }

  async deleteFaq(faqId: string, adminId: string): Promise<{ deleted: true }> {
    const faq = await this.faqs.findOneBy({ id: faqId });
    if (!faq) throw new NotFoundException('FAQ entry not found');

    await this.faqs.delete({ id: faqId });

    await this.auditService.record({
      action: 'support.faq_deleted',
      actorId: adminId,
      actorType: 'admin',
      targetId: faqId,
      metadata: { question: faq.question },
    });

    return { deleted: true };
  }
}
