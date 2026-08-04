export interface DoctorProfileDto {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  specialization?: string | null;
  licenseNumber: string;
  birthDate?: string | null;
  address: string;
  email: string;
}

export interface UpdateDoctorProfileCommand {
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  address?: string | null;
  specialization?: string | null;
}

export interface DoctorAssignedPatientDto {
  id: number;
  doctorId: number;
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  gender?: string | null;
  phoneNumber?: string | null;
  address?: string | null;
  bloodType?: string | null;
  recordNumber: string;
}

export interface ListDoctorPatientsRequest {
  firstName?: string;
  lastName?: string;
  gender?: string;
  bloodType?: string;
  birthYear?: number;
  page?: number;
  pageSize?: number;
}

export interface ListDoctorPatientsResponse {
  items: DoctorAssignedPatientDto[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

export interface MonthlyTrendDto {
  year: number;
  month: string;
  count: number;
}

export interface BloodTypeCountDto {
  bloodType?: string | null;
  count: number;
}

export interface DoctorStatsDto {
  totalPatients: number;
  reportsThisMonth: number;
  reportsToday: number;
  avgReportsPerPatient: number;
  monthlyReportTrend: MonthlyTrendDto[];
  bloodTypeDistribution: BloodTypeCountDto[];
}
