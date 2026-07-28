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
        public async Task GetPrescriptions_WithFiltering_ReturnsFilteredResults()
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
            Assert.Contains("items", content);
            Assert.Contains("LowStockAlerts", content);
            Assert.Contains("ExpiryAlerts", content);
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

        #endregion

        #region Helper Methods

        private void SeedTestData()
        {
            if (_context.Medications.Any())
                return;

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
            builder.ConfigureServices(services =>
            {
                // Remove the real database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                });

                // Add test authentication handler to bypass JWT requirement
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
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
