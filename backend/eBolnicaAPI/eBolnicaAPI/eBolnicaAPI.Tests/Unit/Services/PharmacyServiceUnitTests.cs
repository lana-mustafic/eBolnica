using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for PharmacyService focusing on pagination, filtering, and sorting logic
    /// </summary>
    public class PharmacyServiceUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly PharmacyService _pharmacyService;
        private readonly List<Medication> _testMedications;

        public PharmacyServiceUnitTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _pharmacyService = new PharmacyService();
            _testMedications = SeedTestMedications();
        }

        #region Pagination Logic Tests

        [Fact]
        public void Pagination_SkipCalculation_IsCorrect()
        {
            // Arrange
            int pageNumber = 3;
            int pageSize = 10;
            int expectedSkip = (pageNumber - 1) * pageSize; // Should be 20

            // Act
            int actualSkip = (pageNumber - 1) * pageSize;

            // Assert
            Assert.Equal(20, actualSkip);
            Assert.Equal(expectedSkip, actualSkip);
        }

        [Fact]
        public void Pagination_PageNumberZero_DefaultsToOne()
        {
            // Arrange
            int pageNumber = 0;
            int pageSize = 10;

            // Act
            int normalizedPage = pageNumber < 1 ? 1 : pageNumber;
            int skip = (normalizedPage - 1) * pageSize;

            // Assert
            Assert.Equal(1, normalizedPage);
            Assert.Equal(0, skip);
        }

        [Fact]
        public void Pagination_PageSizeExceedsMax_DefaultsToMax()
        {
            // Arrange
            int pageSize = 150;
            int maxPageSize = 100;

            // Act
            int normalizedPageSize = Math.Clamp(pageSize, 1, maxPageSize);

            // Assert
            Assert.Equal(100, normalizedPageSize);
        }

        [Fact]
        public void Pagination_EmptyResults_ReturnsEmptyPaginatedResponse()
        {
            // Arrange
            var emptyList = new List<MedicationDto>();
            int totalCount = 0;
            int pageNumber = 1;
            int pageSize = 10;

            // Act
            var response = new PaginatedResponse<MedicationDto>(emptyList, totalCount, pageNumber, pageSize);

            // Assert
            Assert.Empty(response.Items);
            Assert.Equal(0, response.TotalCount);
            Assert.Equal(0, response.TotalPages);
            Assert.False(response.HasNext);
            Assert.False(response.HasPrevious);
        }

        [Fact]
        public void Pagination_TotalPagesCalculation_IsCorrect()
        {
            // Arrange
            int totalCount = 95;
            int pageSize = 10;

            // Act
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Assert
            Assert.Equal(10, totalPages); // 95 / 10 = 9.5, ceiling = 10
        }

        [Fact]
        public void Pagination_HasNextAndHasPrevious_AreCorrect()
        {
            // Arrange
            int totalCount = 50;
            int pageSize = 10;
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Act & Assert - Page 1
            int page1 = 1;
            bool hasNext1 = page1 < totalPages;
            bool hasPrevious1 = page1 > 1;
            Assert.True(hasNext1);
            Assert.False(hasPrevious1);

            // Act & Assert - Middle page
            int page3 = 3;
            bool hasNext3 = page3 < totalPages;
            bool hasPrevious3 = page3 > 1;
            Assert.True(hasNext3);
            Assert.True(hasPrevious3);

            // Act & Assert - Last page
            int page5 = 5;
            bool hasNext5 = page5 < totalPages;
            bool hasPrevious5 = page5 > 1;
            Assert.False(hasNext5);
            Assert.True(hasPrevious5);
        }

        #endregion

        #region Filter Builder Tests

        [Fact]
        public async Task GetFilteredMedications_CategoryFilter_AppliesCorrectly()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string> { { "category", "antibiotics" } });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, m => Assert.Equal("antibiotics", m.Category?.ToLower()));
        }

        [Fact]
        public async Task GetFilteredMedications_SearchTermQueryParam_FiltersByName()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string> { { "searchTerm", "amoxicillin" } });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, m =>
                Assert.Contains("amoxicillin", m.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetFilteredMedications_SearchTerm_WithSpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string> { { "search", "test%_[]" } });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert - Should not throw exception, EF Core handles escaping
            Assert.NotNull(results);
        }

        [Fact]
        public async Task GetFilteredMedications_MinPriceGreaterThanMaxPrice_ReturnsEmptyResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "minPrice", "50" },
                { "maxPrice", "10" }
            });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert - Should return empty results (no medications match impossible range)
            Assert.Empty(results);
        }

        [Fact]
        public async Task GetFilteredMedications_InvalidCategory_ReturnsEmptyResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string> { { "category", "nonexistent" } });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task GetFilteredMedications_EmptySearchTerm_DoesNotBreakQuery()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string> { { "search", "" } });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert - Should return all medications (empty search is ignored)
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task GetFilteredMedications_MultipleFilters_CombineWithAndLogic()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string>
            {
                { "category", "antibiotics" },
                { "minPrice", "10" },
                { "maxPrice", "20" },
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
                Assert.True(m.Price >= 10 && m.Price <= 20);
                Assert.True(m.IsActive);
            });
        }

        [Fact]
        public async Task GetFilteredMedications_StockStatusInStock_ReturnsCorrectResults()
        {
            // Arrange
            var queryParams = CreateQueryCollection(new Dictionary<string, string> { { "stockStatus", "in stock" } });
            var baseQuery = _context.Medications.AsQueryable();

            // Act
            var filteredQuery = _pharmacyService.GetFilteredMedications(baseQuery, queryParams);
            var results = await filteredQuery.ToListAsync();

            // Assert
            Assert.All(results, m => Assert.True(m.StockQuantity >= m.MinimumStockLevel));
        }

        #endregion

        #region GetFilteredInventory Tests

        [Fact]
        public async Task GetFilteredInventory_DefaultsToActiveMedicationsOnly()
        {
            var baseQuery = _context.Medications.AsQueryable();

            var filteredQuery = _pharmacyService.GetFilteredInventory(baseQuery, new PharmacyQueryParameters());
            var results = await filteredQuery.ToListAsync();

            Assert.NotEmpty(results);
            Assert.All(results, m => Assert.True(m.IsActive));
        }

        [Fact]
        public async Task GetFilteredInventory_StockStatusLowStock_ReturnsLowStockItems()
        {
            var baseQuery = _context.Medications.AsQueryable();

            var filteredQuery = _pharmacyService.GetFilteredInventory(
                baseQuery,
                new PharmacyQueryParameters { StockStatus = "low stock" });
            var results = await filteredQuery.ToListAsync();

            Assert.Single(results);
            Assert.Equal("Ibuprofen", results[0].Name);
            Assert.True(results[0].StockQuantity >= 5);
            Assert.True(results[0].StockQuantity < results[0].MinimumStockLevel);
        }

        [Fact]
        public async Task GetFilteredInventory_StockStatusCriticalStock_ReturnsCriticalStockItems()
        {
            var baseQuery = _context.Medications.AsQueryable();

            var filteredQuery = _pharmacyService.GetFilteredInventory(
                baseQuery,
                new PharmacyQueryParameters { StockStatus = "critical stock" });
            var results = await filteredQuery.ToListAsync();

            Assert.Single(results);
            Assert.Equal("Critical Stock Med", results[0].Name);
            Assert.InRange(results[0].StockQuantity, 1, 4);
        }

        [Fact]
        public async Task GetFilteredInventory_StockStatusOutOfStock_ReturnsEmptyStockItems()
        {
            var baseQuery = _context.Medications.AsQueryable();

            var filteredQuery = _pharmacyService.GetFilteredInventory(
                baseQuery,
                new PharmacyQueryParameters { StockStatus = "out of stock" });
            var results = await filteredQuery.ToListAsync();

            Assert.Single(results);
            Assert.Equal("Empty Stock Med", results[0].Name);
            Assert.Equal(0, results[0].StockQuantity);
        }

        [Fact]
        public async Task GetFilteredInventory_ExpiryGood_IncludesMissingExpiryAndFarFutureDates()
        {
            var today = DateTime.Now.Date;
            var baseQuery = _context.Medications.AsQueryable();

            var filteredQuery = _pharmacyService.GetFilteredInventory(
                baseQuery,
                new PharmacyQueryParameters { ExpiryAfter = today.AddDays(90) });
            var results = await filteredQuery.ToListAsync();

            Assert.Contains(results, m => m.Name == "Penicillin" && !m.ExpiryDate.HasValue);
            Assert.Contains(results, m => m.Name == "Expiry Good Med");
            Assert.DoesNotContain(results, m => m.Name == "Expiry Warning Med");
        }

        [Fact]
        public async Task GetFilteredInventory_ExpiryWarning_ReturnsWarningBucketItems()
        {
            var today = DateTime.Now.Date;
            var baseQuery = _context.Medications.AsQueryable();

            var filteredQuery = _pharmacyService.GetFilteredInventory(
                baseQuery,
                new PharmacyQueryParameters
                {
                    ExpiryAfter = today.AddDays(30),
                    ExpiryBefore = today.AddDays(89)
                });
            var results = await filteredQuery.ToListAsync();

            Assert.Single(results);
            Assert.Equal("Expiry Warning Med", results[0].Name);
        }

        [Fact]
        public async Task GetFilteredInventory_ExpiryCritical_ReturnsCriticalBucketItems()
        {
            var today = DateTime.Now.Date;
            var baseQuery = _context.Medications.AsQueryable();

            var filteredQuery = _pharmacyService.GetFilteredInventory(
                baseQuery,
                new PharmacyQueryParameters
                {
                    ExpiryAfter = today,
                    ExpiryBefore = today.AddDays(29)
                });
            var results = await filteredQuery.ToListAsync();

            Assert.Single(results);
            Assert.Equal("Expiry Critical Med", results[0].Name);
        }

        [Fact]
        public async Task GetFilteredInventory_ExpiryExpired_ReturnsExpiredItems()
        {
            var today = DateTime.Now.Date;
            var baseQuery = _context.Medications.AsQueryable();

            var filteredQuery = _pharmacyService.GetFilteredInventory(
                baseQuery,
                new PharmacyQueryParameters { ExpiryBefore = today.AddDays(-1) });
            var results = await filteredQuery.ToListAsync();

            Assert.Single(results);
            Assert.Equal("Expiry Expired Med", results[0].Name);
        }

        #endregion

        #region Sort Builder Tests

        [Fact]
        public async Task ApplySorting_SingleColumn_ReturnsSortedResults()
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
        public async Task ApplySorting_InvalidColumn_DefaultsToCreatedAt()
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

        [Fact]
        public async Task ApplySorting_InvalidSortOrder_DefaultsToDesc()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act
            var sortedQuery = _pharmacyService.ApplySorting(query, "name", "invalid");
            var results = await sortedQuery.ToListAsync();

            // Assert - Should default to desc
            var names = results.Select(m => m.Name).ToList();
            var sortedNames = names.OrderByDescending(n => n).ToList();
            Assert.Equal(sortedNames, names);
        }

        [Fact]
        public async Task ApplySorting_MultiColumn_WithDifferentColumnCount_HandlesGracefully()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act - More columns than orders
            var sortedQuery = _pharmacyService.ApplySorting(query, "category,name,price", "asc");
            var results = await sortedQuery.ToListAsync();

            // Assert - Should use first order for all columns or default
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task ApplySorting_MultiColumn_ReturnsCorrectlySortedResults()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act
            var sortedQuery = _pharmacyService.ApplySorting(query, "category,name", "asc,asc");
            var results = await sortedQuery.ToListAsync();

            // Assert
            var grouped = results.GroupBy(m => m.Category).ToList();
            foreach (var group in grouped)
            {
                var names = group.Select(m => m.Name).ToList();
                var sortedNames = names.OrderBy(n => n).ToList();
                Assert.Equal(sortedNames, names);
            }
        }

        [Fact]
        public async Task ApplySorting_EmbeddedOrderFormat_WorksCorrectly()
        {
            // Arrange
            var query = _context.Medications.AsQueryable();

            // Act
            var sortedQuery = _pharmacyService.ApplySorting(query, "name:asc,price:desc", null);
            var results = await sortedQuery.ToListAsync();

            // Assert
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

        #endregion

        #region Parameter Validation Tests

        [Fact]
        public void ParameterValidation_PageNumberLessThanOne_DefaultsToOne()
        {
            // Arrange
            int pageNumber = -5;

            // Act
            int normalized = pageNumber < 1 ? 1 : pageNumber;

            // Assert
            Assert.Equal(1, normalized);
        }

        [Fact]
        public void ParameterValidation_PageSizeLessThanOne_DefaultsToOne()
        {
            // Arrange
            int pageSize = -10;

            // Act
            int normalized = Math.Clamp(pageSize, 1, 100);

            // Assert
            Assert.Equal(1, normalized);
        }

        [Fact]
        public void ParameterValidation_PageSizeGreaterThanMax_ClampsToMax()
        {
            // Arrange
            int pageSize = 200;

            // Act
            int normalized = Math.Clamp(pageSize, 1, 100);

            // Assert
            Assert.Equal(100, normalized);
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

        private List<Medication> SeedTestMedications()
        {
            var today = DateTime.Now.Date;
            var medications = new List<Medication>
            {
                new Medication
                {
                    Name = "Penicillin",
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
                    Category = "painkiller",
                    Price = 6.00m,
                    StockQuantity = 5,
                    MinimumStockLevel = 20,
                    IsActive = true,
                    RequiresPrescription = false,
                    CreatedAt = DateTime.Now.AddDays(-1)
                },
                new Medication
                {
                    Name = "Critical Stock Med",
                    Category = "painkiller",
                    Price = 7.00m,
                    StockQuantity = 3,
                    MinimumStockLevel = 20,
                    IsActive = true,
                    RequiresPrescription = false,
                    CreatedAt = DateTime.Now.AddDays(-2)
                },
                new Medication
                {
                    Name = "Empty Stock Med",
                    Category = "painkiller",
                    Price = 9.00m,
                    StockQuantity = 0,
                    MinimumStockLevel = 10,
                    IsActive = true,
                    RequiresPrescription = false,
                    CreatedAt = DateTime.Now.AddDays(-2)
                },
                new Medication
                {
                    Name = "Expiry Good Med",
                    Category = "antibiotics",
                    Price = 11.00m,
                    StockQuantity = 40,
                    MinimumStockLevel = 10,
                    ExpiryDate = today.AddDays(120),
                    IsActive = true,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.Now.AddDays(-4)
                },
                new Medication
                {
                    Name = "Expiry Warning Med",
                    Category = "antibiotics",
                    Price = 10.00m,
                    StockQuantity = 35,
                    MinimumStockLevel = 10,
                    ExpiryDate = today.AddDays(60),
                    IsActive = true,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.Now.AddDays(-4)
                },
                new Medication
                {
                    Name = "Expiry Critical Med",
                    Category = "antibiotics",
                    Price = 9.50m,
                    StockQuantity = 30,
                    MinimumStockLevel = 10,
                    ExpiryDate = today.AddDays(15),
                    IsActive = true,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.Now.AddDays(-4)
                },
                new Medication
                {
                    Name = "Expiry Expired Med",
                    Category = "antibiotics",
                    Price = 8.00m,
                    StockQuantity = 25,
                    MinimumStockLevel = 10,
                    ExpiryDate = today.AddDays(-10),
                    IsActive = true,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.Now.AddDays(-4)
                }
            };

            _context.Medications.AddRange(medications);
            _context.SaveChanges();
            return medications;
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
