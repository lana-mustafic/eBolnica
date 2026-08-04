// === COMMANDS (WRITE) ===

export type UserType = 'Admin' | 'Doctor' | 'Patient' | 'Pharmacist';

export interface LoginCommand {
  email: string;
  password: string;
  fingerprint?: string | null;
}

export interface LoginCommandDto {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  accessExpiresAtUtc?: string;
  refreshExpiresAtUtc?: string;
}

export interface RefreshTokenCommand {
  refreshToken: string;
  fingerprint?: string | null;
}

export interface RefreshTokenCommandDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
}

export interface LogoutCommand {
  refreshToken: string;
}

export interface RegisterPatientCommand {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  dateOfBirth?: string | null;
  gender?: string | null;
}

export interface RegisterDoctorCommand {
  firstName: string;
  lastName: string;
  licenseNumber: string;
  email: string;
  password: string;
  confirmPassword: string;
  dateOfBirth?: string | null;
  gender?: string | null;
}

export interface RegisterResponseDto {
  message: string;
}
