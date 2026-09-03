import { randomUUID } from 'crypto';
import { ForbiddenException, NotFoundException } from '@nestjs/common';
import { AuditService } from '../audit/audit.service';
import { FaqEntry } from './entities/faq-entry.entity';
import { SupportMessage, SupportSenderType } from './entities/support-message.entity';
import { SupportTicket, TicketCategory, TicketStatus } from './entities/support-ticket.entity';
import { SupportService } from './support.service';

function createTicketsRepo() {
  const rows: SupportTicket[] = [];
  return {
    rows,
    create: jest.fn(
      (data: Partial<SupportTicket>) =>
        ({
          id: randomUUID(),
          category: TicketCategory.GENERAL,
          status: TicketStatus.OPEN,
          assignedTo: null,
          createdAt: new Date(),
          updatedAt: new Date(),
          ...data,
        }) as SupportTicket,
    ),
    save: jest.fn(async (entity: SupportTicket) => {
      const i = rows.findIndex((r) => r.id === entity.id);
      if (i === -1) rows.push(entity);
      else rows[i] = entity;
      return entity;
    }),
    findOneBy: jest.fn(
      async (where: Partial<SupportTicket>) => rows.find((r) => r.id === where.id) ?? null,
    ),
    find: jest.fn(async (options?: { where?: Partial<SupportTicket> }) => {
      const where = options?.where ?? {};
      return rows.filter(
        (r) =>
          (where.userId === undefined || r.userId === where.userId) &&
          (where.status === undefined || r.status === where.status),
      );
    }),
    update: jest.fn(async (where: Partial<SupportTicket>, patch: Partial<SupportTicket>) => {
      const row = rows.find((r) => r.id === where.id);
      if (row) Object.assign(row, patch);
      return { affected: row ? 1 : 0 };
    }),
  };
}

function createMessagesRepo() {
  const rows: SupportMessage[] = [];
  return {
    rows,
    create: jest.fn(
      (data: Partial<SupportMessage>) =>
        ({ id: randomUUID(), createdAt: new Date(), ...data }) as SupportMessage,
    ),
    save: jest.fn(async (entity: SupportMessage) => {
      rows.push(entity);
      return entity;
    }),
    // Honours `order` and `take` rather than always returning insertion
    // order: getTicket reads the thread newest-first and flips it back, so
    // a fake that ignored `order` would happily pass while production
    // returned the thread reversed. Insertion index breaks ties, since
    // several messages in one test can share a millisecond timestamp.
    find: jest.fn(
      async (options?: {
        where?: Partial<SupportMessage>;
        order?: { createdAt?: 'ASC' | 'DESC' };
        take?: number;
      }) => {
        const matching = rows
          .map((row, index) => ({ row, index }))
          .filter(({ row }) => row.ticketId === options?.where?.ticketId);

        const direction = options?.order?.createdAt === 'DESC' ? -1 : 1;
        matching.sort(
          (a, b) =>
            direction * (a.row.createdAt.getTime() - b.row.createdAt.getTime() || a.index - b.index),
        );

        const ordered = matching.map(({ row }) => row);
        return options?.take === undefined ? ordered : ordered.slice(0, options.take);
      },
    ),
  };
}

function createFaqsRepo() {
  const rows: FaqEntry[] = [];
  return {
    rows,
    create: jest.fn(
      (data: Partial<FaqEntry>) =>
        ({
          id: randomUUID(),
          sortOrder: 0,
          isPublished: true,
          createdAt: new Date(),
          updatedAt: new Date(),
          ...data,
        }) as FaqEntry,
    ),
    save: jest.fn(async (entity: FaqEntry) => {
      const i = rows.findIndex((r) => r.id === entity.id);
      if (i === -1) rows.push(entity);
      else rows[i] = entity;
      return entity;
    }),
    findOneBy: jest.fn(async (where: Partial<FaqEntry>) => rows.find((r) => r.id === where.id) ?? null),
    find: jest.fn(async (options?: { where?: Partial<FaqEntry> }) => {
      const where = options?.where ?? {};
      return rows.filter((r) => where.isPublished === undefined || r.isPublished === where.isPublished);
    }),
    delete: jest.fn(async (where: Partial<FaqEntry>) => {
      const i = rows.findIndex((r) => r.id === where.id);
      if (i !== -1) rows.splice(i, 1);
      return { affected: i === -1 ? 0 : 1 };
    }),
  };
}

