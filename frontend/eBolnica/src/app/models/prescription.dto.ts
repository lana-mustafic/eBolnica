import { PatientDataDto } from './patient-data.dto';
import { DoctorDataDto } from './doctor-data.dto';
import { PharmacistDataDto } from './pharmacist-data.dto';
import { PrescriptionItemDto } from './prescription-item.dto';

export interface PrescriptionDto {
  id: number;
  prescriptionNumber: string;
  medicalReportId: number;
  patientId: number;
  patient?: PatientDataDto;
  doctorId: number;
  doctor?: DoctorDataDto;
  pharmacistId?: number;
  pharmacist?: PharmacistDataDto;
  status: string;
  prescribedDate: string;
  dispensedDate?: string;
  totalAmount: number;
  notes?: string;
  createdAt: string;
  updatedAt?: string;
  prescriptionItems: PrescriptionItemDto[];
}
