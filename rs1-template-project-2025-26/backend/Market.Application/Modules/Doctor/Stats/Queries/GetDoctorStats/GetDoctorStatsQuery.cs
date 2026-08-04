namespace Market.Application.Modules.Doctor.Stats.Queries.GetDoctorStats;

public sealed class GetDoctorStatsQuery : IRequest<GetDoctorStatsQueryDto>
{
}

public sealed class GetDoctorStatsQueryDto
{
    public int TotalPatients { get; init; }
    public int ReportsThisMonth { get; init; }
    public int ReportsToday { get; init; }
    public double AvgReportsPerPatient { get; init; }
    public IReadOnlyList<MonthlyTrendDto> MonthlyReportTrend { get; init; } = Array.Empty<MonthlyTrendDto>();
    public IReadOnlyList<BloodTypeCountDto> BloodTypeDistribution { get; init; } = Array.Empty<BloodTypeCountDto>();
}

public sealed class MonthlyTrendDto
{
    public int Year { get; init; }
    public string Month { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class BloodTypeCountDto
{
    public string? BloodType { get; init; }
    public int Count { get; init; }
}
