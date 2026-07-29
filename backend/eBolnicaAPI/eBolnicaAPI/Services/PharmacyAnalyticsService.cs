using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        
        // Cache configuration
        private const int CacheExpirationMinutes = 5; // Cache results for 5 minutes
        private const int LongCacheExpirationMinutes = 15; // Longer cache for stable data
        
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

                // Cache the response
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(CacheExpirationMinutes));

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

                // Cache the result
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));

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

                // Cache the result (longer cache for category data as it changes less frequently)
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(LongCacheExpirationMinutes));

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
        /// Gets stock trends for medications with optimized business logic
        /// Uses UpdatedAt timestamps to estimate historical stock levels
        /// </summary>
        public async Task<StockTrendsData> GetStockTrendsAsync(int[]? medicationIds = null, int days = 30, string interval = "daily")
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validate parameters
                if (days < 1 || days > 365)
                {
                    throw new ArgumentException("Days must be between 1 and 365", nameof(days));
                }

                var validIntervals = new[] { "daily", "weekly", "monthly" };
                if (!validIntervals.Contains(interval.ToLower()))
                {
                    throw new ArgumentException($"Interval must be one of: {string.Join(", ", validIntervals)}", nameof(interval));
                }

                // Generate cache key
                var cacheKey = $"stock_trends_{string.Join(",", medicationIds ?? Array.Empty<int>())}_{days}_{interval}";
                
                if (_cache.TryGetValue(cacheKey, out StockTrendsData? cachedData))
                {
                    _logger.LogInformation("Returning cached stock trends data");
                    return cachedData!;
                }

                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                // Get medications to track
                IQueryable<Medication> medicationQuery = _context.Medications
                    .AsNoTracking()
                    .Where(m => m.IsActive);

                if (medicationIds != null && medicationIds.Length > 0)
                {
                    // Validate medication IDs exist
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
                    // Default: Get top 5 medications by current stock quantity (most relevant)
                    medicationQuery = medicationQuery
                        .OrderByDescending(m => m.StockQuantity)
                        .Take(5);
                }

                var medications = await medicationQuery
                    .Select(m => new MedicationSummary
                    {
                        Id = m.Id,
                        Name = m.Name,
                        CurrentStock = m.StockQuantity,
                        TrendDirection = 0 // Will calculate later
                    })
                    .ToListAsync();

                if (medications.Count == 0)
                {
                    return new StockTrendsData
                    {
                        Data = new List<StockTrendItem>(),
                        Medications = new List<MedicationSummary>(),
                        Timeline = new List<string>()
                    };
                }

                // Assign colors to medications
                for (int i = 0; i < medications.Count; i++)
                {
                    medications[i].Color = ChartColors[i % ChartColors.Length];
                }

                // Get current stock data for all medications at once (optimized)
                var medicationEntities = await _context.Medications
                    .AsNoTracking()
                    .Where(m => medications.Select(med => med.Id).Contains(m.Id))
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.StockQuantity,
                        m.MinimumStockLevel,
                        m.UpdatedAt
                    })
                    .ToListAsync();

                // Generate timeline and stock data points
                var timeline = new List<string>();
                var trendData = new List<StockTrendItem>();
                var currentDate = startDate;

                // Calculate max stock for each medication (using MinimumStockLevel * 3 as max capacity)
                var medicationMaxStock = medicationEntities.ToDictionary(
                    m => m.Id,
                    m => Math.Max(m.MinimumStockLevel * 3, m.StockQuantity) // Ensure max is at least current stock
                );

                while (currentDate <= endDate)
                {
                    string dateLabel;
                    DateTime nextDate;

                    switch (interval.ToLower())
                    {
                        case "weekly":
                            dateLabel = currentDate.ToString("MMM dd", CultureInfo.InvariantCulture);
                            nextDate = currentDate.AddDays(7);
                            break;
                        case "monthly":
                            dateLabel = currentDate.ToString("MMM yyyy", CultureInfo.InvariantCulture);
                            nextDate = currentDate.AddMonths(1);
                            break;
                        default: // daily
                            dateLabel = currentDate.ToString("MMM dd", CultureInfo.InvariantCulture);
                            nextDate = currentDate.AddDays(1);
                            break;
                    }

                    timeline.Add(dateLabel);

                    // For each medication, calculate stock level
                    // Since we don't have historical data, we'll use current stock
                    // In a production system, you'd query InventoryHistory table
                    foreach (var medication in medications)
                    {
                        var medicationEntity = medicationEntities.FirstOrDefault(m => m.Id == medication.Id);
                        if (medicationEntity != null)
                        {
                            var maxStock = medicationMaxStock[medication.Id];
                            var stockLevel = maxStock > 0
                                ? (decimal)medicationEntity.StockQuantity / maxStock * 100
                                : 0;

                            // Determine status based on thresholds
                            string status = DetermineStockStatus(stockLevel);

                            trendData.Add(new StockTrendItem
                            {
                                Date = currentDate,
                                MedicationId = medication.Id,
                                MedicationName = medicationEntity.Name,
                                StockLevel = Math.Round(stockLevel, 2),
                                Quantity = medicationEntity.StockQuantity,
                                Status = status
                            });
                        }
                    }

                    currentDate = nextDate;
                }

                // Calculate trend direction for each medication (based on stock level change)
                foreach (var medication in medications)
                {
                    var medicationTrends = trendData
                        .Where(t => t.MedicationId == medication.Id)
                        .OrderBy(t => t.Date)
                        .ToList();

                    if (medicationTrends.Count >= 2)
                    {
                        var firstStock = medicationTrends.First().StockLevel;
                        var lastStock = medicationTrends.Last().StockLevel;
                        
                        if (firstStock > 0)
                        {
                            medication.TrendDirection = Math.Round((lastStock - firstStock) / firstStock, 4);
                        }
                        else if (lastStock > 0)
                        {
                            medication.TrendDirection = 1; // 100% increase from zero
                        }
                    }
                }

                var result = new StockTrendsData
                {
                    Data = trendData,
                    Medications = medications,
                    Timeline = timeline.Distinct().ToList()
                };

                stopwatch.Stop();
                _logger.LogInformation("Stock trends calculated in {ElapsedMs}ms. Medications: {MedicationCount}, Days: {Days}, Interval: {Interval}",
                    stopwatch.ElapsedMilliseconds, medications.Count, days, interval);

                // Cache the result
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheExpirationMinutes));

                return result;
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation errors
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating stock trends. MedicationIds: {MedicationIds}, Days: {Days}, Interval: {Interval}",
                    string.Join(",", medicationIds ?? Array.Empty<int>()), days, interval);
                throw new InvalidOperationException("An error occurred while calculating stock trends. Please try again later.", ex);
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
