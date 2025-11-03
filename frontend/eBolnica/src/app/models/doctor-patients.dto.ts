export interface DoctorAssignedPatientDto {
  id: number;
  firstName: string;
  lastName: string;
  doctorId: number;
  dateOfBirth?: string;
  gender?: string;
  phoneNumber?: string;
  address?: string;
  bloodType?: string;
  medicalRecordId?: string;
  isAdmitted?: boolean;
}