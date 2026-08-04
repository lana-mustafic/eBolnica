export interface UserOverviewDto {
  appUserId: number;
  firstName: string;
  lastName: string;
  email: string;
  userType: string;
  registrationStatus?: string | null;
  licenseNumber?: string | null;
  doctorProfileId?: number | null;
}

export interface ListUsersResponse {
  totalCount: number;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: string;
  users: UserOverviewDto[];
}

export interface ListUsersRequest {
  page?: number;
  pageSize?: number;
  userType?: string | null;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface CreateUserCommand {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  userType: string;
  doctorId?: number | null;
  licenseNumber?: string | null;
}

export interface UpdateUserCommand {
  firstName: string;
  lastName: string;
  email: string;
}

export interface UpdateRegistrationStatusCommand {
  registrationStatus: string;
}

export interface MessageResponse {
  message: string;
}
