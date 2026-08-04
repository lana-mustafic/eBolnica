export interface PatientProfileDto {

  id: number;

  firstName: string;

  lastName: string;

  email: string;

  dateOfBirth?: string | null;

  gender?: string | null;

  phoneNumber?: string | null;

  address?: string | null;

  bloodType?: string | null;

  registrationStatus: string;

  isAdmitted?: boolean | null;

  recordNumber?: string | null;

  doctorFirstName?: string | null;

  doctorLastName?: string | null;

  doctorSpecialization?: string | null;

}


