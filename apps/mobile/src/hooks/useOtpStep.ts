import { useState } from 'react';
import { ApiError } from '../api/client';
import { OtpPurpose, requestOtp } from '../api/security';

/**
 * Shared "does this action need an OTP" flow for TransferScreen and
 * PayoutScreen: the server is the source of truth on whether an amount
 * crosses the OTP threshold (see SecurityService.enforceOtpIfRequired) —
 * the client doesn't duplicate that threshold, it just reacts to the
 * error and asks for a code.
 */
export function useOtpStep(purpose: OtpPurpose) {
  const [otpRequestId, setOtpRequestId] = useState<string | null>(null);
  const [otpCode, setOtpCode] = useState('');
  const [needsOtp, setNeedsOtp] = useState(false);
  const [requesting, setRequesting] = useState(false);

  const isOtpRequiredError = (err: unknown): boolean =>
    err instanceof ApiError && err.status === 400 && /otp/i.test(err.message);

  const beginOtpFlow = async () => {
    setRequesting(true);
    try {
      const res = await requestOtp(purpose);
      setOtpRequestId(res.otpRequestId);
      setNeedsOtp(true);
    } finally {
      setRequesting(false);
    }
  };

  const reset = () => {
    setNeedsOtp(false);
    setOtpRequestId(null);
    setOtpCode('');
  };

  const otpPayload = otpRequestId && otpCode ? { otpRequestId, otpCode } : undefined;

  return { needsOtp, otpCode, setOtpCode, requesting, isOtpRequiredError, beginOtpFlow, otpPayload, reset };
}
