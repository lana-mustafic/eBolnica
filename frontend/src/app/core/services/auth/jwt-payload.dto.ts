import { UserType } from '../../../api-services/auth/auth-api.model';

export interface JwtPayloadDto {
  sub: string;
  email?: string;
  user_type?: string;
  role?: string;
  is_admin?: string;
  is_manager?: string;
  is_employee?: string;
  ver?: string;
  iat?: number;
  exp?: number;
  aud?: string | string[];
  iss?: string;
  [claim: string]: unknown;
}

const EMAIL_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export function resolveJwtEmail(payload: JwtPayloadDto): string {
  const email = payload.email ?? payload[EMAIL_CLAIM];
  return typeof email === 'string' ? email : '';
}

export function resolveUserType(payload: JwtPayloadDto): UserType {
  const roleClaim = payload[ROLE_CLAIM];
  const raw =
    payload.user_type ??
    payload.role ??
    (typeof roleClaim === 'string' ? roleClaim : '') ??
    '';
  if (raw === 'Admin' || raw === 'Doctor' || raw === 'Patient' || raw === 'Pharmacist') {
    return raw;
  }
  if (payload.is_admin === 'true') return 'Admin';
  return 'Patient';
}