function buildService() {
  const tickets = createTicketsRepo();
  const messages = createMessagesRepo();
  const faqs = createFaqsRepo();
  const auditService = { record: jest.fn().mockResolvedValue(undefined) } as unknown as AuditService;

  const service = new SupportService(
    tickets as unknown as never,
    messages as unknown as never,
    faqs as unknown as never,
    auditService,
  );

  return { service, tickets, messages, faqs, auditService };
}

describe('SupportService tickets', () => {
  it('opens a ticket with the customer text as the thread’s first message', async () => {
    const { service, tickets, messages } = buildService();

    const { ticket, messages: thread } = await service.createTicket('user-1', {
      subject: 'Transfer stuck',
      category: TicketCategory.TRANSACTION,
      message: 'My transfer has been pending for two days.',
    });

    expect(tickets.rows).toHaveLength(1);
    expect(ticket.status).toBe(TicketStatus.OPEN);
    expect(ticket.category).toBe(TicketCategory.TRANSACTION);
    expect(thread).toHaveLength(1);
    expect(messages.rows[0].senderType).toBe(SupportSenderType.USER);
    expect(messages.rows[0].body).toBe('My transfer has been pending for two days.');
  });

  it('defaults an unspecified category to general', async () => {
    const { service } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'Hello', message: 'Hi' });
    expect(ticket.category).toBe(TicketCategory.GENERAL);
  });

  it('returns the full thread in chronological order to the ticket owner', async () => {
    const { service } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'S', message: 'first' });
    await service.addMessage(ticket.id, 'admin-1', SupportSenderType.ADMIN, { body: 'second' });
    await service.addMessage(ticket.id, 'user-1', SupportSenderType.USER, { body: 'third' });

    const { messages } = await service.getTicket(ticket.id, 'user-1');
    expect(messages.map((m) => m.body)).toEqual(['first', 'second', 'third']);
  });

  // Ownership is enforced as a 404 rather than a 403 on purpose: a 403 would
  // confirm the ticket id exists, letting anyone enumerate valid ids.
  it('hides another customer’s ticket behind a 404', async () => {
    const { service } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'S', message: 'm' });

    await expect(service.getTicket(ticket.id, 'user-2')).rejects.toBeInstanceOf(NotFoundException);
  });

  it('lets an admin read any ticket', async () => {
    const { service } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'S', message: 'm' });

    const result = await service.getTicket(ticket.id, null);
    expect(result.ticket.id).toBe(ticket.id);
  });

  it('rejects a customer message on another customer’s ticket', async () => {
    const { service, messages } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'S', message: 'm' });

    await expect(
      service.addMessage(ticket.id, 'user-2', SupportSenderType.USER, { body: 'sneaky' }),
    ).rejects.toBeInstanceOf(NotFoundException);
    expect(messages.rows).toHaveLength(1);
  });

  it('moves an open ticket to in_progress when an admin replies', async () => {
    const { service } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'S', message: 'm' });
    expect(ticket.status).toBe(TicketStatus.OPEN);

    await service.addMessage(ticket.id, 'admin-1', SupportSenderType.ADMIN, { body: 'Looking into it' });

    const { ticket: after } = await service.getTicket(ticket.id, null);
    expect(after.status).toBe(TicketStatus.IN_PROGRESS);
  });

  it('reopens a resolved ticket when the customer replies again', async () => {
    const { service } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'S', message: 'm' });
    await service.updateTicket(ticket.id, 'admin-1', { status: TicketStatus.RESOLVED });

    await service.addMessage(ticket.id, 'user-1', SupportSenderType.USER, { body: 'Still broken' });

    const { ticket: after } = await service.getTicket(ticket.id, null);
    expect(after.status).toBe(TicketStatus.IN_PROGRESS);
  });

  it('refuses a customer message on a closed ticket but still allows an admin reply', async () => {
    const { service } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'S', message: 'm' });
    await service.updateTicket(ticket.id, 'admin-1', { status: TicketStatus.CLOSED });

    await expect(
      service.addMessage(ticket.id, 'user-1', SupportSenderType.USER, { body: 'one more thing' }),
    ).rejects.toBeInstanceOf(ForbiddenException);

    const adminReply = await service.addMessage(ticket.id, 'admin-1', SupportSenderType.ADMIN, {
      body: 'Closing note',
    });
    expect(adminReply.body).toBe('Closing note');

    // The admin's reply must not resurrect a deliberately closed ticket.
    const { ticket: after } = await service.getTicket(ticket.id, null);
    expect(after.status).toBe(TicketStatus.CLOSED);
  });

  it('lists only the calling customer’s tickets', async () => {
    const { service } = buildService();
    await service.createTicket('user-1', { subject: 'Mine', message: 'm' });
    await service.createTicket('user-2', { subject: 'Theirs', message: 'm' });

    const mine = await service.listMyTickets('user-1');
    expect(mine.map((t) => t.subject)).toEqual(['Mine']);
  });

  it('filters the operator queue by status when asked', async () => {
    const { service } = buildService();
    const { ticket: a } = await service.createTicket('user-1', { subject: 'A', message: 'm' });
    await service.createTicket('user-2', { subject: 'B', message: 'm' });
    await service.updateTicket(a.id, 'admin-1', { status: TicketStatus.RESOLVED });

    expect(await service.listAllTickets()).toHaveLength(2);
    const resolved = await service.listAllTickets(TicketStatus.RESOLVED);
    expect(resolved.map((t) => t.subject)).toEqual(['A']);
  });

  it('404s on a ticket that does not exist', async () => {
    const { service } = buildService();
    await expect(service.getTicket(randomUUID(), null)).rejects.toBeInstanceOf(NotFoundException);
    await expect(
      service.updateTicket(randomUUID(), 'admin-1', { status: TicketStatus.CLOSED }),
    ).rejects.toBeInstanceOf(NotFoundException);
  });

  it('records an audit entry for a ticket creation and for each message', async () => {
    const { service, auditService } = buildService();
    const { ticket } = await service.createTicket('user-1', { subject: 'S', message: 'm' });
    await service.addMessage(ticket.id, 'admin-1', SupportSenderType.ADMIN, { body: 'reply' });

    const record = auditService.record as jest.Mock;
    expect(record.mock.calls.map((c: [{ action: string }]) => c[0].action)).toEqual([
      'support.ticket_created',
      'support.message_added',
    ]);
  });
});

