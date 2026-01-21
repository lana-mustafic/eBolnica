using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace eBolnicaAPI.Services
{
    /// <summary>
    /// Service implementation for Pharmacy Analytics operations
    /// </summary>
    public class PharmacyAnalyticsService : IPharmacyAnalyticsService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PharmacyAnalyticsService> _logger;
        private const int CacheExpirationMinutes = 5; // Cache results for 5 minutes

        // Chart color palette for medications
        private static readonly string[] ChartColors = new[]
        {
            "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899",
            "#06b6d4", "#84cc16", "#f97316", "#6366f1", "#14b8a6", "#a855f7"
        };

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

                await Task.WhenAll(revenueTask, categoriesTask, trendsTask);

                response.MonthlyRevenue = await revenueTask;
                response.TopCategories = await categoriesTask;
                response.StockTrends = await trendsTask;

                // Update metadata summary
                response.Metadata.Summary = new StatisticsSummary
                {
                    TotalRevenue = response.MonthlyRevenue.TotalRevenue,
                    TotalCategories = response.TopCategories.TotalCategories,
                    TotalMedications = response.TopCategories.TotalMedications,
                    TotalPrescriptions = response.MonthlyRevenue.Data.Sum(m => m.PrescriptionCount)
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
        /// Gets monthly revenue data
        /// </summary>
        public async Task<RevenueData> GetMonthlyRevenueAsync(DateTime? startDate = null, DateTime? endDate = null, int months = 12)
        {
            try
            {
                // Determine date range
                DateTime start, end;
                if (startDate.HasValue && endDate.HasValue)
                {
                    start = startDate.Value;
                    end = endDate.Value;
                }
                else
                {
                    end = DateTime.UtcNow;
                    start = end.AddMonths(-months);
                }

                // Validate date range
                if (start > end)
                {
                    throw new ArgumentException("Start date cannot be after end date");
                }

                // Query dispensed prescriptions within date range
                var prescriptions = await _context.Prescriptions
                    .AsNoTracking()
                    .Where(p => p.Status == "Dispensed" && 
                                p.DispensedDate.HasValue &&
                                p.DispensedDate.Value >= start && 
                                p.DispensedDate.Value <= end)
                    .Select(p => new
                    {
                        p.DispensedDate!.Value,
                        p.TotalAmount
                    })
                    .ToListAsync();

                // Group by month-year and calculate revenue
                var monthlyData = prescriptions
                    .GroupBy(p => new { Year = p.Value.Year, Month = p.Value.Month })
                    .Select(g => new MonthlyRevenueItem
                    {
                        YearMonth = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                        MonthShort = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM", CultureInfo.InvariantCulture),
                        Revenue = g.Sum(p => p.TotalAmount),
                        PrescriptionCount = g.Count()
                    })
                    .OrderBy(m => m.YearMonth)
                    .ToList();

                // Fill in missing months with zero revenue
                var allMonths = new List<MonthlyRevenueItem>();
                var currentDate = new DateTime(start.Year, start.Month, 1);
                var endDateMonth = new DateTime(end.Year, end.Month, 1);

                while (currentDate <= endDateMonth)
                {
                    var existingMonth = monthlyData.FirstOrDefault(m => m.YearMonth == $"{currentDate.Year}-{currentDate.Month:D2}");
                    if (existingMonth != null)
                    {
                        allMonths.Add(existingMonth);
                    }
                    else
                    {
                        allMonths.Add(new MonthlyRevenueItem
                        {
                            YearMonth = $"{currentDate.Year}-{currentDate.Month:D2}",
                            Month = currentDate.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                            MonthShort = currentDate.ToString("MMM", CultureInfo.InvariantCulture),
                            Revenue = 0,
                            PrescriptionCount = 0
                        });
                    }
                    currentDate = currentDate.AddMonths(1);
                }

                var totalRevenue = allMonths.Sum(m => m.Revenue);
                var averageRevenue = allMonths.Count > 0 ? totalRevenue / allMonths.Count : 0;

                // Calculate percentage change vs previous period
                decimal revenueChangePercentage = 0;
                if (allMonths.Count >= 2)
                {
                    var currentPeriodRevenue = allMonths.Skip(allMonths.Count / 2).Sum(m => m.Revenue);
                    var previousPeriodRevenue = allMonths.Take(allMonths.Count / 2).Sum(m => m.Revenue);
                    
                    if (previousPeriodRevenue > 0)
                    {
                        revenueChangePercentage = ((currentPeriodRevenue - previousPeriodRevenue) / previousPeriodRevenue) * 100;
                    }
                }

                return new RevenueData
                {
                    Data = allMonths,
                    TotalRevenue = totalRevenue,
                    AverageMonthlyRevenue = averageRevenue,
                    RevenueChangePercentage = revenueChangePercentage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating monthly revenue");
                throw;
            }
        }

        /// <summary>
        /// Gets top medication categories
        /// </summary>
        public async Task<CategoriesData> GetTopCategoriesAsync(int topCount = 8)
        {
            try
            {
                // Get all active medications with categories
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

                // Group by category and calculate statistics
                var categoryGroups = medications
                    .GroupBy(m => m.Category!)
                    .Select(g => new CategoryItem
                    {
                        Category = g.Key,
                        MedicationCount = g.Count(),
                        Percentage = (decimal)g.Count() / totalMedications * 100,
                        TotalValue = g.Sum(m => m.Price * m.StockQuantity)
                    })
                    .OrderByDescending(c => c.MedicationCount)
                    .ToList();

                var totalCategories = categoryGroups.Count;

                // Take top N categories and aggregate the rest as "Other"
                var topCategories = categoryGroups.Take(topCount).ToList();
                var otherCategories = categoryGroups.Skip(topCount).ToList();

                if (otherCategories.Any())
                {
                    topCategories.Add(new CategoryItem
                    {
                        Category = "Other",
                        MedicationCount = otherCategories.Sum(c => c.MedicationCount),
                        Percentage = otherCategories.Sum(c => c.Percentage),
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating top categories");
                throw;
            }
        }

        /// <summary>
        /// Gets stock trends for medications
        /// </summary>
        public async Task<StockTrendsData> GetStockTrendsAsync(int[]? medicationIds = null, int days = 30, string interval = "daily")
        {
            try
            {
                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                // Get medications to track
                IQueryable<Medication> medicationQuery = _context.Medications.AsNoTracking().Where(m => m.IsActive);

                if (medicationIds != null && medicationIds.Length > 0)
                {
                    medicationQuery = medicationQuery.Where(m => medicationIds.Contains(m.Id));
                }
                else
                {
                    // Default: Get top 5 medications by stock movement (if we had historical data)
                    // For now, get top 5 by current stock quantity
                    medicationQuery = medicationQuery.OrderByDescending(m => m.StockQuantity).Take(5);
                }

                var medications = await medicationQuery
                    .Select(m => new MedicationSummary
                    {
                        Id = m.Id,
                        Name = m.Name,
                        CurrentStock = m.StockQuantity,
                        TrendDirection = 0 // Would need historical data to calculate
                    })
                    .ToListAsync();

                // Assign colors to medications
                for (int i = 0; i < medications.Count; i++)
                {
                    medications[i].Color = ChartColors[i % ChartColors.Length];
                }

                // Generate timeline based on interval
                var timeline = new List<string>();
                var trendData = new List<StockTrendItem>();
                var currentDate = startDate;

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

                    // For each medication, get current stock level
                    // Note: In a real system, you'd have historical stock data
                    // For now, we'll use current stock and simulate some variation
                    foreach (var medication in medications)
                    {
                        var medicationEntity = await _context.Medications
                            .AsNoTracking()
                            .FirstOrDefaultAsync(m => m.Id == medication.Id);

                        if (medicationEntity != null)
                        {
                            // Calculate stock level percentage
                            // Assuming maximum stock is 3x minimum stock level for calculation
                            var maxStock = medicationEntity.MinimumStockLevel * 3;
                            var stockLevel = maxStock > 0 
                                ? (decimal)medicationEntity.StockQuantity / maxStock * 100 
                                : 0;

                            // Determine status
                            string status = "Normal";
                            if (stockLevel < 20)
                                status = "Critical";
                            else if (stockLevel < 50)
                                status = "Low";

                            trendData.Add(new StockTrendItem
                            {
                                Date = currentDate,
                                MedicationId = medication.Id,
                                MedicationName = medication.Name,
                                StockLevel = Math.Round(stockLevel, 2),
                                Quantity = medicationEntity.StockQuantity,
                                Status = status
                            });
                        }
                    }

                    currentDate = nextDate;
                }

                // Calculate trend direction for each medication
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
                            medication.TrendDirection = ((lastStock - firstStock) / firstStock);
                        }
                    }
                }

                return new StockTrendsData
                {
                    Data = trendData,
                    Medications = medications,
                    Timeline = timeline.Distinct().ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating stock trends");
                throw;
            }
        }
    }
}
