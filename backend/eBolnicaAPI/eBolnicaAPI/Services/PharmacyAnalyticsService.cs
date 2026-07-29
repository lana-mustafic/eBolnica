using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
using System.Globalization;

namespace eBolnicaAPI.Services
{
    /// <summary>
    /// Service implementation for Pharmacy Analytics operations with optimized business logic
    /// </summary>
    public class PharmacyAnalyticsService : IPharmacyAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PharmacyAnalyticsService> _logger;
        
        // Cache configuration — all analytics endpoints share the same TTL and invalidation token.
        private const int CacheExpirationMinutes = 5;

        private readonly object _cacheInvalidationLock = new();
        private CancellationTokenSource _cacheInvalidationCts = new();
        
        // Performance thresholds
        private const int MaxQueryTimeoutSeconds = 30;
        private const int MaxResultSetSize = 10000;

        // Chart color palette for medications
        private static readonly string[] ChartColors = new[]
        {
            "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899",
            "#06b6d4", "#84cc16", "#f97316", "#6366f1", "#14b8a6", "#a855f7"
        };

        // Stock level thresholds
        private const decimal CriticalStockThreshold = 20m; // < 20%
        private const decimal LowStockThreshold = 50m; // 20% - 50%
        private const decimal NormalStockThreshold = 80m; // 50% - 80%
        // > 80% is Optimal

