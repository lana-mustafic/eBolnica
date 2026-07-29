using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class PharmacyAnalyticsServiceUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly PharmacyAnalyticsService _service;

        public PharmacyAnalyticsServiceUnitTests()
        {
            _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            _service = new PharmacyAnalyticsService(
                _context,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<PharmacyAnalyticsService>.Instance);

            SeedDashboardMetricsData();
        }

        [Fact]
        public async Task GetStockTrendsAsync_ReturnsCurrentSnapshotWithoutSyntheticTimeline()
        {
            var result = await _service.GetStockTrendsAsync(null, 30, "daily");

            Assert.Equal("current-stock-snapshot", result.MetricType);
            Assert.Equal(4, result.Data.Count);
            Assert.Equal(4, result.Data.Select(item => item.MedicationId).Distinct().Count());
            Assert.Empty(result.Timeline);
            Assert.All(result.Data, item => Assert.Equal(item.Date, result.SnapshotAt));
            Assert.All(result.Medications, summary => Assert.Equal(0m, summary.TrendDirection));
        }

        [Fact]
        public async Task GetDashboardStatsAsync_SummaryContainsRealInventoryMetrics()
        {
            var result = await _service.GetDashboardStatsAsync(new DashboardStatsQueryParams
            {
                RevenueMonths = 1,
                TopCategoriesCount = 1,
                TrendDays = 1
            });

            var summary = result.Metadata.Summary;

            Assert.Equal(4, summary.TotalMedications);
            Assert.Equal(2, summary.LowStockAlerts);
            Assert.Equal(1, summary.ExpiringSoon);
            Assert.Equal(1, summary.ExpiredMedications);
            Assert.Equal(1825m, summary.InventoryValue);
        }

        private void SeedDashboardMetricsData()
        {
            var now = DateTime.Now;

            _context.Medications.AddRange(
                new Medication
                {
                    Name = "In Stock Med",
                    Price = 10m,
                    StockQuantity = 100,
                    MinimumStockLevel = 20,
                    IsActive = true,
                    RequiresPrescription = false
                },
                new Medication
                {
                    Name = "Low Stock Med",
                    Price = 5m,
                    StockQuantity = 5,
                    MinimumStockLevel = 20,
                    IsActive = true,
                    RequiresPrescription = false
                },
                new Medication
                {
                    Name = "Expiring Soon Med",
                    Price = 8m,
                    StockQuantity = 25,
                    MinimumStockLevel = 10,
                    ExpiryDate = now.AddDays(10),
                    IsActive = true,
                    RequiresPrescription = false
                },
                new Medication
                {
                    Name = "Expired Med",
                    Price = 12m,
                    StockQuantity = 50,
                    MinimumStockLevel = 60,
                    ExpiryDate = now.AddDays(-2),
                    IsActive = true,
                    RequiresPrescription = false
                },
                new Medication
                {
                    Name = "Inactive Med",
                    Price = 100m,
                    StockQuantity = 999,
                    MinimumStockLevel = 1,
                    IsActive = false,
                    RequiresPrescription = false
                }
            );

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
