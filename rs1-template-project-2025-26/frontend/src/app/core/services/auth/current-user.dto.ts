import { UserType } from '../../../api-services/auth/auth-api.model';

export interface CurrentUserDto {
  userId: number;
  email: string;
  userType: UserType;
  isAdmin: boolean;
  isManager: boolean;
  isEmployee: boolean;
  tokenVersion: number;
}
