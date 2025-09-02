export interface UserOverview {
  appUserId: string;
  firstName: string;
  lastName: string;
  email: string;
  userType: string;
  registrationStatus?: string; 
  licenseNumber?: string;       
}