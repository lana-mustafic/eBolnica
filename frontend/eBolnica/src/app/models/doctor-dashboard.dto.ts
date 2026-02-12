export interface DashboardStats {
  totalPatients: number;
  reportsThisMonth: number;
  reportsToday: number;
  avgReportsPerPatient: number;
  monthlyReportTrend: MonthlyTrend[];
  bloodTypeDistribution: BloodTypeCount[];
}

export interface DiagnosisCount {
  diagnosis: string;
  count: number;
}

export interface MonthlyTrend {
  year: number;
  month: string;
  count: number;
}

export interface BloodTypeCount {
  bloodType: string;
  count: number;
}