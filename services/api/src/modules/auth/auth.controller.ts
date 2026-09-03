import { Body, Controller, Delete, Get, Param, Patch, Post, UseGuards } from '@nestjs/common';
import { AuthService } from './auth.service';
import { ChangePinDto } from './dto/change-pin.dto';
import { LoginDto } from './dto/login.dto';
import { RegisterDto } from './dto/register.dto';
import { UpdateProfileDto } from './dto/update-profile.dto';
import { CurrentUser } from './decorators/current-user.decorator';
import { JwtAuthGuard } from './guards/jwt-auth.guard';

@Controller('auth')
export class AuthController {
  constructor(private readonly authService: AuthService) {}

  @Post('register')
  register(@Body() dto: RegisterDto) {
    return this.authService.register(dto);
  }

  @Post('login')
  login(@Body() dto: LoginDto) {
    return this.authService.login(dto);
  }

  @UseGuards(JwtAuthGuard)
  @Patch('pin')
  changePin(@CurrentUser() user: { id: string }, @Body() dto: ChangePinDto) {
    return this.authService.changePin(user.id, dto);
  }

  @UseGuards(JwtAuthGuard)
  @Patch('profile')
  updateProfile(@CurrentUser() user: { id: string }, @Body() dto: UpdateProfileDto) {
    return this.authService.updateProfile(user.id, dto);
  }

  @UseGuards(JwtAuthGuard)
  @Get('devices')
  listDevices(@CurrentUser() user: { id: string }) {
    return this.authService.listDevices(user.id);
  }

  @UseGuards(JwtAuthGuard)
  @Delete('devices/:id')
  revokeDevice(@CurrentUser() user: { id: string }, @Param('id') deviceSessionId: string) {
    return this.authService.revokeDevice(user.id, deviceSessionId);
  }
}
