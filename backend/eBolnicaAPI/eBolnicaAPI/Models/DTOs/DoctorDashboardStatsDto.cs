namespace eBolnicaAPI.Models.DTOs
{
    public class DoctorDashboardStatsDto
    {
        public int TotalPatients { get; set; }

        public int ReportsThisMonth { get; set; }

        public int ReportsToday { get; set; }

        public double AvgReportsPerPatient { get; set; }

        public List<MonthlyTrendDto> MonthlyReportTrend { get; set; }

        public List<BloodTypeCountDto> BloodTypeDistribution { get; set; }
    }

    public class MonthlyTrendDto
    {
        public int Year { get; set; }
        public string Month { get; set; }

        public int Count { get; set; }
    }

    public class BloodTypeCountDto
    {
        public string BloodType { get; set; }

        public int Count { get; set; }
    }
}
