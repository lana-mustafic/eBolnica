using eBolnica.Application.Modules.Doctor.Stats.Queries.GetDoctorStats;

public sealed class GetDoctorStatsQueryHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<GetDoctorStatsQuery, GetDoctorStatsQueryDto>
{
    public async Task<GetDoctorStatsQueryDto> Handle(GetDoctorStatsQuery request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var doctor = await ctx.Doctors
            .FirstOrDefaultAsync(d => d.UserId == currentUser.UserId.Value, ct);

        if (doctor is null)
            throw new eBolnicaNotFoundException("Doctor profile not found.");

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var todayStart = now.Date;

        var totalPatients = await ctx.Patients.CountAsync(p => p.DoctorId == doctor.Id, ct);

        var reportsThisMonth = await ctx.MedicalReports
            .Where(r => r.DoctorId == doctor.Id && r.CreatedAtUtc >= monthStart)
            .CountAsync(ct);

        var reportsToday = await ctx.MedicalReports
            .Where(r => r.DoctorId == doctor.Id && r.CreatedAtUtc >= todayStart)
            .CountAsync(ct);

        var monthlyTrend = await GetMonthlyReportTrend(ctx, doctor.Id, 6, ct);

        var bloodTypeDistribution = await ctx.Patients
            .Where(p => p.DoctorId == doctor.Id && p.BloodType != null)
            .GroupBy(p => p.BloodType)
            .Select(g => new BloodTypeCountDto
            {
                BloodType = g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct);

        double avgReportsPerPatient = 0;
        if (totalPatients > 0)
        {
            var reportCounts = await ctx.Patients
                .Where(p => p.DoctorId == doctor.Id && p.MedicalRecord != null)
                .Select(p => p.MedicalRecord!.Id)
                .ToListAsync(ct);

            if (reportCounts.Count > 0)
            {
                var totalReports = await ctx.MedicalReports
                    .CountAsync(r => reportCounts.Contains(r.MedicalRecordId), ct);
                avgReportsPerPatient = (double)totalReports / totalPatients;
            }
        }

        return new GetDoctorStatsQueryDto
        {
            TotalPatients = totalPatients,
            ReportsThisMonth = reportsThisMonth,
            ReportsToday = reportsToday,
            MonthlyReportTrend = monthlyTrend,
            BloodTypeDistribution = bloodTypeDistribution,
            AvgReportsPerPatient = avgReportsPerPatient
        };
    }

    private static async Task<List<MonthlyTrendDto>> GetMonthlyReportTrend(
        IAppDbContext ctx, int doctorId, int months, CancellationToken ct)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);

        var results = await ctx.MedicalReports
            .Where(r => r.DoctorId == doctorId && r.CreatedAtUtc >= startDate)
            .GroupBy(r => new { r.CreatedAtUtc.Year, r.CreatedAtUtc.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(ct);

        return results.Select(x => new MonthlyTrendDto
        {
            Year = x.Year,
            Month = $"{x.Year}-{x.Month:00}",
            Count = x.Count
        }).ToList();
    }
}
