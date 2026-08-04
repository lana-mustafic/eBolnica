import { UserType } from '../../../api-services/auth/auth-api.model';

export interface JwtPayloadDto {
  sub: string;
  email: string;
  user_type?: string;
  role?: string;
  is_admin: string;
  is_manager: string;
  is_employee: string;
  ver: string;
  iat: number;
  exp: number;
  aud: string;
  iss: string;
}

export function resolveUserType(payload: JwtPayloadDto): UserType {
  const raw = payload.user_type ?? payload.role ?? '';
  if (raw === 'Admin' || raw === 'Doctor' || raw === 'Patient' || raw === 'Pharmacist') {
    return raw;
  }
  if (payload.is_admin === 'true') return 'Admin';
  return 'Patient';
}
