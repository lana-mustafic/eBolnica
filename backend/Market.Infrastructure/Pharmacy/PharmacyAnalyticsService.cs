using Market.Application.Abstractions;
using Market.Application.Modules.Pharmacy.Analytics;
using Market.Domain.Entities.Pharmacy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Market.Infrastructure.Pharmacy;

public sealed class PharmacyAnalyticsService(
    IAppDbContext ctx,
    IMemoryCache cache,
    ILogger<PharmacyAnalyticsService> logger) : IPharmacyAnalyticsService
{
    private const int CacheExpirationMinutes = 5;
    private readonly object _cacheInvalidationLock = new();
    private CancellationTokenSource _cacheInvalidationCts = new();

    private static readonly string[] ChartColors =
    [
        "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899",
        "#06b6d4", "#84cc16", "#f97316", "#6366f1", "#14b8a6", "#a855f7"
    ];

    public async Task<DashboardStatsResponseDto> GetDashboardStatsAsync(
        DateTime? startDate,
        DateTime? endDate,
        int revenueMonths = 12,
        int topCategoriesCount = 8,
        int[]? medicationIds = null,
        int trendDays = 30,
        string trendInterval = "daily",
        CancellationToken ct = default)
    {
        var cacheKey =
            $"dashboard_stats_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{revenueMonths}_{topCategoriesCount}_{string.Join(',', medicationIds ?? [])}_{trendDays}_{trendInterval}";

        if (cache.TryGetValue(cacheKey, out DashboardStatsResponseDto? cached))
            return cached!;

        DateTime revenueStart, revenueEnd;
        if (startDate.HasValue && endDate.HasValue)
        {
            revenueStart = startDate.Value;
            revenueEnd = endDate.Value;
        }
        else
        {
            revenueEnd = DateTime.UtcNow;
            revenueStart = revenueEnd.AddMonths(-revenueMonths);
        }

        var revenueTask = GetMonthlyRevenueAsync(revenueStart, revenueEnd, revenueMonths, ct);
        var categoriesTask = GetTopCategoriesAsync(topCategoriesCount, ct);
        var trendsTask = GetStockTrendsAsync(medicationIds, trendDays, trendInterval, ct);
        var summaryTask = GetSummaryMetricsAsync(ct);

        await Task.WhenAll(revenueTask, categoriesTask, trendsTask, summaryTask);

        var revenue = await revenueTask;
        var categories = await categoriesTask;
        var trends = await trendsTask;
        var summary = await summaryTask;

        var response = new DashboardStatsResponseDto
        {
            MonthlyRevenue = revenue,
            TopCategories = categories,
            StockTrends = trends,
            Metadata = new AnalyticsMetadataDto
            {
                GeneratedAt = DateTime.UtcNow,
                DateRange = startDate.HasValue && endDate.HasValue
                    ? new AnalyticsDateRangeDto { StartDate = startDate.Value, EndDate = endDate.Value }
                    : null,
                Summary = new StatisticsSummaryDto
                {
                    TotalRevenue = revenue.TotalRevenue,
                    TotalCategories = categories.TotalCategories,
                    TotalMedications = summary.ActiveMedications,
                    TotalPrescriptions = revenue.Data.Sum(m => m.PrescriptionCount),
                    PendingPrescriptions = summary.PendingPrescriptions,
                    LowStockAlerts = summary.LowStockAlerts,
                    ExpiringSoon = summary.ExpiringSoon,
                    ExpiredMedications = summary.ExpiredMedications,
                    InventoryValue = summary.InventoryValue
                }
            }
        };

        cache.Set(cacheKey, response, CreateCacheEntryOptions());
        return response;
    }

    public async Task<RevenueDataDto> GetMonthlyRevenueAsync(
        DateTime? startDate,
        DateTime? endDate,
        int months = 12,
        CancellationToken ct = default)
    {
        var cacheKey = $"monthly_revenue_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{months}";
        if (cache.TryGetValue(cacheKey, out RevenueDataDto? cached))
            return cached!;

        DateTime start, end;
        if (startDate.HasValue && endDate.HasValue)
        {
            start = startDate.Value.Date;
            end = endDate.Value.Date.AddDays(1).AddTicks(-1);
        }
        else
        {
            end = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
            start = end.AddMonths(-months).Date;
        }

        if (start > end)
            throw new ArgumentException("Start date cannot be after end date.");

        var revenueByMonth = await (
            from item in ctx.PrescriptionItems.AsNoTracking()
            join rx in ctx.Prescriptions.AsNoTracking() on item.PrescriptionId equals rx.Id
            where rx.Status == PrescriptionStatuses.Dispensed
                  && rx.DispensedDate.HasValue
                  && rx.DispensedDate.Value >= start
                  && rx.DispensedDate.Value <= end
            group item by new { rx.DispensedDate!.Value.Year, rx.DispensedDate!.Value.Month } into g
            select new { g.Key.Year, g.Key.Month, Revenue = g.Sum(x => x.TotalPrice) }
        ).ToListAsync(ct);

        var countsByMonth = await ctx.Prescriptions.AsNoTracking()
            .Where(rx => rx.Status == PrescriptionStatuses.Dispensed
                         && rx.DispensedDate.HasValue
                         && rx.DispensedDate.Value >= start
                         && rx.DispensedDate.Value <= end)
            .GroupBy(rx => new { rx.DispensedDate!.Value.Year, rx.DispensedDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var allMonths = new List<MonthlyRevenueItemDto>();
        var cursor = new DateTime(start.Year, start.Month, 1);
        var endMonth = new DateTime(end.Year, end.Month, 1);

        while (cursor <= endMonth)
        {
            var row = revenueByMonth.FirstOrDefault(r => r.Year == cursor.Year && r.Month == cursor.Month);
            var count = countsByMonth.FirstOrDefault(c => c.Year == cursor.Year && c.Month == cursor.Month)?.Count ?? 0;
            allMonths.Add(new MonthlyRevenueItemDto
            {
                YearMonth = $"{cursor.Year}-{cursor.Month:D2}",
                Month = cursor.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                MonthShort = cursor.ToString("MMM", CultureInfo.InvariantCulture),
                Revenue = row?.Revenue ?? 0,
                PrescriptionCount = count
            });
            cursor = cursor.AddMonths(1);
        }

        var total = allMonths.Sum(m => m.Revenue);
        var avg = allMonths.Count > 0 ? total / allMonths.Count : 0m;
        decimal change = 0;
        if (allMonths.Count >= 2)
        {
            var last = allMonths[^1];
            var prev = allMonths[^2];
            change = prev.Revenue > 0 ? Math.Round((last.Revenue - prev.Revenue) / prev.Revenue * 100, 2) : last.Revenue > 0 ? 100 : 0;
        }

        var result = new RevenueDataDto
        {
            Data = allMonths,
            TotalRevenue = total,
            AverageMonthlyRevenue = avg,
            RevenueChangePercentage = change
        };

        cache.Set(cacheKey, result, CreateCacheEntryOptions());
        return result;
    }

    public async Task<CategoriesDataDto> GetTopCategoriesAsync(int topCount = 8, CancellationToken ct = default)
    {
        if (topCount is < 1 or > 50)
            throw new ArgumentException("TopCount must be between 1 and 50.", nameof(topCount));

        var cacheKey = $"top_categories_{topCount}";
        if (cache.TryGetValue(cacheKey, out CategoriesDataDto? cached))
            return cached!;

        var startDate = DateTime.UtcNow.AddMonths(-12);
        var categoryStats = await (
            from item in ctx.PrescriptionItems.AsNoTracking()
            join rx in ctx.Prescriptions.AsNoTracking() on item.PrescriptionId equals rx.Id
            join med in ctx.Medications.AsNoTracking() on item.MedicationId equals med.Id
            where rx.Status == PrescriptionStatuses.Dispensed
                  && med.IsActive
                  && med.Category != null && med.Category != ""
                  && rx.DispensedDate.HasValue
                  && rx.DispensedDate.Value >= startDate
            group new { med.Category, item.Quantity, item.TotalPrice } by med.Category! into g
            select new CategoryItemDto
            {
                Category = g.Key,
                MedicationCount = g.Sum(x => x.Quantity),
                TotalValue = g.Sum(x => x.TotalPrice)
            }
        ).OrderByDescending(c => c.MedicationCount).ToListAsync(ct);

        if (categoryStats.Count == 0)
            return await GetTopCategoriesFromInventoryAsync(topCount, ct);

        var totalSold = categoryStats.Sum(c => c.MedicationCount);
        foreach (var c in categoryStats)
            c.Percentage = totalSold > 0 ? Math.Round((decimal)c.MedicationCount / totalSold * 100, 2) : 0;

        var top = categoryStats.Take(topCount).ToList();
        var other = categoryStats.Skip(topCount).ToList();
        if (other.Count > 0)
        {
            top.Add(new CategoryItemDto
            {
                Category = "Other",
                MedicationCount = other.Sum(c => c.MedicationCount),
                Percentage = Math.Round(other.Sum(c => c.Percentage), 2),
                TotalValue = other.Sum(c => c.TotalValue)
            });
        }

        var result = new CategoriesDataDto
        {
            Data = top,
            TotalCategories = categoryStats.Count,
            TotalMedications = totalSold
        };

        cache.Set(cacheKey, result, CreateCacheEntryOptions());
        return result;
    }

    public async Task<StockTrendsDataDto> GetStockTrendsAsync(
        int[]? medicationIds = null,
        int days = 30,
        string interval = "daily",
        CancellationToken ct = default)
    {
        var cacheKey = $"current_stock_snapshot_{string.Join(',', medicationIds ?? [])}";
        if (cache.TryGetValue(cacheKey, out StockTrendsDataDto? cached))
            return cached!;

        IQueryable<MedicationEntity> query = ctx.Medications.AsNoTracking().Where(m => m.IsActive);
        if (medicationIds is { Length: > 0 })
            query = query.Where(m => medicationIds.Contains(m.Id));
        else
            query = query.OrderByDescending(m => m.StockQuantity).Take(5);

        var meds = await query.Select(m => new { m.Id, m.Name, m.StockQuantity, m.MinimumStockLevel }).ToListAsync(ct);
        var snapshotAt = DateTime.UtcNow;
        var data = new List<StockTrendItemDto>();
        var summaries = new List<MedicationSummaryDto>();

        for (var i = 0; i < meds.Count; i++)
        {
            var m = meds[i];
            var capacity = Math.Max(m.MinimumStockLevel * 3, m.StockQuantity);
            var level = capacity > 0 ? Math.Round((decimal)m.StockQuantity / capacity * 100, 2) : 0m;
            data.Add(new StockTrendItemDto
            {
                Date = snapshotAt,
                MedicationId = m.Id,
                MedicationName = m.Name,
                StockLevel = level,
                Quantity = m.StockQuantity,
                Status = DetermineStockStatus(level)
            });
            summaries.Add(new MedicationSummaryDto
            {
                Id = m.Id,
                Name = m.Name,
                Color = ChartColors[i % ChartColors.Length],
                CurrentStock = m.StockQuantity
            });
        }

        var result = new StockTrendsDataDto
        {
            Data = data,
            Medications = summaries,
            MetricType = "current-stock-snapshot",
            SnapshotAt = snapshotAt
        };

        cache.Set(cacheKey, result, CreateCacheEntryOptions());
        return result;
    }

    public void InvalidateAnalyticsCache()
    {
        lock (_cacheInvalidationLock)
        {
            _cacheInvalidationCts.Cancel();
            _cacheInvalidationCts.Dispose();
            _cacheInvalidationCts = new CancellationTokenSource();
        }

        logger.LogInformation("Pharmacy analytics cache invalidated");
    }

    private async Task<(int ActiveMedications, int PendingPrescriptions, int LowStockAlerts, int ExpiringSoon, int ExpiredMedications, decimal InventoryValue)> GetSummaryMetricsAsync(CancellationToken ct)
    {
        var now = DateTime.Now;
        var in30Days = now.AddDays(30);
        var active = ctx.Medications.AsNoTracking().Where(m => m.IsActive);

        return (
            await active.CountAsync(ct),
            await ctx.Prescriptions.AsNoTracking().CountAsync(p => p.Status == PrescriptionStatuses.Pending, ct),
            await active.CountAsync(m => m.StockQuantity < m.MinimumStockLevel, ct),
            await active.CountAsync(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value > now && m.ExpiryDate.Value <= in30Days, ct),
            await active.CountAsync(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= now, ct),
            await active.SumAsync(m => m.Price * m.StockQuantity, ct)
        );
    }

    private async Task<CategoriesDataDto> GetTopCategoriesFromInventoryAsync(int topCount, CancellationToken ct)
    {
        var meds = await ctx.Medications.AsNoTracking()
            .Where(m => m.IsActive && m.Category != null && m.Category != "")
            .Select(m => new { m.Category, m.Price, m.StockQuantity })
            .ToListAsync(ct);

        if (meds.Count == 0)
            return new CategoriesDataDto();

        var groups = meds.GroupBy(m => m.Category!)
            .Select(g => new CategoryItemDto
            {
                Category = g.Key,
                MedicationCount = g.Count(),
                TotalValue = g.Sum(m => m.Price * m.StockQuantity)
            })
            .OrderByDescending(c => c.MedicationCount)
            .ToList();

        var total = meds.Count;
        foreach (var g in groups)
            g.Percentage = Math.Round((decimal)g.MedicationCount / total * 100, 2);

        var top = groups.Take(topCount).ToList();
        var other = groups.Skip(topCount).ToList();
        if (other.Count > 0)
        {
            top.Add(new CategoryItemDto
            {
                Category = "Other",
                MedicationCount = other.Sum(c => c.MedicationCount),
                Percentage = Math.Round(other.Sum(c => c.Percentage), 2),
                TotalValue = other.Sum(c => c.TotalValue)
            });
        }

        return new CategoriesDataDto
        {
            Data = top,
            TotalCategories = groups.Count,
            TotalMedications = total
        };
    }

    private MemoryCacheEntryOptions CreateCacheEntryOptions()
    {
        CancellationToken token;
        lock (_cacheInvalidationLock)
            token = _cacheInvalidationCts.Token;

        return new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes))
            .AddExpirationToken(new CancellationChangeToken(token));
    }

    private static string DetermineStockStatus(decimal stockLevel) => stockLevel switch
    {
        < 20 => "Critical",
        < 50 => "Low",
        < 80 => "Normal",
        _ => "Optimal"
    };
}
