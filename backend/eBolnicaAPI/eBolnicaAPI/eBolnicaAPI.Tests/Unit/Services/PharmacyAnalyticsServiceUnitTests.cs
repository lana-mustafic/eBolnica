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
                new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 }),
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

        [Fact]
        public async Task GetMonthlyRevenueAsync_ReturnsAggregatedDispensedRevenueByMonth()
        {
            var now = DateTime.UtcNow;
            var medication = _context.Medications.First();

            _context.Prescriptions.Add(new Prescription
            {
                PrescriptionNumber = "RX-TEST-1",
                MedicalReportId = 1,
                PatientId = 1,
                DoctorId = 1,
                Status = "Dispensed",
                PrescribedDate = now.AddMonths(-1),
                DispensedDate = now.AddMonths(-1),
                TotalAmount = 150m,
                PrescriptionItems = new List<PrescriptionItem>
                {
                    new PrescriptionItem
                    {
                        MedicationId = medication.Id,
                        Quantity = 2,
                        UnitPrice = 50m,
                        TotalPrice = 100m
                    },
                    new PrescriptionItem
                    {
                        MedicationId = medication.Id,
                        Quantity = 1,
                        UnitPrice = 50m,
                        TotalPrice = 50m
                    }
                }
            });

            _context.SaveChanges();

            var end = now;
            var start = now.AddMonths(-3);

            var result = await _service.GetMonthlyRevenueAsync(start, end, 3);

            Assert.NotEmpty(result.Data);
            Assert.Contains(result.Data, item => item.Revenue == 150m && item.PrescriptionCount == 1);
            Assert.Equal(150m, result.TotalRevenue);
        }

        [Fact]
        public async Task InvalidateAnalyticsCache_ForcesFreshDataOnNextRequest()
        {
            var first = await _service.GetStockTrendsAsync(null, 30, "daily");
            var cached = await _service.GetStockTrendsAsync(null, 30, "daily");

            Assert.Same(first, cached);

            _service.InvalidateAnalyticsCache();

            var fresh = await _service.GetStockTrendsAsync(null, 30, "daily");
            Assert.NotSame(first, fresh);
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