describe('SupportService FAQ', () => {
  it('shows only published entries to customers but all of them to operators', async () => {
    const { service } = buildService();
    await service.createFaq('admin-1', { category: 'wallet', question: 'Live?', answer: 'Yes' });
    await service.createFaq('admin-1', {
      category: 'wallet',
      question: 'Draft?',
      answer: 'Not yet',
      isPublished: false,
    });

    const published = await service.listPublishedFaqs();
    expect(published.map((f) => f.question)).toEqual(['Live?']);
    expect(await service.listAllFaqs()).toHaveLength(2);
  });

  it('updates only the fields provided', async () => {
    const { service } = buildService();
    const faq = await service.createFaq('admin-1', {
      category: 'wallet',
      question: 'Q',
      answer: 'A',
      sortOrder: 3,
    });

    const updated = await service.updateFaq(faq.id, 'admin-1', { answer: 'Better A' });
    expect(updated.answer).toBe('Better A');
    expect(updated.question).toBe('Q');
    expect(updated.sortOrder).toBe(3);
  });

  it('unpublishes an entry without deleting it', async () => {
    const { service, faqs } = buildService();
    const faq = await service.createFaq('admin-1', { category: 'c', question: 'Q', answer: 'A' });

    await service.updateFaq(faq.id, 'admin-1', { isPublished: false });

    expect(faqs.rows).toHaveLength(1);
    expect(await service.listPublishedFaqs()).toHaveLength(0);
  });

  it('deletes an entry and 404s on a missing one', async () => {
    const { service, faqs } = buildService();
    const faq = await service.createFaq('admin-1', { category: 'c', question: 'Q', answer: 'A' });

    expect(await service.deleteFaq(faq.id, 'admin-1')).toEqual({ deleted: true });
    expect(faqs.rows).toHaveLength(0);

    await expect(service.deleteFaq(randomUUID(), 'admin-1')).rejects.toBeInstanceOf(NotFoundException);
    await expect(
      service.updateFaq(randomUUID(), 'admin-1', { answer: 'x' }),
    ).rejects.toBeInstanceOf(NotFoundException);
  });
});
