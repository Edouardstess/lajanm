import { Body, Controller, Delete, Get, Param, Patch, Post, Query, UseGuards } from '@nestjs/common';
import { CurrentAdmin } from '../admin/decorators/current-admin.decorator';
import { AdminJwtAuthGuard } from '../admin/guards/admin-jwt-auth.guard';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { AddMessageDto } from './dto/add-message.dto';
import { CreateFaqDto } from './dto/create-faq.dto';
import { CreateTicketDto } from './dto/create-ticket.dto';
import { UpdateFaqDto } from './dto/update-faq.dto';
import { UpdateTicketDto } from './dto/update-ticket.dto';
import { SupportSenderType } from './entities/support-message.entity';
import { TicketStatus } from './entities/support-ticket.entity';
import { SupportService } from './support.service';

/**
 * Route order matters: Express matches in registration order, so every
 * literal path ('tickets/me', 'tickets/queue', 'faq/all') is declared
 * before the ':id' route that would otherwise swallow it as an id — the
 * same ordering constraint TopupController's 'history' vs ':id' has.
 */
@Controller('support')
export class SupportController {
  constructor(private readonly supportService: SupportService) {}

  // --- FAQ (customer) ---

  @UseGuards(JwtAuthGuard)
  @Get('faq')
  listFaq() {
    return this.supportService.listPublishedFaqs();
  }

  // --- FAQ (admin) ---

  @UseGuards(AdminJwtAuthGuard)
  @Get('faq/all')
  listAllFaq() {
    return this.supportService.listAllFaqs();
  }

  @UseGuards(AdminJwtAuthGuard)
  @Post('faq')
  createFaq(@CurrentAdmin() admin: { id: string }, @Body() dto: CreateFaqDto) {
    return this.supportService.createFaq(admin.id, dto);
  }

  @UseGuards(AdminJwtAuthGuard)
  @Patch('faq/:id')
  updateFaq(@CurrentAdmin() admin: { id: string }, @Param('id') faqId: string, @Body() dto: UpdateFaqDto) {
    return this.supportService.updateFaq(faqId, admin.id, dto);
  }

  @UseGuards(AdminJwtAuthGuard)
  @Delete('faq/:id')
  deleteFaq(@CurrentAdmin() admin: { id: string }, @Param('id') faqId: string) {
    return this.supportService.deleteFaq(faqId, admin.id);
  }

  // --- Tickets (customer) ---

  @UseGuards(JwtAuthGuard)
  @Post('tickets')
  createTicket(@CurrentUser() user: { id: string }, @Body() dto: CreateTicketDto) {
    return this.supportService.createTicket(user.id, dto);
  }

  @UseGuards(JwtAuthGuard)
  @Get('tickets/me')
  listMyTickets(@CurrentUser() user: { id: string }) {
    return this.supportService.listMyTickets(user.id);
  }

  // --- Tickets (admin) ---

  @UseGuards(AdminJwtAuthGuard)
  @Get('tickets/queue')
  listQueue(@Query('status') status?: TicketStatus) {
    return this.supportService.listAllTickets(status);
  }

  /**
   * Admin thread view. Deliberately a distinct path from the customer's
   * 'tickets/:id' rather than the same route behind a different guard:
   * two handlers on one method+path would leave whichever registered
   * second as dead code.
   */
  @UseGuards(AdminJwtAuthGuard)
  @Get('tickets/queue/:id')
  getTicketAsAdmin(@Param('id') ticketId: string) {
    return this.supportService.getTicket(ticketId, null);
  }

  @UseGuards(AdminJwtAuthGuard)
  @Post('tickets/:id/reply')
  reply(@CurrentAdmin() admin: { id: string }, @Param('id') ticketId: string, @Body() dto: AddMessageDto) {
    return this.supportService.addMessage(ticketId, admin.id, SupportSenderType.ADMIN, dto);
  }

  @UseGuards(AdminJwtAuthGuard)
  @Patch('tickets/:id')
  updateTicket(
    @CurrentAdmin() admin: { id: string },
    @Param('id') ticketId: string,
    @Body() dto: UpdateTicketDto,
  ) {
    return this.supportService.updateTicket(ticketId, admin.id, dto);
  }

  // --- Tickets (customer, dynamic paths last) ---

  @UseGuards(JwtAuthGuard)
  @Post('tickets/:id/messages')
  addMessage(@CurrentUser() user: { id: string }, @Param('id') ticketId: string, @Body() dto: AddMessageDto) {
    return this.supportService.addMessage(ticketId, user.id, SupportSenderType.USER, dto);
  }

  @UseGuards(JwtAuthGuard)
  @Get('tickets/:id')
  getTicket(@CurrentUser() user: { id: string }, @Param('id') ticketId: string) {
    return this.supportService.getTicket(ticketId, user.id);
  }
}
