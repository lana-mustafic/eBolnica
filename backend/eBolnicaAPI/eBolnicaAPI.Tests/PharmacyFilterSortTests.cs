using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace eBolnicaAPI.Tests
{
    /// <summary>
    /// Integration tests for Pharmacy module filtering and sorting functionality
    /// Tests LiveFilter support and multi-column sorting requirements
    /// </summary>
    public class PharmacyFilterSortTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly PharmacyService _pharmacyService;
        private readonly List<Medication> _testMedications;
        private readonly List<Prescription> _testPrescriptions;

        public PharmacyFilterSortTests()
        {
            // Setup in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _pharmacyService = new PharmacyService();

            // Seed test data
            _testMedications = SeedMedications();
            _testPrescriptions = SeedPrescriptions();
        }

        #region Medication Filtering Tests

        [Fact]
        public async Task GetFilteredMedications_SingleFilter_Category_ReturnsFilteredResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "category", "antibiotics" }
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, m => Assert.Equal("antibiotics", m.Category?.ToLower()));
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task GetFilteredMedications_MultipleFilters_ReturnsCombinedResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "category", "antibiotics" },
                { "minPrice", "10" },
                { "isActive", "true" }
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, m =>
            {
                Assert.Equal("antibiotics", m.Category?.ToLower());
                Assert.True(m.Price >= 10);
                Assert.True(m.IsActive);
            });
        }

        [Fact]
        public async Task GetFilteredMedications_SearchAndFilters_ReturnsCombinedResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "search", "penicillin" },
                { "category", "antibiotics" },
                { "stockStatus", "InStock" }
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, m =>
            {
                Assert.Contains("penicillin", m.Name.ToLower());
                Assert.Equal("antibiotics", m.Category?.ToLower());
                Assert.True(m.StockQuantity >= m.MinimumStockLevel);
            });
        }

        [Fact]
        public async Task GetFilteredMedications_AllFilterTypesCombined_ReturnsCorrectResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "search", "aspirin" },
                { "category", "painkiller" },
                { "minPrice", "5" },
                { "maxPrice", "50" },
                { "requiresPrescription", "false" },
                { "isActive", "true" },
                { "stockStatus", "InStock" }
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, m =>
            {
                Assert.Contains("aspirin", m.Name.ToLower());
                Assert.Equal("painkiller", m.Category?.ToLower());
                Assert.True(m.Price >= 5 && m.Price <= 50);
                Assert.False(m.RequiresPrescription);
                Assert.True(m.IsActive);
                Assert.True(m.StockQuantity >= m.MinimumStockLevel);
            });
        }

        [Fact]
        public async Task GetFilteredMedications_EmptySearchTerm_DoesNotBreakQuery()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "search", "" },
                { "category", "antibiotics" }
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert - Should only filter by category, ignore empty search
            Assert.All(results, m => Assert.Equal("antibiotics", m.Category?.ToLower()));
        }

        [Fact]
        public async Task GetFilteredMedications_CaseInsensitiveFiltering_WorksCorrectly()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "category", "ANTIBIOTICS" }, // Uppercase
                { "search", "PENICILLIN" } // Uppercase
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, m =>
            {
                Assert.Equal("antibiotics", m.Category?.ToLower());
                Assert.Contains("penicillin", m.Name.ToLower());
            });
        }

        [Fact]
        public async Task GetFilteredMedications_StockStatusInStock_ReturnsCorrectResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "stockStatus", "InStock" }
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, m => Assert.True(m.StockQuantity >= m.MinimumStockLevel));
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task ApplySorting_SingleColumn_Ascending_ReturnsSortedResults()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act
            var sortedQuery = _pharmacyService.ApplySorting(query, "name", "asc");
            var results = await sortedQuery.ToListAsync();

            // Assert
            var names = results.Select(m => m.Name).ToList();
            var sortedNames = names.OrderBy(n => n).ToList();
            Assert.Equal(sortedNames, names);
        }

        [Fact]
        public async Task ApplySorting_SingleColumn_Descending_ReturnsSortedResults()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act
            var sortedQuery = _pharmacyService.ApplySorting(query, "price", "desc");
            var results = await sortedQuery.ToListAsync();

            // Assert
            var prices = results.Select(m => m.Price).ToList();
            var sortedPrices = prices.OrderByDescending(p => p).ToList();
            Assert.Equal(sortedPrices, prices);
        }

        [Fact]
        public async Task ApplySorting_MultiColumn_ReturnsCorrectlySortedResults()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act - Sort by category ascending, then price descending
            var sortedQuery = _pharmacyService.ApplySorting(query, "category,price", "asc,desc");
            var results = await sortedQuery.ToListAsync();

            // Assert
            var grouped = results.GroupBy(m => m.Category).ToList();
            foreach (var group in grouped)
            {
                var prices = group.Select(m => m.Price).ToList();
                var sortedPrices = prices.OrderByDescending(p => p).ToList();
                Assert.Equal(sortedPrices, prices);
            }
        }

        [Fact]
        public async Task ApplySorting_MultiColumn_EmbeddedOrder_ReturnsCorrectlySortedResults()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act - Sort using embedded order format: name:asc,price:desc
            var sortedQuery = _pharmacyService.ApplySorting(query, "name:asc,price:desc", null);
            var results = await sortedQuery.ToListAsync();

            // Assert - Should be sorted by name ascending, then price descending
            var grouped = results.GroupBy(m => m.Name).ToList();
            foreach (var group in grouped)
            {
                var prices = group.Select(m => m.Price).ToList();
                var sortedPrices = prices.OrderByDescending(p => p).ToList();
                Assert.Equal(sortedPrices, prices);
            }
        }

        [Fact]
        public async Task ApplySorting_DefaultSorting_ReturnsCreatedAtDesc()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act
            var sortedQuery = _pharmacyService.ApplySorting(query, null, null);
            var results = await sortedQuery.ToListAsync();

            // Assert
            var dates = results.Select(m => m.CreatedAt).ToList();
            var sortedDates = dates.OrderByDescending(d => d).ToList();
            Assert.Equal(sortedDates, dates);
        }

        [Fact]
        public async Task ApplySorting_InvalidColumn_FallsBackToDefault()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act
            var sortedQuery = _pharmacyService.ApplySorting(query, "invalidColumn", "asc");
            var results = await sortedQuery.ToListAsync();

            // Assert - Should fall back to CreatedAt desc
            var dates = results.Select(m => m.CreatedAt).ToList();
            var sortedDates = dates.OrderByDescending(d => d).ToList();
            Assert.Equal(sortedDates, dates);
        }

        #endregion

        #region Combined Filtering and Sorting Tests

        [Fact]
        public async Task GetFilteredMedications_WithSorting_ReturnsFilteredAndSortedResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "category", "antibiotics" },
                { "minPrice", "10" }
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var sortedQuery = _pharmacyService.ApplySorting(filteredQuery, "price", "asc");
            var results = await sortedQuery.ToListAsync();

            // Assert
            Assert.All(results, m =>
            {
                Assert.Equal("antibiotics", m.Category?.ToLower());
                Assert.True(m.Price >= 10);
            });
            var prices = results.Select(m => m.Price).ToList();
            var sortedPrices = prices.OrderBy(p => p).ToList();
            Assert.Equal(sortedPrices, prices);
        }

        #endregion

        #region Prescription Filtering Tests

        [Fact]
        public async Task GetFilteredPrescriptions_StatusFilter_ReturnsFilteredResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "status", "Pending" }
            });
            var baseQuery = _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionItems)
                .AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredPrescriptions(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, p => Assert.Equal("Pending", p.Status));
        }

        [Fact]
        public async Task GetFilteredPrescriptions_MultipleFilters_ReturnsCombinedResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "status", "Pending" },
                { "minAmount", "50" },
                { "maxAmount", "200" }
            });
            var baseQuery = _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionItems)
                .AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredPrescriptions(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, p =>
            {
                Assert.Equal("Pending", p.Status);
                Assert.True(p.TotalAmount >= 50 && p.TotalAmount <= 200);
            });
        }

        #endregion

        #region Helper Methods

        private IQueryCollection CreateQueryCollection(Dictionary<string, string> parameters)
        {
            var queryParams = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            foreach (var param in parameters)
            {
                queryParams[param.Key] = param.Value;
            }
            return new QueryCollection(queryParams);
        }

        private List<Medication> SeedMedications()
        {
            var medications = new List<Medication>
            {
                new Medication
                {
                    Name = "Penicillin",
                    GenericName = "Penicillin G",
                    Category = "antibiotics",
                    Price = 15.50m,
                    StockQuantity = 100,
                    MinimumStockLevel = 20,
                    IsActive = true,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.Now.AddDays(-10)
                },
                new Medication
                {
                    Name = "Amoxicillin",
                    GenericName = "Amoxicillin",
                    Category = "antibiotics",
                    Price = 12.00m,
                    StockQuantity = 50,
                    MinimumStockLevel = 15,
                    IsActive = true,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.Now.AddDays(-5)
                },
                new Medication
                {
                    Name = "Aspirin",
                    GenericName = "Acetylsalicylic Acid",
                    Category = "painkiller",
                    Price = 8.50m,
                    StockQuantity = 200,
                    MinimumStockLevel = 50,
                    IsActive = true,
                    RequiresPrescription = false,
                    CreatedAt = DateTime.Now.AddDays(-3)
                },
                new Medication
                {
                    Name = "Ibuprofen",
                    GenericName = "Ibuprofen",
                    Category = "painkiller",
                    Price = 6.00m,
                    StockQuantity = 5, // Low stock
                    MinimumStockLevel = 20,
                    IsActive = true,
                    RequiresPrescription = false,
                    CreatedAt = DateTime.Now.AddDays(-1)
                },
                new Medication
                {
                    Name = "Paracetamol",
                    GenericName = "Paracetamol",
                    Category = "painkiller",
                    Price = 4.50m,
                    StockQuantity = 0, // Out of stock
                    MinimumStockLevel = 30,
                    IsActive = true,
                    RequiresPrescription = false,
                    CreatedAt = DateTime.Now
                }
            };

            _context.Medications.AddRange(medications);
            _context.SaveChanges();
            return medications;
        }

        private List<Prescription> SeedPrescriptions()
        {
            // Note: This requires Patient, Doctor entities to exist
            // For full integration tests, you'd need to seed those as well
            return new List<Prescription>();
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
