export interface MedicalRecordDto{
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
}