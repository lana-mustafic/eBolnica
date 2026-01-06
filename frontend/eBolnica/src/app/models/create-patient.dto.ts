export interface CreatePatientDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  dateOfBirth?: string;
  gender?: string;
  phoneNumber?: string;
  address?: string;
  bloodType?: string;
  medicalRecordId?: string;
}
