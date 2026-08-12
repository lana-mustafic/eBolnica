export interface MedicalReportItemDto {
  id: number;
  doctorId: number;
  createdAt: string;
  diagnosis?: string | null;
  therapy?: string | null;
  symptoms?: string | null;
  description?: string | null;
}

export interface MedicalRecordDto {
  id: number;
  patientId: number;
  recordNumber: string;
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  gender?: string | null;
  phoneNumber?: string | null;
  address?: string | null;
  isAdmitted?: boolean | null;
  bloodType?: string | null;
  email: string;
  reports: MedicalReportItemDto[];
}

export interface CreateMedicalReportCommand {
  medicalRecordId: number;
  symptoms: string;
  diagnosis: string;
  therapy: string;
  description?: string | null;
}
