export interface MedicalRecord{
    id: number, 
    patientId: number,
    recordNumber: string
    firstName: string;
    lastName: string;
    dateOfBirth?: Date;
    gender?: string;
    phoneNumber?: string;
    address?: string;
    bloodType?: string;
    isAdmitted?: boolean;
    email:string;
    reports: MedicalReport[];
}

export interface MedicalReport{
  doctorId: number;
  createdAt: string;
  diagnosis: string;
  therapy: string;
  symptoms: string;
}

export interface newMedicalReport{
    medicalRecordId: number,
    description: string,
    diagnosis: string,
    therapy: string,
    symptoms: string
}