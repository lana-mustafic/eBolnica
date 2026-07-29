using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace eBolnicaAPI.Tests.Integration.Controllers
{
    /// <summary>
    /// Integration tests for PharmacyController API endpoints
    /// Tests HTTP endpoints directly with full request/response cycle
    /// </summary>
    public class PharmacyControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
    {
        private readonly HttpClient _client;
        private readonly AppDbContext _context;
        private readonly CustomWebApplicationFactory _factory;

        public PharmacyControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

            // Get database context for seeding
            var scope = factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedTestData();
        }

        #region GetMedications Integration Tests

        [Fact]
        public async Task GetMedications_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var url = "/api/pharmacy/medications?pageNumber=1&pageSize=5";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(5, result.PageSize);
            Assert.True(result.TotalCount > 0);
            Assert.True(result.Items.Count <= 5);
        }

        [Fact]
        public async Task GetMedications_WithFiltering_ReturnsFilteredResults()
        {
            // Arrange
            var url = "/api/pharmacy/medications?category=antibiotics&minPrice=10&maxPrice=20";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.All(result.Items, m =>
            {
                Assert.Equal("antibiotics", m.Category?.ToLower());
                Assert.True(m.Price >= 10 && m.Price <= 20);
            });
        }

        [Fact]
        public async Task GetMedications_WithSearchTerm_ReturnsFilteredResults()
        {
            // Arrange
            var url = "/api/pharmacy/medications?searchTerm=amoxicillin&pageSize=100";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.All(result.Items, m =>
                Assert.Contains("amoxicillin", m.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetMedications_WithAllMedicationFilterParams_ReturnsFilteredResults()
        {
            // Arrange — query names aligned with PharmacyQueryParameters / FE buildMedicationQueryParams
            var url = "/api/pharmacy/medications?pageNumber=1&pageSize=20&searchTerm=amox&category=antibiotics&stockStatus=normal stock&requiresPrescription=true&isActive=true&minPrice=1&maxPrice=100&sortBy=name&sortOrder=asc";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.All(result.Items, m =>
            {
                Assert.Equal("antibiotics", m.Category?.ToLower());
                Assert.True(m.RequiresPrescription);
                Assert.True(m.IsActive);
                Assert.True(m.Price >= 1 && m.Price <= 100);
                Assert.True(m.StockQuantity >= m.MinimumStockLevel);
            });
        }

        [Fact]
        public async Task GetMedications_WithSorting_ReturnsSortedResults()
        {
            // Arrange
            var url = "/api/pharmacy/medications?sortBy=name&sortOrder=asc";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            var names = result.Items.Select(m => m.Name).ToList();
            var sortedNames = names.OrderBy(n => n).ToList();
            Assert.Equal(sortedNames, names);
        }

        [Fact]
        public async Task GetMedications_WithMultiColumnSorting_ReturnsCorrectlySortedResults()
        {
            // Arrange
            var url = "/api/pharmacy/medications?sortBy=category,name&sortOrder=asc,asc";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            var grouped = result.Items.GroupBy(m => m.Category).ToList();
            foreach (var group in grouped)
            {
                var names = group.Select(m => m.Name).ToList();
                var sortedNames = names.OrderBy(n => n).ToList();
                Assert.Equal(sortedNames, names);
            }
        }

        [Fact]
        public async Task GetMedications_AllCombined_ReturnsCorrectResults()
        {
            // Arrange
            var url = "/api/pharmacy/medications?pageNumber=1&pageSize=10&category=antibiotics&sortBy=price&sortOrder=asc";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(10, result.PageSize);
            Assert.All(result.Items, m => Assert.Equal("antibiotics", m.Category?.ToLower()));
            var prices = result.Items.Select(m => m.Price).ToList();
            var sortedPrices = prices.OrderBy(p => p).ToList();
            Assert.Equal(sortedPrices, prices);
        }

        [Fact]
        public async Task GetMedications_EmptyResults_ReturnsEmptyPaginatedResponse()
        {
            // Arrange
            var url = "/api/pharmacy/medications?category=nonexistent";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetMedications_WithRequiresPrescriptionFilter_ReturnsOnlyMatching()
        {
            var url = "/api/pharmacy/medications?requiresPrescription=false&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.All(result.Items, m => Assert.False(m.RequiresPrescription));
        }

        [Fact]
        public async Task GetMedications_WithIsActiveFalse_ReturnsInactiveOnly()
        {
            var url = "/api/pharmacy/medications?isActive=false&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.All(result.Items, m => Assert.False(m.IsActive));
            Assert.Contains(result.Items, m => m.Name == "Discontinued Drug");
        }

        [Fact]
        public async Task GetMedications_WithStockStatusLowStock_ReturnsLowStockOnly()
        {
            var url = "/api/pharmacy/medications?stockStatus=low stock&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.All(result.Items, m =>
            {
                Assert.True(m.StockQuantity >= 5);
                Assert.True(m.StockQuantity < m.MinimumStockLevel);
            });
            Assert.Contains(result.Items, m => m.Name == "Ibuprofen");
        }

        [Fact]
        public async Task GetMedications_WithStockStatusOutOfStock_ReturnsOutOfStockOnly()
        {
            var url = "/api/pharmacy/medications?stockStatus=out of stock&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.All(result.Items, m => Assert.Equal(0, m.StockQuantity));
            Assert.Contains(result.Items, m => m.Name == "EmptyStock Med");
        }

        [Fact]
        public async Task GetMedications_WithLegacySearchParam_ReturnsFilteredResults()
        {
            var url = "/api/pharmacy/medications?search=penicillin&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.Contains(result.Items, m =>
                m.Name.Contains("Penicillin", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetMedications_CategoryAndSearchTerm_CombinesWithAndLogic()
        {
            var url = "/api/pharmacy/medications?category=antibiotics&searchTerm=amox&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Amoxicillin", result.Items[0].Name);
        }

        [Fact]
        public async Task GetMedications_PriceRangeAndCategory_ReturnsMatchingSubset()
        {
            var url = "/api/pharmacy/medications?category=painkiller&minPrice=7&maxPrice=9&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Equal(3, result.Items.Count);
            Assert.All(result.Items, m =>
            {
                Assert.Equal("painkiller", m.Category?.ToLower());
                Assert.True(m.Price >= 7 && m.Price <= 9);
            });
        }

        [Fact]
        public async Task GetMedications_FilterPlusPagination_ReturnsCorrectPageOfFilteredSet()
        {
            var url = "/api/pharmacy/medications?category=painkiller&sortBy=name&sortOrder=asc&pageNumber=1&pageSize=1";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(4, result.TotalCount);
            Assert.Equal("Aspirin", result.Items[0].Name);
        }

        [Fact]
        public async Task GetMedications_MinPriceGreaterThanMaxPrice_ReturnsBadRequest()
        {
            // Arrange
            var url = "/api/pharmacy/medications?minPrice=50&maxPrice=10";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMedications_InvalidStockStatus_ReturnsBadRequest()
        {
            // Arrange
            var url = "/api/pharmacy/medications?stockStatus=invalid";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMedications_NegativeMinPrice_ReturnsBadRequest()
        {
            // Arrange
            var url = "/api/pharmacy/medications?minPrice=-5";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMedications_InvalidPageNumber_ReturnsFirstPage()
        {
            // Arrange
            var url = "/api/pharmacy/medications?pageNumber=0&pageSize=10";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Equal(1, result.CurrentPage); // Should default to 1
        }

        [Fact]
        public async Task GetMedications_PageSizeExceedsMax_ClampsToMax()
        {
            // Arrange
            var url = "/api/pharmacy/medications?pageNumber=1&pageSize=200";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Equal(100, result.PageSize); // Should clamp to max 100
        }

        [Fact]
        public async Task GetMedications_PageSizeBelowMin_ClampsToMin()
        {
            var url = "/api/pharmacy/medications?pageNumber=1&pageSize=0";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<MedicationDto>>();
            Assert.NotNull(result);
            Assert.Equal(1, result.PageSize);
        }

        #endregion

        #region GetPrescriptions Integration Tests

        [Fact]
        public async Task GetPrescriptions_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var url = "/api/pharmacy/prescriptions?pageNumber=1&pageSize=5";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PrescriptionDto>>();
            Assert.NotNull(result);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(5, result.PageSize);
        }

        [Fact]
        public async Task GetPrescriptions_MinAmountGreaterThanMaxAmount_ReturnsBadRequest()
        {
            var url = "/api/pharmacy/prescriptions?minAmount=500&maxAmount=50";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPrescriptions_WithStatusAndAmountRange_ReturnsFilteredResults()
        {
            // Arrange
            var url = "/api/pharmacy/prescriptions?status=Pending&minAmount=50";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PrescriptionDto>>();
            Assert.NotNull(result);
            Assert.All(result.Items, p =>
            {
                Assert.Equal("Pending", p.Status);
                Assert.True(p.TotalAmount >= 50);
            });
        }

        [Fact]
        public async Task GetPrescriptions_WithSorting_ReturnsSortedResults()
        {
            // Arrange
            var url = "/api/pharmacy/prescriptions?sortBy=createdAt&sortOrder=desc";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PrescriptionDto>>();
            Assert.NotNull(result);
            var dates = result.Items.Select(p => p.CreatedAt).ToList();
            var sortedDates = dates.OrderByDescending(d => d).ToList();
            Assert.Equal(sortedDates, dates);
        }

        [Fact]
        public async Task GetPrescriptions_InvalidPageNumber_ReturnsFirstPage()
        {
            var url = "/api/pharmacy/prescriptions?pageNumber=0&pageSize=10";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PrescriptionDto>>();
            Assert.NotNull(result);
            Assert.Equal(1, result.CurrentPage);
        }

        [Fact]
        public async Task GetPrescriptions_PageSizeExceedsMax_ClampsToMax()
        {
            var url = "/api/pharmacy/prescriptions?pageNumber=1&pageSize=200";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PrescriptionDto>>();
            Assert.NotNull(result);
            Assert.Equal(100, result.PageSize);
        }

        [Fact]
        public async Task GetPrescriptions_PageSizeBelowMin_ClampsToMin()
        {
            var url = "/api/pharmacy/prescriptions?pageNumber=1&pageSize=0";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PrescriptionDto>>();
            Assert.NotNull(result);
            Assert.Equal(1, result.PageSize);
        }

        #endregion

        #region GetInventory Integration Tests

        [Fact]
        public async Task GetInventory_ReturnsPaginatedResultsWithAlerts()
        {
            // Arrange
            var url = "/api/pharmacy/inventory?pageNumber=1&pageSize=10";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("items", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("lowStockAlerts", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("expiryAlerts", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetInventory_WithCategoryAndMinStock_ReturnsFilteredActiveResults()
        {
            var url = "/api/pharmacy/inventory?category=painkiller&minStock=10&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.NotEmpty(json.Items);
            Assert.Single(json.Items);
            Assert.All(json.Items, m =>
            {
                Assert.Equal("painkiller", m.Category?.ToLower());
                Assert.True(m.StockQuantity >= 10);
                Assert.True(m.IsActive);
            });
            Assert.Equal("Aspirin", json.Items[0].Name);
        }

        [Fact]
        public async Task GetInventory_MinStockGreaterThanMaxStock_ReturnsBadRequest()
        {
            var url = "/api/pharmacy/inventory?minStock=100&maxStock=10";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetInventory_WithFiltering_ReturnsFilteredResults()
        {
            // Arrange
            var url = "/api/pharmacy/inventory?category=painkiller&minStock=5";

            // Act
            var response = await _client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("items", content);
        }

        [Fact]
        public async Task GetInventory_InvalidPageNumber_ReturnsFirstPage()
        {
            var url = "/api/pharmacy/inventory?pageNumber=-1&pageSize=10";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Equal(1, json.CurrentPage);
        }

        [Fact]
        public async Task GetInventory_PageSizeExceedsMax_ClampsToMax()
        {
            var url = "/api/pharmacy/inventory?pageNumber=1&pageSize=250";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Equal(100, json.PageSize);
        }

        [Fact]
        public async Task GetInventory_PageSizeBelowMin_ClampsToMin()
        {
            var url = "/api/pharmacy/inventory?pageNumber=1&pageSize=0";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Equal(1, json.PageSize);
        }

        [Fact]
        public async Task GetInventory_StockStatusLowStock_ReturnsLowStockActiveItems()
        {
            var url = "/api/pharmacy/inventory?stockStatus=low%20stock&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Single(json.Items);
            Assert.Equal("Ibuprofen", json.Items[0].Name);
            Assert.All(json.Items, m =>
            {
                Assert.True(m.IsActive);
                Assert.True(m.StockQuantity >= 5);
                Assert.True(m.StockQuantity < m.MinimumStockLevel);
            });
        }

        [Fact]
        public async Task GetInventory_StockStatusCriticalStock_ReturnsCriticalStockActiveItems()
        {
            var url = "/api/pharmacy/inventory?stockStatus=critical%20stock&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Single(json.Items);
            Assert.Equal("Critical Stock Med", json.Items[0].Name);
            Assert.InRange(json.Items[0].StockQuantity, 1, 4);
        }

        [Fact]
        public async Task GetInventory_StockStatusOutOfStock_ReturnsEmptyStockActiveItems()
        {
            var url = "/api/pharmacy/inventory?stockStatus=out%20of%20stock&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Single(json.Items);
            Assert.Equal("EmptyStock Med", json.Items[0].Name);
            Assert.Equal(0, json.Items[0].StockQuantity);
        }

        [Fact]
        public async Task GetInventory_ExpiryGood_IncludesMissingExpiryAndFarFutureDates()
        {
            var today = DateTime.Now.Date;
            var url = $"/api/pharmacy/inventory?expiryAfter={today.AddDays(90):yyyy-MM-dd}&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Contains(json.Items, m => m.Name == "Penicillin");
            Assert.Contains(json.Items, m => m.Name == "Expiry Good Med");
            Assert.DoesNotContain(json.Items, m => m.Name == "Expiry Warning Med");
        }

        [Fact]
        public async Task GetInventory_ExpiryWarning_ReturnsWarningBucketItems()
        {
            var today = DateTime.Now.Date;
            var url = $"/api/pharmacy/inventory?expiryAfter={today.AddDays(30):yyyy-MM-dd}&expiryBefore={today.AddDays(89):yyyy-MM-dd}&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Single(json.Items);
            Assert.Equal("Expiry Warning Med", json.Items[0].Name);
        }

        [Fact]
        public async Task GetInventory_ExpiryCritical_ReturnsCriticalBucketItems()
        {
            var today = DateTime.Now.Date;
            var url = $"/api/pharmacy/inventory?expiryAfter={today:yyyy-MM-dd}&expiryBefore={today.AddDays(29):yyyy-MM-dd}&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Single(json.Items);
            Assert.Equal("Expiry Critical Med", json.Items[0].Name);
        }

        [Fact]
        public async Task GetInventory_ExpiryExpired_ReturnsExpiredItems()
        {
            var today = DateTime.Now.Date;
            var url = $"/api/pharmacy/inventory?expiryBefore={today.AddDays(-1):yyyy-MM-dd}&pageSize=100";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.Single(json.Items);
            Assert.Equal("Expiry Expired Med", json.Items[0].Name);
        }

        #endregion

        private sealed class InventoryResponse
        {
            [JsonPropertyName("items")]
            public List<MedicationDto> Items { get; set; } = new();

            [JsonPropertyName("currentPage")]
            public int CurrentPage { get; set; }

            [JsonPropertyName("pageSize")]
            public int PageSize { get; set; }
        }

        #region Helper Methods

        private void SeedTestData()
        {
            if (_context.Medications.Any())
                return;

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
                    Name = "Discontinued Drug",
                    Category = "antibiotics",
                    Price = 20.00m,
                    StockQuantity = 30,
                    MinimumStockLevel = 10,
                    IsActive = false,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.Now.AddDays(-20)
                },
                new Medication
                {
                    Name = "EmptyStock Med",
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
        }

        #endregion

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
            _client?.Dispose();
        }
    }

    /// <summary>
    /// Custom WebApplicationFactory for integration testing
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }
    }

    /// <summary>
    /// Test authentication handler to bypass JWT authentication in tests
    /// </summary>
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, "Pharmacist")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