        public PharmacyAnalyticsService(
            AppDbContext context,
            IMemoryCache cache,
            ILogger<PharmacyAnalyticsService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Gets complete dashboard statistics
        /// </summary>
        public async Task<DashboardStatsResponse> GetDashboardStatsAsync(DashboardStatsQueryParams queryParams)
        {
            // Generate cache key from query parameters
            var cacheKey = $"dashboard_stats_" +
                $"{queryParams.StartDate?.ToString("yyyyMMdd") ?? "null"}_" +
                $"{queryParams.EndDate?.ToString("yyyyMMdd") ?? "null"}_" +
                $"{queryParams.RevenueMonths ?? 12}_" +
                $"{queryParams.TopCategoriesCount ?? 8}_" +
                $"{string.Join(",", queryParams.MedicationIds ?? Array.Empty<int>())}_" +
                $"{queryParams.TrendDays ?? 30}_" +
                $"{queryParams.TrendInterval ?? "daily"}";
            
            if (_cache.TryGetValue(cacheKey, out DashboardStatsResponse? cachedResponse))
            {
                _logger.LogInformation("Returning cached dashboard stats");
                return cachedResponse!;
            }

            var response = new DashboardStatsResponse
            {
                Metadata = new Metadata
                {
                    GeneratedAt = DateTime.UtcNow,
                    DateRange = queryParams.StartDate.HasValue && queryParams.EndDate.HasValue
                        ? new DateRange { StartDate = queryParams.StartDate.Value, EndDate = queryParams.EndDate.Value }
                        : null
                }
            };

            try
            {
                // Calculate date range for revenue
                DateTime revenueStartDate, revenueEndDate;
                if (queryParams.StartDate.HasValue && queryParams.EndDate.HasValue)
                {
                    revenueStartDate = queryParams.StartDate.Value;
                    revenueEndDate = queryParams.EndDate.Value;
                }
                else
                {
                    revenueEndDate = DateTime.UtcNow;
                    revenueStartDate = revenueEndDate.AddMonths(-(queryParams.RevenueMonths ?? 12));
                }

                // Execute all queries in parallel for better performance
                var revenueTask = GetMonthlyRevenueAsync(revenueStartDate, revenueEndDate, queryParams.RevenueMonths ?? 12);
                var categoriesTask = GetTopCategoriesAsync(queryParams.TopCategoriesCount ?? 8);
                var trendsTask = GetStockTrendsAsync(
                    queryParams.MedicationIds,
                    queryParams.TrendDays ?? 30,
                    queryParams.TrendInterval ?? "daily");
                var summaryCountsTask = GetDashboardSummaryMetricsAsync();

                await Task.WhenAll(revenueTask, categoriesTask, trendsTask, summaryCountsTask);

                response.MonthlyRevenue = await revenueTask;
                response.TopCategories = await categoriesTask;
                response.StockTrends = await trendsTask;
                var summaryCounts = await summaryCountsTask;

                // Update metadata summary
                response.Metadata.Summary = new StatisticsSummary
                {
                    TotalRevenue = response.MonthlyRevenue.TotalRevenue,
                    TotalCategories = response.TopCategories.TotalCategories,
                    TotalMedications = summaryCounts.ActiveMedications,
                    TotalPrescriptions = response.MonthlyRevenue.Data.Sum(m => m.PrescriptionCount),
                    PendingPrescriptions = summaryCounts.PendingPrescriptions,
                    LowStockAlerts = summaryCounts.LowStockAlerts,
                    ExpiringSoon = summaryCounts.ExpiringSoon,
                    ExpiredMedications = summaryCounts.ExpiredMedications,
                    InventoryValue = summaryCounts.InventoryValue
                };

                _cache.Set(cacheKey, response, CreateCacheEntryOptions());

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard statistics");
                throw;
            }
        }

        /// <summary>
        /// Gets monthly revenue data with optimized business logic
        /// Calculates revenue from PrescriptionItems (Quantity * UnitPrice) for accurate totals
        /// </summary>
        public async Task<RevenueData> GetMonthlyRevenueAsync(DateTime? startDate = null, DateTime? endDate = null, int months = 12)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Generate cache key
                var cacheKey = $"monthly_revenue_{startDate?.ToString("yyyyMMdd") ?? "null"}_{endDate?.ToString("yyyyMMdd") ?? "null"}_{months}";
                
                if (_cache.TryGetValue(cacheKey, out RevenueData? cachedData))
                {
                    _logger.LogInformation("Returning cached monthly revenue data");
                    return cachedData!;
                }

                // Determine date range
                DateTime start, end;
                if (startDate.HasValue && endDate.HasValue)
                {
                    start = startDate.Value.Date;
                    end = endDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
                }
                else
                {
                    end = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
                    start = end.AddMonths(-months).Date;
                }

                // Validate date range
                if (start > end)
                {
                    throw new ArgumentException("Start date cannot be after end date");
                }

                // Limit date range to prevent excessive queries
                var maxDateRange = TimeSpan.FromDays(730); // 2 years max
                if (end - start > maxDateRange)
                {
                    throw new ArgumentException($"Date range cannot exceed {maxDateRange.Days} days");
                }

                // Optimized query: Aggregate revenue from PrescriptionItems for accurate calculation
                // Only include completed/dispensed prescriptions
                var monthlyRevenueQuery = from prescription in _context.Prescriptions.AsNoTracking()
                                         join prescriptionItem in _context.PrescriptionItems.AsNoTracking()
                                             on prescription.Id equals prescriptionItem.PrescriptionId
                                         where prescription.Status == "Dispensed"
                                             && prescription.DispensedDate.HasValue
                                             && prescription.DispensedDate.Value >= start
                                             && prescription.DispensedDate.Value <= end
                                         group new
                                         {
                                             prescription.DispensedDate!.Value,
                                             prescriptionItem.TotalPrice, // Use TotalPrice (Quantity * UnitPrice)
                                             prescription.Id
                                         } by new
                                         {
                                             Year = prescription.DispensedDate.Value.Year,
                                             Month = prescription.DispensedDate.Value.Month
                                         } into g
                                         select new MonthlyRevenueItem
                                         {
                                             YearMonth = $"{g.Key.Year}-{g.Key.Month:D2}",
                                             Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                                             MonthShort = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM", CultureInfo.InvariantCulture),
                                             Revenue = g.Sum(x => x.TotalPrice),
                                             PrescriptionCount = g.Select(x => x.Id).Distinct().Count()
                                         };

                var monthlyData = await monthlyRevenueQuery
                    .OrderBy(m => m.YearMonth)
                    .ToListAsync();

                // Fill in missing months with zero revenue
                var allMonths = new List<MonthlyRevenueItem>();
                var currentDate = new DateTime(start.Year, start.Month, 1);
                var endDateMonth = new DateTime(end.Year, end.Month, 1);

                while (currentDate <= endDateMonth)
                {
                    var yearMonth = $"{currentDate.Year}-{currentDate.Month:D2}";
                    var existingMonth = monthlyData.FirstOrDefault(m => m.YearMonth == yearMonth);
                    
                    if (existingMonth != null)
                    {
                        allMonths.Add(existingMonth);
                    }
                    else
                    {
                        allMonths.Add(new MonthlyRevenueItem
                        {
                            YearMonth = yearMonth,
                            Month = currentDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                            MonthShort = currentDate.ToString("MMM", CultureInfo.InvariantCulture),
                            Revenue = 0,
                            PrescriptionCount = 0
                        });
                    }
                    currentDate = currentDate.AddMonths(1);
                }

                // Calculate summary statistics
                var totalRevenue = allMonths.Sum(m => m.Revenue);
                var averageRevenue = allMonths.Count > 0 ? totalRevenue / allMonths.Count : 0;

                // Calculate percentage change vs previous period (last month vs previous month)
                decimal revenueChangePercentage = 0;
                if (allMonths.Count >= 2)
                {
                    var lastMonth = allMonths.Last();
                    var previousMonth = allMonths[allMonths.Count - 2];
                    
                    if (previousMonth.Revenue > 0)
                    {
                        revenueChangePercentage = ((lastMonth.Revenue - previousMonth.Revenue) / previousMonth.Revenue) * 100;
                    }
                    else if (lastMonth.Revenue > 0)
                    {
                        revenueChangePercentage = 100; // 100% increase from zero
                    }
                }

                var result = new RevenueData
                {
                    Data = allMonths,
                    TotalRevenue = totalRevenue,
                    AverageMonthlyRevenue = averageRevenue,
                    RevenueChangePercentage = Math.Round(revenueChangePercentage, 2)
                };

                stopwatch.Stop();
                _logger.LogInformation("Monthly revenue calculated in {ElapsedMs}ms. Period: {StartDate} to {EndDate}, Months: {MonthCount}",
                    stopwatch.ElapsedMilliseconds, start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"), allMonths.Count);

                _cache.Set(cacheKey, result, CreateCacheEntryOptions());

                return result;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation errors
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating monthly revenue. StartDate: {StartDate}, EndDate: {EndDate}, Months: {Months}",
                    startDate, endDate, months);
                throw new InvalidOperationException("An error occurred while calculating monthly revenue. Please try again later.", ex);
            }
        }

        /// <summary>
        /// Gets top medication categories with optimized business logic
        /// Calculates categories based on prescription sales (both count and revenue weighted)
        /// </summary>
        public async Task<CategoriesData> GetTopCategoriesAsync(int topCount = 8)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validate topCount
                if (topCount < 1 || topCount > 50)
                {
                    throw new ArgumentException("TopCount must be between 1 and 50", nameof(topCount));
                }

                // Generate cache key
                var cacheKey = $"top_categories_{topCount}";
                
                if (_cache.TryGetValue(cacheKey, out CategoriesData? cachedData))
                {
                    _logger.LogInformation("Returning cached top categories data");
                    return cachedData!;
                }

                // Get date range for recent sales (last 12 months for relevance)
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddMonths(-12);

                // Optimized query: Calculate categories from prescription sales
                // This gives more accurate representation than just medication inventory
                var categoryStatsQuery = from prescriptionItem in _context.PrescriptionItems.AsNoTracking()
                                       join prescription in _context.Prescriptions.AsNoTracking()
                                           on prescriptionItem.PrescriptionId equals prescription.Id
                                       join medication in _context.Medications.AsNoTracking()
                                           on prescriptionItem.MedicationId equals medication.Id
                                       where prescription.Status == "Dispensed"
                                           && medication.IsActive
                                           && !string.IsNullOrEmpty(medication.Category)
                                           && prescription.DispensedDate.HasValue
                                           && prescription.DispensedDate.Value >= startDate
                                           && prescription.DispensedDate.Value <= endDate
                                       group new
                                       {
                                           medication.Category,
                                           prescriptionItem.Quantity,
                                           prescriptionItem.TotalPrice
                                       } by medication.Category! into g
                                       select new CategoryItem
                                       {
                                           Category = g.Key,
                                           MedicationCount = g.Sum(x => x.Quantity), // Total quantity sold
                                           TotalValue = g.Sum(x => x.TotalPrice), // Total revenue from category
                                           Percentage = 0 // Will calculate after getting total
                                       };

                var categoryStats = await categoryStatsQuery
                    .OrderByDescending(c => c.MedicationCount)
                    .ToListAsync();

                // Calculate total for percentage calculation
                var totalMedicationsSold = categoryStats.Sum(c => c.MedicationCount);
                var totalRevenue = categoryStats.Sum(c => c.TotalValue);

                if (totalMedicationsSold == 0)
                {
                    // Fallback: Use medication inventory if no sales data
                    return await GetTopCategoriesFromInventoryAsync(topCount);
                }

                // Calculate percentages
                foreach (var category in categoryStats)
                {
                    category.Percentage = totalMedicationsSold > 0
                        ? Math.Round((decimal)category.MedicationCount / totalMedicationsSold * 100, 2)
                        : 0;
                }

                var totalCategories = categoryStats.Count;

                // Take top N categories and aggregate the rest as "Other"
                var topCategories = categoryStats.Take(topCount).ToList();
                var otherCategories = categoryStats.Skip(topCount).ToList();

                if (otherCategories.Any())
                {
                    topCategories.Add(new CategoryItem
                    {
                        Category = "Other",
                        MedicationCount = otherCategories.Sum(c => c.MedicationCount),
                        Percentage = Math.Round(otherCategories.Sum(c => c.Percentage), 2),
                        TotalValue = otherCategories.Sum(c => c.TotalValue)
                    });
                }

                var result = new CategoriesData
                {
                    Data = topCategories,
                    TotalCategories = totalCategories,
                    TotalMedications = totalMedicationsSold
                };

                stopwatch.Stop();
                _logger.LogInformation("Top categories calculated in {ElapsedMs}ms. TopCount: {TopCount}, Categories: {CategoryCount}",
                    stopwatch.ElapsedMilliseconds, topCount, totalCategories);

                _cache.Set(cacheKey, result, CreateCacheEntryOptions());

                return result;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation errors
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating top categories. TopCount: {TopCount}", topCount);
                throw new InvalidOperationException("An error occurred while calculating top categories. Please try again later.", ex);
            }
        }

        /// <summary>
        /// Fallback method: Get top categories from medication inventory when no sales data available
        /// </summary>
        private async Task<CategoriesData> GetTopCategoriesFromInventoryAsync(int topCount)
        {
            var medications = await _context.Medications
                .AsNoTracking()
                .Where(m => m.IsActive && !string.IsNullOrEmpty(m.Category))
                .Select(m => new
                {
                    m.Category,
                    m.Price,
                    m.StockQuantity
                })
                .ToListAsync();

            var totalMedications = medications.Count;

            if (totalMedications == 0)
            {
                return new CategoriesData
                {
                    Data = new List<CategoryItem>(),
                    TotalCategories = 0,
                    TotalMedications = 0
                };
            }

            var categoryGroups = medications
                .GroupBy(m => m.Category!)
                .Select(g => new CategoryItem
                {
                    Category = g.Key,
                    MedicationCount = g.Count(),
                    Percentage = Math.Round((decimal)g.Count() / totalMedications * 100, 2),
                    TotalValue = g.Sum(m => m.Price * m.StockQuantity)
                })
                .OrderByDescending(c => c.MedicationCount)
                .ToList();

            var totalCategories = categoryGroups.Count;
            var topCategories = categoryGroups.Take(topCount).ToList();
            var otherCategories = categoryGroups.Skip(topCount).ToList();

            if (otherCategories.Any())
            {
                topCategories.Add(new CategoryItem
                {
                    Category = "Other",
                    MedicationCount = otherCategories.Sum(c => c.MedicationCount),
                    Percentage = Math.Round(otherCategories.Sum(c => c.Percentage), 2),
                    TotalValue = otherCategories.Sum(c => c.TotalValue)
                });
            }

            return new CategoriesData
            {
                Data = topCategories,
                TotalCategories = totalCategories,
                TotalMedications = totalMedications
            };
        }

        /// <summary>
        /// Gets current stock levels by medication (snapshot).
        /// No inventory history table exists; returns one honest data point per medication.
        /// </summary>
        public async Task<StockTrendsData> GetStockTrendsAsync(int[]? medicationIds = null, int days = 30, string interval = "daily")
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (days < 1 || days > 365)
                {
                    throw new ArgumentException("Days must be between 1 and 365", nameof(days));
                }

                var validIntervals = new[] { "daily", "weekly", "monthly" };
                if (!validIntervals.Contains(interval.ToLower()))
                {
                    throw new ArgumentException($"Interval must be one of: {string.Join(", ", validIntervals)}", nameof(interval));
                }

                if (days != 30 || !string.Equals(interval, "daily", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Stock trends days/interval parameters are ignored until inventory history is available. Days: {Days}, Interval: {Interval}",
                        days,
                        interval);
                }

                var cacheKey = $"current_stock_snapshot_{string.Join(",", medicationIds ?? Array.Empty<int>())}";

                if (_cache.TryGetValue(cacheKey, out StockTrendsData? cachedData))
                {
                    _logger.LogInformation("Returning cached current stock snapshot");
                    return cachedData!;
                }

                IQueryable<Medication> medicationQuery = _context.Medications
                    .AsNoTracking()
                    .Where(m => m.IsActive);

                if (medicationIds != null && medicationIds.Length > 0)
                {
                    var validIds = await _context.Medications
                        .Where(m => medicationIds.Contains(m.Id) && m.IsActive)
                        .Select(m => m.Id)
                        .ToListAsync();

                    if (validIds.Count != medicationIds.Length)
                    {
                        var invalidIds = medicationIds.Except(validIds).ToList();
                        _logger.LogWarning("Invalid medication IDs provided: {InvalidIds}", string.Join(", ", invalidIds));
                    }

                    medicationQuery = medicationQuery.Where(m => validIds.Contains(m.Id));
                }
                else
                {
                    medicationQuery = medicationQuery
                        .OrderByDescending(m => m.StockQuantity)
                        .Take(5);
                }

                var medicationEntities = await medicationQuery
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.StockQuantity,
                        m.MinimumStockLevel
                    })
                    .ToListAsync();

                if (medicationEntities.Count == 0)
                {
                    return new StockTrendsData
                    {
                        Data = new List<StockTrendItem>(),
                        Medications = new List<MedicationSummary>(),
                        Timeline = new List<string>(),
                        MetricType = "current-stock-snapshot",
                        SnapshotAt = DateTime.UtcNow
                    };
                }

                var snapshotAt = DateTime.UtcNow;
                var trendData = new List<StockTrendItem>(medicationEntities.Count);
                var medications = new List<MedicationSummary>(medicationEntities.Count);

                for (var i = 0; i < medicationEntities.Count; i++)
                {
                    var entity = medicationEntities[i];
                    var capacity = Math.Max(entity.MinimumStockLevel * 3, entity.StockQuantity);
                    var stockLevel = capacity > 0
                        ? Math.Round((decimal)entity.StockQuantity / capacity * 100, 2)
                        : 0m;

                    trendData.Add(new StockTrendItem
                    {
                        Date = snapshotAt,
                        MedicationId = entity.Id,
                        MedicationName = entity.Name,
                        StockLevel = stockLevel,
                        Quantity = entity.StockQuantity,
                        Status = DetermineStockStatus(stockLevel)
                    });

                    medications.Add(new MedicationSummary
                    {
                        Id = entity.Id,
                        Name = entity.Name,
                        Color = ChartColors[i % ChartColors.Length],
                        CurrentStock = entity.StockQuantity,
                        TrendDirection = 0
                    });
                }

                var result = new StockTrendsData
                {
                    Data = trendData,
                    Medications = medications,
                    Timeline = new List<string>(),
                    MetricType = "current-stock-snapshot",
                    SnapshotAt = snapshotAt
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "Current stock snapshot calculated in {ElapsedMs}ms. Medications: {MedicationCount}",
                    stopwatch.ElapsedMilliseconds,
                    medications.Count);

                _cache.Set(cacheKey, result, CreateCacheEntryOptions());

                return result;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating current stock snapshot. MedicationIds: {MedicationIds}",
                    string.Join(",", medicationIds ?? Array.Empty<int>()));
                throw new InvalidOperationException("An error occurred while calculating stock levels. Please try again later.", ex);
            }
        }

        /// <summary>
        /// Authoritative metrics for dashboard summary cards (inventory + prescriptions).
        /// Uses the same expiry window as the inventory API (30 days, local time).
        /// </summary>
        private async Task<(int ActiveMedications, int PendingPrescriptions, int LowStockAlerts, int ExpiringSoon, int ExpiredMedications, decimal InventoryValue)> GetDashboardSummaryMetricsAsync()
        {
            var now = DateTime.Now;
            var in30Days = now.AddDays(30);

            var activeMedicationsQuery = _context.Medications.AsNoTracking().Where(m => m.IsActive);

            var activeMedications = await activeMedicationsQuery.CountAsync();
            var pendingPrescriptions = await _context.Prescriptions.AsNoTracking()
                .CountAsync(p => p.Status == "Pending");
            var lowStockAlerts = await activeMedicationsQuery
                .CountAsync(m => m.StockQuantity < m.MinimumStockLevel);
            var expiringSoon = await activeMedicationsQuery
                .CountAsync(m => m.ExpiryDate.HasValue
                    && m.ExpiryDate.Value > now
                    && m.ExpiryDate.Value <= in30Days);
            var expiredMedications = await activeMedicationsQuery
                .CountAsync(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= now);
            var inventoryValue = await activeMedicationsQuery
                .SumAsync(m => m.Price * m.StockQuantity);

            return (activeMedications, pendingPrescriptions, lowStockAlerts, expiringSoon, expiredMedications, inventoryValue);
        }

        /// <inheritdoc />
        public void InvalidateAnalyticsCache()
        {
            lock (_cacheInvalidationLock)
            {
                _cacheInvalidationCts.Cancel();
                _cacheInvalidationCts.Dispose();
                _cacheInvalidationCts = new CancellationTokenSource();
            }

            _logger.LogInformation("Pharmacy analytics cache invalidated");
        }

        private MemoryCacheEntryOptions CreateCacheEntryOptions()
        {
            CancellationToken invalidationToken;
            lock (_cacheInvalidationLock)
            {
                invalidationToken = _cacheInvalidationCts.Token;
            }

            return new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes))
                .AddExpirationToken(new CancellationChangeToken(invalidationToken));
        }

        /// <summary>
        /// Determines stock status based on stock level percentage
        /// </summary>
        private string DetermineStockStatus(decimal stockLevel)
        {
            if (stockLevel < CriticalStockThreshold)
                return "Critical";
            else if (stockLevel < LowStockThreshold)
                return "Low";
            else if (stockLevel < NormalStockThreshold)
                return "Normal";
            else
                return "Optimal";
        }
    }
}
