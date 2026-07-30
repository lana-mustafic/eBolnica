using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy;
using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
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
        public async Task ExportMedicationsCsv_WithCategoryFilter_ReturnsFilteredCsv()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/export/csv?category=antibiotics");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Name", content);
            Assert.Contains("Penicillin", content);
            Assert.Contains("Amoxicillin", content);
            Assert.DoesNotContain("Aspirin", content);
        }

        [Fact]
        public async Task ExportMedicationsCsv_WithSearchFilter_ReturnsMatchingRows()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/export/csv?search=aspirin");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Aspirin", content);
            Assert.DoesNotContain("Penicillin", content);
        }

        [Fact]
        public async Task DownloadMedicationImportTemplate_ReturnsCsvWithHeadersAndExampleRow()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/import/template");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

            var disposition = response.Content.Headers.ContentDisposition;
            Assert.NotNull(disposition);
            Assert.Contains("medication-import-template.csv", disposition.FileName);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Name", content);
            Assert.Contains("Requires Prescription", content);
            Assert.DoesNotContain(",Status", content);
            Assert.Contains("Paracetamol (required, 3-100 characters)", content);
        }

        [Fact]
        public async Task ImportMedicationsCsv_ValidFile_ImportsRows()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Imported From Api,,Vitamins,,,11.00,30,10,{expiry},,,,No,Yes
                """;

            using var content = CreateCsvUploadContent(csv);
            var response = await _client.PostAsync("/api/pharmacy/medications/import/csv", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var summary = await response.Content.ReadFromJsonAsync<MedicationImportResultDto>();
            Assert.NotNull(summary);
            Assert.Equal(1, summary.SuccessCount);
            Assert.Equal(0, summary.FailureCount);
            Assert.True(summary.Committed);
            Assert.Single(summary.ImportedMedicationIds);
            Assert.True(_context.Medications.Any(m => m.Name == "Imported From Api"));
        }

        [Fact]
        public async Task ImportMedicationsCsv_DuplicateName_ReturnsRowFailure()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Aspirin,,Vitamins,,,11.00,30,10,{expiry},,,,No,Yes
                """;

            using var content = CreateCsvUploadContent(csv);
            var response = await _client.PostAsync("/api/pharmacy/medications/import/csv", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var summary = await response.Content.ReadFromJsonAsync<MedicationImportResultDto>();
            Assert.NotNull(summary);
            Assert.Equal(0, summary.SuccessCount);
            Assert.Equal(1, summary.FailureCount);
            Assert.True(summary.Committed);
            Assert.Contains(summary.Errors, e => e.Field == "Name");
        }

        [Fact]
        public async Task ImportMedicationsCsv_DuplicateNameWithinFile_ImportsFirstRowOnly()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var csv = $"""
                Name,Generic Name,Category,Manufacturer,Description,Price,Stock Quantity,Minimum Stock Level,Expiry Date,Batch Number,Dosage Form,Strength,Requires Prescription,Active
                Brand New Med,,Vitamins,,,11.00,30,10,{expiry},,,,No,Yes
                brand new med,,Vitamins,,,12.00,31,11,{expiry},,,,No,Yes
                """;

            using var content = CreateCsvUploadContent(csv);
            var response = await _client.PostAsync("/api/pharmacy/medications/import/csv", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var summary = await response.Content.ReadFromJsonAsync<MedicationImportResultDto>();
            Assert.NotNull(summary);
            Assert.Equal(1, summary.SuccessCount);
            Assert.Equal(1, summary.FailureCount);
            Assert.Contains(summary.Errors, e => e.Reason.Contains("Duplicate name in this import file"));
        }

        [Fact]
        public async Task ImportMedicationsCsv_MissingHeaders_ReturnsBadRequest()
        {
            using var content = CreateCsvUploadContent("Wrong,Headers\na,b");
            var response = await _client.PostAsync("/api/pharmacy/medications/import/csv", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ImportMedicationsCsv_NoFile_ReturnsBadRequest()
        {
            using var content = new MultipartFormDataContent();
            var response = await _client.PostAsync("/api/pharmacy/medications/import/csv", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

        #endregion

        #region CheckMedicationNameAvailability Integration Tests

        [Fact]
        public async Task CheckMedicationNameAvailability_ExistingName_ReturnsNotAvailable()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/check-name?name=Penicillin");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MedicationNameAvailabilityDto>();

            Assert.NotNull(result);
            Assert.False(result.IsAvailable);
        }

        [Fact]
        public async Task CheckMedicationNameAvailability_UniqueName_ReturnsAvailable()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/check-name?name=Brand%20New%20Med");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MedicationNameAvailabilityDto>();

            Assert.NotNull(result);
            Assert.True(result.IsAvailable);
        }

        [Fact]
        public async Task CheckMedicationNameAvailability_CaseInsensitiveMatch_ReturnsNotAvailable()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/check-name?name=penicillin");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MedicationNameAvailabilityDto>();

            Assert.NotNull(result);
            Assert.False(result.IsAvailable);
        }

        [Fact]
        public async Task CheckMedicationNameAvailability_WithExcludeId_ExcludesCurrentRecord()
        {
            var penicillin = await _context.Medications.FirstAsync(m => m.Name == "Penicillin");

            var response = await _client.GetAsync(
                $"/api/pharmacy/medications/check-name?name=Penicillin&excludeId={penicillin.Id}");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MedicationNameAvailabilityDto>();

            Assert.NotNull(result);
            Assert.True(result.IsAvailable);
        }

        [Fact]
        public async Task CheckMedicationNameAvailability_WithExcludeId_StillDetectsOtherDuplicate()
        {
            var amoxicillin = await _context.Medications.FirstAsync(m => m.Name == "Amoxicillin");

            var response = await _client.GetAsync(
                $"/api/pharmacy/medications/check-name?name=Penicillin&excludeId={amoxicillin.Id}");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MedicationNameAvailabilityDto>();

            Assert.NotNull(result);
            Assert.False(result.IsAvailable);
        }

        [Fact]
        public async Task CheckMedicationNameAvailability_EmptyName_ReturnsBadRequest()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/check-name?name=");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetMedicationAutocompleteSuggestions_ReturnsUpToTenMatches()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/autocomplete?q=in&limit=10");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var suggestions = await response.Content.ReadFromJsonAsync<List<MedicationAutocompleteSuggestionDto>>();
            Assert.NotNull(suggestions);
            Assert.True(suggestions.Count > 0);
            Assert.True(suggestions.Count <= 10);
            Assert.All(suggestions, s => Assert.False(string.IsNullOrWhiteSpace(s.Name)));
            Assert.DoesNotContain(suggestions, s => s.Name == "Discontinued Drug");
            Assert.Contains(suggestions, s => !string.IsNullOrWhiteSpace(s.Category) || !string.IsNullOrWhiteSpace(s.Manufacturer));
        }

        [Fact]
        public async Task GetMedicationAutocompleteSuggestions_ShortQuery_ReturnsEmptyList()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/autocomplete?q=a");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var suggestions = await response.Content.ReadFromJsonAsync<List<MedicationAutocompleteSuggestionDto>>();
            Assert.NotNull(suggestions);
            Assert.Empty(suggestions);
        }

        [Fact]
        public async Task GetMedicationAutocompleteSuggestions_RespectsLimitCap()
        {
            var response = await _client.GetAsync("/api/pharmacy/medications/autocomplete?q=e&limit=25");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var suggestions = await response.Content.ReadFromJsonAsync<List<MedicationAutocompleteSuggestionDto>>();
            Assert.NotNull(suggestions);
            Assert.True(suggestions.Count <= 10);
        }

        #endregion

        #region GetMedications Validation Integration Tests

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
        public async Task GetMedications_InvalidSortColumn_ReturnsBadRequest()
        {
            var url = "/api/pharmacy/medications?sortBy=invalidColumn&sortOrder=asc";

            var response = await _client.GetAsync(url);

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
        public async Task GetPrescriptions_InvalidSortColumn_ReturnsBadRequest()
        {
            var url = "/api/pharmacy/prescriptions?sortBy=patientName&sortOrder=asc";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
            var json = await response.Content.ReadFromJsonAsync<InventoryResponse>();
            Assert.NotNull(json);
            Assert.NotNull(json.Items);
            Assert.True(json.TotalCount > 0);
            Assert.NotNull(json.LowStockAlerts);
            Assert.NotNull(json.ExpiryAlerts);
        }

        [Fact]
        public async Task GetInventory_ResponseShape_MatchesFrontendContract()
        {
            var url = "/api/pharmacy/inventory?pageNumber=1&pageSize=5";

            var response = await _client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            using var document = System.Text.Json.JsonDocument.Parse(content);
            var root = document.RootElement;

            Assert.True(root.TryGetProperty("items", out _));
            Assert.True(root.TryGetProperty("totalCount", out var totalCount));
            Assert.True(root.TryGetProperty("totalPages", out _));
            Assert.True(root.TryGetProperty("currentPage", out var currentPage));
            Assert.True(root.TryGetProperty("pageSize", out var pageSize));
            Assert.True(root.TryGetProperty("hasNext", out _));
            Assert.True(root.TryGetProperty("hasPrevious", out _));
            Assert.True(root.TryGetProperty("lowStockAlerts", out _));
            Assert.True(root.TryGetProperty("expiryAlerts", out _));

            Assert.Equal(1, currentPage.GetInt32());
            Assert.Equal(5, pageSize.GetInt32());
            Assert.True(totalCount.GetInt32() > 0);
        }

        [Fact]
        public async Task GetInventory_PageOneAndPageTwo_ReturnDifferentItemSets()
        {
            const int pageSize = 3;
            var sortQuery = "sortBy=name&sortOrder=asc";

            var pageOneResponse = await _client.GetAsync(
                $"/api/pharmacy/inventory?pageNumber=1&pageSize={pageSize}&{sortQuery}");
            var pageTwoResponse = await _client.GetAsync(
                $"/api/pharmacy/inventory?pageNumber=2&pageSize={pageSize}&{sortQuery}");

            Assert.Equal(HttpStatusCode.OK, pageOneResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, pageTwoResponse.StatusCode);

            var pageOne = await pageOneResponse.Content.ReadFromJsonAsync<InventoryResponse>();
            var pageTwo = await pageTwoResponse.Content.ReadFromJsonAsync<InventoryResponse>();

            Assert.NotNull(pageOne);
            Assert.NotNull(pageTwo);
            Assert.True(pageOne.TotalCount > pageSize);
            Assert.Equal(pageOne.TotalCount, pageTwo.TotalCount);
            Assert.Equal(1, pageOne.CurrentPage);
            Assert.Equal(2, pageTwo.CurrentPage);
            Assert.Equal(pageSize, pageOne.Items.Count);
            Assert.Equal(pageSize, pageTwo.Items.Count);

            var pageOneIds = pageOne.Items.Select(m => m.Id).ToHashSet();
            var pageTwoIds = pageTwo.Items.Select(m => m.Id).ToHashSet();
            Assert.Empty(pageOneIds.Intersect(pageTwoIds));
        }

        [Fact]
        public async Task GetInventory_FilterReducesResultSet()
        {
            var unfilteredResponse = await _client.GetAsync("/api/pharmacy/inventory?pageSize=100");
            var filteredResponse = await _client.GetAsync("/api/pharmacy/inventory?category=painkiller&pageSize=100");

            Assert.Equal(HttpStatusCode.OK, unfilteredResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);

            var unfiltered = await unfilteredResponse.Content.ReadFromJsonAsync<InventoryResponse>();
            var filtered = await filteredResponse.Content.ReadFromJsonAsync<InventoryResponse>();

            Assert.NotNull(unfiltered);
            Assert.NotNull(filtered);
            Assert.True(unfiltered.TotalCount > filtered.TotalCount);
            Assert.True(filtered.TotalCount > 0);
            Assert.All(filtered.Items, m => Assert.Equal("painkiller", m.Category?.ToLower()));
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
        public async Task GetInventory_InvalidSortColumn_ReturnsBadRequest()
        {
            var url = "/api/pharmacy/inventory?sortBy=supplier&sortOrder=asc";

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

        #region Medication Create/Update Integration Tests

        [Fact]
        public async Task CreateMedication_DuplicateName_Returns409Conflict()
        {
            var dto = CreateValidMedicationDto("Penicillin");

            var response = await _client.PostAsJsonAsync("/api/pharmacy/medications", dto);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<DuplicateNameErrorResponse>();
            Assert.NotNull(body);
            Assert.Equal("Medication name already exists", body!.Message);
        }

        [Fact]
        public async Task CreateMedication_CaseInsensitiveDuplicateName_Returns409Conflict()
        {
            var dto = CreateValidMedicationDto("  penicillin  ");

            var response = await _client.PostAsJsonAsync("/api/pharmacy/medications", dto);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CreateMedication_UniqueName_ReturnsCreated()
        {
            var dto = CreateValidMedicationDto("Unique Server Med");

            var response = await _client.PostAsJsonAsync("/api/pharmacy/medications", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.True(_context.Medications.Any(m => m.Name == "Unique Server Med"));
        }

        [Fact]
        public async Task UpdateMedication_SameNameOnCurrentRecord_ReturnsOk()
        {
            var aspirin = await _context.Medications.FirstAsync(m => m.Name == "Aspirin");
            var dto = CreateValidMedicationDto("Aspirin");

            var response = await _client.PutAsJsonAsync($"/api/pharmacy/medications/{aspirin.Id}", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateMedication_RenameToExistingOtherName_Returns409Conflict()
        {
            var aspirin = await _context.Medications.FirstAsync(m => m.Name == "Aspirin");
            var dto = CreateValidMedicationDto("Penicillin");

            var response = await _client.PutAsJsonAsync($"/api/pharmacy/medications/{aspirin.Id}", dto);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<DuplicateNameErrorResponse>();
            Assert.NotNull(body);
            Assert.Equal("Medication name already exists", body!.Message);
        }

        [Fact]
        public async Task SeedTestData_HasNoDuplicateMedicationNames()
        {
            var checker = new MedicationImportDuplicateChecker(_context);
            var duplicates = await checker.FindExistingDuplicateNamesAsync();

            Assert.Empty(duplicates);
        }

        #endregion

        #region UploadMedicationImage Integration Tests

        [Fact]
        public async Task UploadMedicationImage_SequentialRequests_AllSucceedAndAccumulate()
        {
            var medication = await _context.Medications.FirstAsync(m => m.Name == "Penicillin");
            var uploadUrl = $"/api/pharmacy/medications/{medication.Id}/images";
            var uploaded = new List<MedicationImageDto>();

            for (var i = 0; i < 3; i++)
            {
                using var content = CreateMedicationImageUploadContent($"image-{i + 1}.jpg");
                var response = await _client.PostAsync(uploadUrl, content);

                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var image = await response.Content.ReadFromJsonAsync<MedicationImageDto>();
                Assert.NotNull(image);
                uploaded.Add(image);
            }

            Assert.Equal(3, uploaded.Count);
            Assert.All(uploaded, img => Assert.Equal(medication.Id, img.MedicationId));
            Assert.True(uploaded[0].IsPrimary);
            Assert.False(uploaded[1].IsPrimary);
            Assert.False(uploaded[2].IsPrimary);
            Assert.Equal(new[] { 0, 1, 2 }, uploaded.Select(img => img.SortOrder).ToArray());
            Assert.Equal(3, uploaded.Select(img => img.Id).Distinct().Count());

            var listResponse = await _client.GetAsync(uploadUrl);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var images = await listResponse.Content.ReadFromJsonAsync<List<MedicationImageDto>>();
            Assert.NotNull(images);
            Assert.Equal(3, images.Count);
        }

        [Fact]
        public async Task UploadMedicationImage_SequentialRequests_AssignsDistinctStoredFiles()
        {
            var medication = await _context.Medications.FirstAsync(m => m.Name == "Aspirin");
            var uploadUrl = $"/api/pharmacy/medications/{medication.Id}/images";
            var imageUrls = new List<string>();

            for (var i = 0; i < 2; i++)
            {
                using var content = CreateMedicationImageUploadContent($"batch-{i + 1}.jpg");
                var response = await _client.PostAsync(uploadUrl, content);

                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var image = await response.Content.ReadFromJsonAsync<MedicationImageDto>();
                Assert.NotNull(image);
                imageUrls.Add(image.ImageUrl);
            }

            Assert.Equal(2, imageUrls.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public async Task UploadMedicationImage_MultipartFormData_ReturnsCreatedWithOptimizedMetadata()
        {
            var medication = await _context.Medications.FirstAsync(m => m.Name == "Penicillin");
            var uploadUrl = $"/api/pharmacy/medications/{medication.Id}/images";

            using var content = CreateMedicationImageUploadContent("optimized-check.jpg");
            var response = await _client.PostAsync(uploadUrl, content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Contains("multipart/form-data", content.Headers.ContentType?.MediaType ?? string.Empty);

            var image = await response.Content.ReadFromJsonAsync<MedicationImageDto>();
            Assert.NotNull(image);
            Assert.Equal(medication.Id, image.MedicationId);
            Assert.Equal("optimized-check.jpg", image.FileName);
            Assert.False(string.IsNullOrWhiteSpace(image.ImageUrl));
            Assert.False(string.IsNullOrWhiteSpace(image.ThumbnailUrl));
            Assert.True(image.FileSizeBytes > 0);
            Assert.True(image.Width > 0);
            Assert.True(image.Height > 0);
        }

        [Fact]
        public async Task UploadMedicationImage_MissingFileField_ReturnsBadRequest()
        {
            var medication = await _context.Medications.FirstAsync(m => m.Name == "Aspirin");
            var uploadUrl = $"/api/pharmacy/medications/{medication.Id}/images";

            using var content = new MultipartFormDataContent();
            var response = await _client.PostAsync(uploadUrl, content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadMedicationImage_WrongFormFieldName_ReturnsBadRequest()
        {
            var medication = await _context.Medications.FirstAsync(m => m.Name == "Aspirin");
            var uploadUrl = $"/api/pharmacy/medications/{medication.Id}/images";
            var bytes = CreateMinimalJpegBytes();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            using var content = new MultipartFormDataContent();
            content.Add(fileContent, "image", "wrong-field.jpg");

            var response = await _client.PostAsync(uploadUrl, content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UploadMedicationImage_EmptyMultipartFile_ReturnsBadRequest()
        {
            var medication = await _context.Medications.FirstAsync(m => m.Name == "Ibuprofen");
            var uploadUrl = $"/api/pharmacy/medications/{medication.Id}/images";
            var fileContent = new ByteArrayContent(Array.Empty<byte>());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            using var content = new MultipartFormDataContent();
            content.Add(fileContent, "file", "empty.jpg");

            var response = await _client.PostAsync(uploadUrl, content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("No file uploaded", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReorderMedicationImages_UpdatesSortOrderAndPreservesPrimary()
        {
            var medication = await _context.Medications.FirstAsync(m => m.Name == "Ibuprofen");
            var uploadUrl = $"/api/pharmacy/medications/{medication.Id}/images";
            var uploaded = new List<MedicationImageDto>();

            for (var i = 0; i < 3; i++)
            {
                using var content = CreateMedicationImageUploadContent($"reorder-{i + 1}.jpg");
                var response = await _client.PostAsync(uploadUrl, content);
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);

                var image = await response.Content.ReadFromJsonAsync<MedicationImageDto>();
                Assert.NotNull(image);
                uploaded.Add(image);
            }

            var primaryId = uploaded.Single(image => image.IsPrimary).Id;
            var reorderedIds = new[] { uploaded[2].Id, uploaded[0].Id, uploaded[1].Id };

            var reorderResponse = await _client.PutAsJsonAsync(
                $"/api/pharmacy/medications/{medication.Id}/images/reorder",
                new { imageIds = reorderedIds });

            Assert.Equal(HttpStatusCode.OK, reorderResponse.StatusCode);
            var reordered = await reorderResponse.Content.ReadFromJsonAsync<List<MedicationImageDto>>();
            Assert.NotNull(reordered);
            Assert.Equal(reorderedIds, reordered.Select(image => image.Id).ToArray());
            Assert.Equal(new[] { 0, 1, 2 }, reordered.Select(image => image.SortOrder).ToArray());
            Assert.True(reordered.Single(image => image.Id == primaryId).IsPrimary);

            var listResponse = await _client.GetAsync(uploadUrl);
            var persisted = await listResponse.Content.ReadFromJsonAsync<List<MedicationImageDto>>();
            Assert.NotNull(persisted);
            Assert.Equal(reorderedIds, persisted.Select(image => image.Id).ToArray());
        }

        #endregion

        #region GenerateMedicationAiSummary Integration Tests

        [Fact]
        public async Task GenerateMedicationAiSummary_ReturnsStructuredSummary()
        {
            var medication = await _context.Medications.FirstAsync(m => m.Name == "Ibuprofen");

            var response = await _client.PostAsync(
                $"/api/pharmacy/medications/{medication.Id}/ai-summary",
                JsonContent.Create(new { }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var summary = await response.Content.ReadFromJsonAsync<MedicationAiSummaryDto>();
            Assert.NotNull(summary);
            Assert.Equal("Test overview", summary.Overview);
            Assert.Equal("Test usage notes", summary.UsageNotes);
            Assert.Equal("Test stock alert", summary.StockExpiryAlert);
            Assert.Equal("Test prescription info", summary.PrescriptionRequirement);
            await AssertResponseDoesNotExposeSecrets(response);
        }

        [Fact]
        public async Task GenerateMedicationAiSummary_UnknownMedication_ReturnsNotFoundWithoutSecrets()
        {
            var response = await _client.PostAsync(
                "/api/pharmacy/medications/999999/ai-summary",
                JsonContent.Create(new { }));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            await AssertResponseDoesNotExposeSecrets(response);
        }

        [Fact]
        public async Task GenerateMedicationAiSummary_WhenServiceNotConfigured_ReturnsServiceUnavailable()
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["MedicationAiSummary:Enabled"] = "true",
                        ["MedicationAiSummary:ApiKey"] = string.Empty
                    });
                });
            });

            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

            var medication = await _context.Medications.FirstAsync(m => m.Name == "Ibuprofen");
            var response = await client.PostAsync(
                $"/api/pharmacy/medications/{medication.Id}/ai-summary",
                JsonContent.Create(new { }));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            await AssertResponseDoesNotExposeSecrets(response);
        }

        #endregion

        #region Helper Methods

        private static async Task AssertResponseDoesNotExposeSecrets(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("ApiKey", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api-key", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Bearer ", body, StringComparison.Ordinal);
            Assert.DoesNotContain("sk-", body, StringComparison.Ordinal);
            Assert.False(response.Headers.Contains("api-key"));
        }

        private static MedicationCreateDto CreateValidMedicationDto(string name) =>
            new()
            {
                Name = name,
                Category = "Other",
                Price = 9.99m,
                StockQuantity = 10,
                MinimumStockLevel = 5,
                ExpiryDate = DateTime.Now.AddYears(1),
                IsActive = true,
                RequiresPrescription = false
            };

        private sealed class DuplicateNameErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }

        private static MultipartFormDataContent CreateCsvUploadContent(string csv, string fileName = "medications.csv")
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");

            var form = new MultipartFormDataContent();
            form.Add(fileContent, "file", fileName);
            return form;
        }

        private static MultipartFormDataContent CreateMedicationImageUploadContent(string fileName)
        {
            var bytes = CreateMinimalJpegBytes();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            var form = new MultipartFormDataContent();
            form.Add(fileContent, "file", fileName);
            return form;
        }

        private static byte[] CreateMinimalJpegBytes()
        {
            using var image = new Image<Rgba32>(4, 4);
            using var ms = new MemoryStream();
            image.SaveAsJpeg(ms, new JpegEncoder());
            return ms.ToArray();
        }

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

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MedicationAiSummary:Enabled"] = "true",
                    ["MedicationAiSummary:ApiKey"] = "integration-test-key"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                services.RemoveAll<IMedicationAiSummaryClient>();
                services.AddSingleton<IMedicationAiSummaryClient, TestMedicationAiSummaryClient>();
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
