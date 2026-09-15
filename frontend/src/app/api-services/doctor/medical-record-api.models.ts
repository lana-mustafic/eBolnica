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
  appointments?: AppointmentItemDto[];
  hospitalizations?: HospitalizationItemDto[];
  diagnoses?: ClinicalDiagnosisItemDto[];
  allergies?: PatientAllergyItemDto[];
}

export interface AppointmentItemDto {
  id: number;
  scheduledAtUtc: string;
  durationMinutes: number;
  reason: string;
  status: string;
  notes?: string | null;
}

export interface HospitalizationItemDto {
  id: number;
  admittedAtUtc: string;
  dischargedAtUtc?: string | null;
  ward: string;
  roomNumber?: string | null;
  admissionReason: string;
  status: string;
}

export interface ClinicalDiagnosisItemDto {
  id: number;
  code?: string | null;
  name: string;
  description?: string | null;
  diagnosedAtUtc: string;
  medicalReportId?: number | null;
}

export interface PatientAllergyItemDto {
  id: number;
  allergen: string;
  severity: string;
  reaction?: string | null;
}

export interface CreateMedicalReportCommand {
  medicalRecordId: number;
  symptoms: string;
  diagnosis: string;
  therapy: string;
  description?: string | null;
}
