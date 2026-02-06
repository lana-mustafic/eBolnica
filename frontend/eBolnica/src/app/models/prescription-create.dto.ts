export interface PrescriptionItemCreateDto {
  medicationId: number;
  quantity: number;
  instructions?: string;
}

export interface PrescriptionCreateDto {
  medicalReportId: number;
  patientId: number;
  doctorId: number;
  prescriptionItems: PrescriptionItemCreateDto[];
  notes?: string;
}
