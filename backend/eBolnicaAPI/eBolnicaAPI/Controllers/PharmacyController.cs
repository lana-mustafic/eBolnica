using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services;
using eBolnicaAPI.Services.Pharmacy;
using eBolnicaAPI.Services.Pharmacy.MedicationAiSummary;
using eBolnicaAPI.Services.Pharmacy.MedicationImages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace eBolnicaAPI.Controllers
{
    [Route("api/pharmacy")]
    [ApiController]
    public class PharmacyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPharmacyService _pharmacyService;
        private readonly IPdfReportService _pdfReportService;
        private readonly IPharmacyAnalyticsService _analyticsService;
        private readonly IMedicationImageService _medicationImageService;
        private readonly IMedicationAiSummaryService _medicationAiSummaryService;
        private readonly IMedicationCsvExportService _medicationCsvExportService;
        private readonly IMedicationCsvImportService _medicationCsvImportService;
        private readonly IMedicationImportDuplicateChecker _medicationDuplicateChecker;
        private readonly IMedicationAutocompleteService _medicationAutocompleteService;
        private readonly ILogger<PharmacyController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public PharmacyController(
            AppDbContext context, 
            UserManager<AppUser> userManager, 
            IPharmacyService pharmacyService,
            IPdfReportService pdfReportService,
            IPharmacyAnalyticsService analyticsService,
            IMedicationImageService medicationImageService,
            IMedicationAiSummaryService medicationAiSummaryService,
            IMedicationCsvExportService medicationCsvExportService,
            IMedicationCsvImportService medicationCsvImportService,
            IMedicationImportDuplicateChecker medicationDuplicateChecker,
            IMedicationAutocompleteService medicationAutocompleteService,
            ILogger<PharmacyController> logger,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _pharmacyService = pharmacyService;
            _pdfReportService = pdfReportService;
            _analyticsService = analyticsService;
            _medicationImageService = medicationImageService;
            _medicationAiSummaryService = medicationAiSummaryService;
            _medicationCsvExportService = medicationCsvExportService;
            _medicationCsvImportService = medicationCsvImportService;
            _medicationDuplicateChecker = medicationDuplicateChecker;
            _medicationAutocompleteService = medicationAutocompleteService;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
        }

        #region Medications CRUD

        /// <summary>
        /// Get paginated medications with filtering and sorting support
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of medications with support for:
        /// - **Pagination**: pageNumber (default: 1), pageSize (default: 10, max: 100)
        /// - **Filtering**: category, search, stockStatus, minPrice, maxPrice, isActive, requiresPrescription, minStock, maxStock, createdAfter, createdBefore, expiryAfter, expiryBefore
        /// - **Sorting**: sortBy (single or comma-separated), sortOrder (asc/desc or comma-separated)
        /// 
        /// **Filter Examples:**
        /// - Single filter: ?category=antibiotics
        /// - Multiple filters: ?category=antibiotics&amp;minPrice=10&amp;maxPrice=50&amp;isActive=true
        /// - Search + filters: ?search=penicillin&amp;category=antibiotics&amp;stockStatus=InStock
        /// 
        /// **Sorting Examples:**
        /// - Single column: ?sortBy=name&amp;sortOrder=asc
        /// - Multi-column: ?sortBy=category,name&amp;sortOrder=asc,asc
        /// - Embedded order: ?sortBy=name:asc,price:desc
        /// 
        /// **Combined Example:**
        /// ?pageNumber=1&amp;pageSize=10&amp;category=antibiotics&amp;minPrice=10&amp;sortBy=price&amp;sortOrder=asc
        /// </remarks>
        /// <param name="category">Filter by medication category (exact match, case-insensitive). Example: "antibiotics"</param>
        /// <param name="search">Search term across name, generic name, and manufacturer (case-insensitive). Example: "penicillin"</param>
        /// <param name="stockStatus">Filter by stock status: "low stock", "out of stock", "normal stock", or "InStock". Example: "InStock"</param>
        /// <param name="requiresPrescription">Filter by prescription requirement. Example: true</param>
        /// <param name="isActive">Filter by active status. Default: true (shows only active when not specified). Example: true</param>
        /// <param name="page">Page number for backward compatibility (use pageNumber instead). Default: 1</param>
        /// <param name="pageNumber">Page number (1-based). Default: 1, Minimum: 1</param>
        /// <param name="pageSize">Number of items per page. Default: 10, Range: 1-100</param>
        /// <param name="sortBy">Field(s) to sort by. Single: "name", Multi: "name,price", Embedded: "name:asc,price:desc". Supported: name, price, createdAt, stockQuantity, category, expiryDate</param>
        /// <param name="sortOrder">Sort order(s). Single: "asc", Multi: "asc,desc". Default: "desc"</param>
        /// <returns>Paginated response containing medications and pagination metadata</returns>
        /// <response code="200">Returns paginated medications successfully</response>
        /// <response code="400">Invalid query parameters</response>
        /// <response code="401">Unauthorized - JWT token required</response>
        /// <response code="403">Forbidden - Pharmacist role required</response>
        [HttpGet("medications")]
        [Authorize(Roles = "Pharmacist")]
        [ProducesResponseType(typeof(PaginatedResponse<MedicationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMedications(
            [FromQuery] PharmacyQueryParameters parameters,
            [FromQuery(Name = "search")] string? search = null,
            [FromQuery] int page = 1)
        {
            var stopwatch = Stopwatch.StartNew();

            ApplyLegacyMedicationQueryAliases(parameters, search, page);

            var filterValidationError = ValidateFilterParameters(parameters, PharmacyListEndpoint.Medications);
            if (filterValidationError != null)
            {
                return filterValidationError;
            }

            NormalizePagination(parameters);

            var query = _context.Medications.AsNoTracking().AsQueryable();
            var defaultActiveOnly = !Request.Query.ContainsKey("isActive");
            query = _pharmacyService.GetFilteredMedications(query, parameters, defaultActiveOnly);

            // Get total count BEFORE pagination (for performance optimization)
            var totalCount = await query.CountAsync();

            var currentPage = parameters.PageNumber;
            var pageSize = parameters.PageSize;

            // Edge case: Handle empty results
            if (totalCount == 0)
            {
                return Ok(new PaginatedResponse<MedicationDto>(
                    new List<MedicationDto>(),
                    totalCount: 0,
                    pageNumber: currentPage,
                    pageSize: pageSize
                ));
            }

            // Edge case: Handle pageNumber out of range - adjust to last valid page
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (currentPage > totalPages && totalPages > 0)
            {
                currentPage = totalPages;
            }

            query = _pharmacyService.ApplySorting(query, parameters.SortBy, parameters.SortOrder);

            // Calculate Skip and Take values for server-side pagination
            // Skip = (pageNumber - 1) * pageSize
            var skipValue = (currentPage - 1) * pageSize;
            var takeValue = pageSize;

            // Apply pagination with projection to DTO at database level for optimal performance
            // This reduces memory usage and improves query performance
            var dtoList = await query
                .Skip(skipValue)
                .Take(takeValue)
                .Select(m => new MedicationDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    GenericName = m.GenericName,
                    Description = m.Description,
                    Manufacturer = m.Manufacturer,
                    Price = m.Price,
                    StockQuantity = m.StockQuantity,
                    MinimumStockLevel = m.MinimumStockLevel,
                    ExpiryDate = m.ExpiryDate,
                    BatchNumber = m.BatchNumber,
                    IsActive = m.IsActive,
                    RequiresPrescription = m.RequiresPrescription,
                    Category = m.Category,
                    DosageForm = m.DosageForm,
                    Strength = m.Strength,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    PrimaryImageUrl = m.ImageUrl ?? m.Images
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => i.RelativeUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            stopwatch.Stop();

            // Log performance metrics
            var activeFilters = CountMedicationFilters(parameters);
            _logger.LogInformation(
                "Medications query executed in {ElapsedMs}ms. Filters: {FilterCount}, Results: {ResultCount}, Page: {Page}, PageSize: {PageSize}",
                stopwatch.ElapsedMilliseconds,
                activeFilters,
                dtoList.Count,
                currentPage,
                pageSize);

            // Return paginated response using PaginatedResponse<T> class
            var response = new PaginatedResponse<MedicationDto>(
                dtoList,
                totalCount,
                currentPage,
                pageSize
            );

            return Ok(response);
        }

        /// <summary>
        /// Export medications matching the current filters as CSV.
        /// </summary>
        /// <remarks>
        /// Accepts the same filter and sort query parameters as GET /medications.
        /// Pagination parameters are ignored — all matching rows are exported (up to 10,000).
        /// </remarks>
        [HttpGet("medications/export/csv")]
        [Authorize(Roles = "Pharmacist")]
        [Produces("text/csv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportMedicationsCsv(
            [FromQuery] PharmacyQueryParameters parameters,
            [FromQuery(Name = "search")] string? search = null,
            [FromQuery] int page = 1)
        {
            ApplyLegacyMedicationQueryAliases(parameters, search, page);

            var filterValidationError = ValidateFilterParameters(parameters, PharmacyListEndpoint.Medications);
            if (filterValidationError != null)
            {
                return filterValidationError;
            }

            var query = _context.Medications.AsNoTracking().AsQueryable();
            var defaultActiveOnly = !Request.Query.ContainsKey("isActive");
            query = _pharmacyService.GetFilteredMedications(query, parameters, defaultActiveOnly);
            query = _pharmacyService.ApplySorting(query, parameters.SortBy, parameters.SortOrder);

            var totalCount = await query.CountAsync();
            if (totalCount > _medicationCsvExportService.MaxExportRows)
            {
                return BadRequest(new
                {
                    error = $"Export is limited to {_medicationCsvExportService.MaxExportRows} rows. Refine your filters and try again."
                });
            }

            var medications = await query.Take(_medicationCsvExportService.MaxExportRows).ToListAsync();
            var csvContent = _medicationCsvExportService.BuildCsv(medications);
            var fileName = _medicationCsvExportService.GetExportFileName();

            return ReturnCsvFile(Encoding.UTF8.GetBytes(csvContent), fileName);
        }

        /// <summary>
        /// Download the medication import CSV template (headers + example row with validation hints).
        /// </summary>
        [HttpGet("medications/import/template")]
        [Authorize(Roles = "Pharmacist")]
        [Produces("text/csv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult DownloadMedicationImportTemplate()
        {
            var csvContent = _medicationCsvExportService.BuildImportTemplateCsv();
            var fileName = _medicationCsvExportService.GetImportTemplateFileName();

            return ReturnCsvFile(Encoding.UTF8.GetBytes(csvContent), fileName);
        }

        /// <summary>
        /// Import medications from an uploaded CSV file (multipart/form-data field: file).
        /// </summary>
        /// <remarks>
        /// Returns row-level success/failure counts. File-level problems (missing file, malformed CSV,
        /// invalid headers) return 400.
        /// </remarks>
        [HttpPost("medications/import/csv")]
        [Authorize(Roles = "Pharmacist")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(MedicationImportResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ImportMedicationsCsv([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            var (fileError, result) = await _medicationCsvImportService.ImportAsync(file, cancellationToken);
            if (fileError != null)
            {
                return BadRequest(new { error = fileError });
            }

            return Ok(result);
        }

        /// <summary>
        /// Check whether a medication name is available (case-insensitive).
        /// </summary>
        /// <param name="name">Medication name to check</param>
        /// <param name="excludeId">Optional medication ID to exclude (edit mode — current record)</param>
        [HttpGet("medications/check-name")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(MedicationNameAvailabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckMedicationNameAvailability(
            [FromQuery] string name,
            [FromQuery] int? excludeId = null)
        {
            try
            {
                var isAvailable = await _medicationDuplicateChecker.IsNameAvailableAsync(name, excludeId);

                return Ok(new MedicationNameAvailabilityDto
                {
                    IsAvailable = isAvailable
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Returns up to 10 medication name suggestions for search autocomplete.
        /// </summary>
        /// <param name="q">Search query (minimum 2 characters)</param>
        /// <param name="limit">Maximum suggestions to return (1-10, default 10)</param>
        [HttpGet("medications/autocomplete")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(IReadOnlyList<MedicationAutocompleteSuggestionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMedicationAutocomplete(
            [FromQuery] string q,
            [FromQuery] int limit = MedicationAutocompleteService.MaxSuggestions,
            CancellationToken cancellationToken = default)
        {
            var suggestions = await _medicationAutocompleteService.GetSuggestionsAsync(
                q,
                limit,
                cancellationToken);

            return Ok(suggestions);
        }

        [HttpGet("medications/{id}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetMedication(int id)
        {
            var medication = await _context.Medications
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medication == null)
            {
                return NotFound("Medication not found");
            }

            var images = await _medicationImageService.GetImagesAsync(id);

            var dto = new MedicationDto
            {
                Id = medication.Id,
                Name = medication.Name,
                GenericName = medication.GenericName,
                Description = medication.Description,
                Manufacturer = medication.Manufacturer,
                Price = medication.Price,
                StockQuantity = medication.StockQuantity,
                MinimumStockLevel = medication.MinimumStockLevel,
                ExpiryDate = medication.ExpiryDate,
                BatchNumber = medication.BatchNumber,
                IsActive = medication.IsActive,
                RequiresPrescription = medication.RequiresPrescription,
                Category = medication.Category,
                DosageForm = medication.DosageForm,
                Strength = medication.Strength,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt,
                PrimaryImageUrl = medication.ImageUrl
                    ?? images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? images.FirstOrDefault()?.ImageUrl,
                Images = images
            };

            return Ok(dto);
        }

        /// <summary>
        /// Get all images for a medication, including metadata (filename, size, upload date, dimensions).
        /// </summary>
        /// <param name="id">Medication identifier</param>
        /// <response code="200">Returns the medication image list</response>
        /// <response code="404">Medication not found</response>
        [HttpGet("medications/{id}/images")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(IEnumerable<MedicationImageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<MedicationImageDto>>> GetMedicationImages(int id)
        {
            try
            {
                var images = await _medicationImageService.GetImagesAsync(id);
                return Ok(images);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Medication not found");
            }
        }

        /// <summary>
        /// Upload an image for a medication.
        /// Validates file type and size, scans content, optimizes the image, and stores it securely.
        /// </summary>
        /// <param name="id">Medication identifier</param>
        /// <param name="file">Image file (JPG, PNG, or WEBP, max 5MB)</param>
        /// <returns>Created medication image metadata</returns>
        /// <response code="201">Image uploaded successfully</response>
        /// <response code="400">Validation failed</response>
        /// <response code="403">Security scan failed</response>
        /// <response code="404">Medication not found</response>
        [HttpPost("medications/{id}/images")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [ProducesResponseType(typeof(MedicationImageDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadMedicationImage(int id, [FromForm] IFormFile file)
        {
            try
            {
                var image = await _medicationImageService.UploadImageAsync(id, file);
                return CreatedAtAction(nameof(GetMedicationImages), new { id }, image);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Medication not found");
            }
            catch (MedicationImageValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (MedicationImageSecurityException ex)
            {
                _logger.LogWarning("Medication image upload blocked by security scan. MedicationId={MedicationId}, Reason={Reason}", id, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (MedicationImageUploadException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Set the primary image for a medication
        /// </summary>
        [HttpPut("medications/{id}/images/{imageId}/primary")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> SetPrimaryMedicationImage(int id, int imageId)
        {
            try
            {
                await _medicationImageService.SetPrimaryImageAsync(id, imageId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Delete a medication image
        /// </summary>
        [HttpDelete("medications/{id}/images/{imageId}")]
        [Authorize(Roles = "Pharmacist,Admin")]
        public async Task<IActionResult> DeleteMedicationImage(int id, int imageId)
        {
            try
            {
                await _medicationImageService.DeleteImageAsync(id, imageId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Reorder medication gallery images by the provided image id sequence.
        /// </summary>
        [HttpPut("medications/{id}/images/reorder")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(IEnumerable<MedicationImageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<MedicationImageDto>>> ReorderMedicationImages(
            int id,
            [FromBody] MedicationImageReorderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var images = await _medicationImageService.ReorderImagesAsync(id, request.ImageIds);
                return Ok(images);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Medication not found");
            }
            catch (MedicationImageValidationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Generate a structured AI summary for a medication using stored inventory fields only.
        /// </summary>
        /// <param name="id">Medication identifier</param>
        /// <param name="cancellationToken">Request cancellation token</param>
        /// <response code="200">Returns the AI-generated summary</response>
        /// <response code="404">Medication not found</response>
        /// <response code="503">AI summary service unavailable</response>
        [HttpPost("medications/{id}/ai-summary")]
        [Authorize(Roles = "Pharmacist")]
        [ProducesResponseType(typeof(MedicationAiSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<MedicationAiSummaryDto>> GenerateMedicationAiSummary(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var summary = await _medicationAiSummaryService.GenerateSummaryAsync(id, cancellationToken);
                return Ok(summary);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Medication not found");
            }
            catch (MedicationAiSummaryUnavailableException ex)
            {
                _logger.LogWarning(ex, "AI summary unavailable for MedicationId={MedicationId}", id);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
            }
        }

        [HttpPost("medications")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(MedicationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateMedication([FromBody] MedicationCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.ExpiryDate <= DateTime.Now)
            {
                return BadRequest("Expiry date must be in the future");
            }

            var nameConflict = await ValidateMedicationNameAvailabilityAsync(dto.Name);
            if (nameConflict != null)
            {
                return nameConflict;
            }

            var medication = new Medication
            {
                Name = dto.Name,
                GenericName = dto.GenericName,
                Description = dto.Description,
                Manufacturer = dto.Manufacturer,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                MinimumStockLevel = dto.MinimumStockLevel,
                ExpiryDate = dto.ExpiryDate,
                BatchNumber = dto.BatchNumber,
                IsActive = dto.IsActive,
                RequiresPrescription = dto.RequiresPrescription,
                Category = dto.Category,
                DosageForm = dto.DosageForm,
                Strength = dto.Strength,
                CreatedAt = DateTime.Now
            };

            _context.Medications.Add(medication);
            await _context.SaveChangesAsync();
            _analyticsService.InvalidateAnalyticsCache();

            var resultDto = new MedicationDto
            {
                Id = medication.Id,
                Name = medication.Name,
                GenericName = medication.GenericName,
                Description = medication.Description,
                Manufacturer = medication.Manufacturer,
                Price = medication.Price,
                StockQuantity = medication.StockQuantity,
                MinimumStockLevel = medication.MinimumStockLevel,
                ExpiryDate = medication.ExpiryDate,
                BatchNumber = medication.BatchNumber,
                IsActive = medication.IsActive,
                RequiresPrescription = medication.RequiresPrescription,
                Category = medication.Category,
                DosageForm = medication.DosageForm,
                Strength = medication.Strength,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt,
                PrimaryImageUrl = medication.ImageUrl
            };

            return CreatedAtAction(nameof(GetMedication), new { id = medication.Id }, resultDto);
        }

        [HttpPut("medications/{id}")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(MedicationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateMedication(int id, [FromBody] MedicationCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var medication = await _context.Medications.FindAsync(id);

            if (medication == null || !medication.IsActive)
            {
                return NotFound("Medication not found");
            }

            if (dto.ExpiryDate <= DateTime.Now)
            {
                return BadRequest("Expiry date must be in the future");
            }

            var nameConflict = await ValidateMedicationNameAvailabilityAsync(dto.Name, id);
            if (nameConflict != null)
            {
                return nameConflict;
            }

            medication.Name = dto.Name;
            medication.GenericName = dto.GenericName;
            medication.Description = dto.Description;
            medication.Manufacturer = dto.Manufacturer;
            medication.Price = dto.Price;
            medication.StockQuantity = dto.StockQuantity;
            medication.MinimumStockLevel = dto.MinimumStockLevel;
            medication.ExpiryDate = dto.ExpiryDate;
            medication.BatchNumber = dto.BatchNumber;
            medication.IsActive = dto.IsActive;
            medication.RequiresPrescription = dto.RequiresPrescription;
            medication.Category = dto.Category;
            medication.DosageForm = dto.DosageForm;
            medication.Strength = dto.Strength;
            medication.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            _analyticsService.InvalidateAnalyticsCache();

            var resultDto = new MedicationDto
            {
                Id = medication.Id,
                Name = medication.Name,
                GenericName = medication.GenericName,
                Description = medication.Description,
                Manufacturer = medication.Manufacturer,
                Price = medication.Price,
                StockQuantity = medication.StockQuantity,
                MinimumStockLevel = medication.MinimumStockLevel,
                ExpiryDate = medication.ExpiryDate,
                BatchNumber = medication.BatchNumber,
                IsActive = medication.IsActive,
                RequiresPrescription = medication.RequiresPrescription,
                Category = medication.Category,
                DosageForm = medication.DosageForm,
                Strength = medication.Strength,
                CreatedAt = medication.CreatedAt,
                UpdatedAt = medication.UpdatedAt,
                PrimaryImageUrl = medication.ImageUrl
            };

            return Ok(resultDto);
        }

        [HttpDelete("medications/{id}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            var medication = await _context.Medications.FindAsync(id);

            if (medication == null)
            {
                return NotFound("Medication not found");
            }

            medication.IsActive = false;
            medication.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            _analyticsService.InvalidateAnalyticsCache();

            return NoContent();
        }

        #endregion

        #region Prescriptions Management

        /// <summary>
        /// Get paginated prescriptions with filtering and sorting support
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of prescriptions with support for:
        /// - **Pagination**: pageNumber (default: 1), pageSize (default: 10, max: 100)
        /// - **Filtering**: status, patientId, doctorId, pharmacistId, minAmount, maxAmount, prescribedAfter, prescribedBefore, dispensedAfter, dispensedBefore
        /// - **Sorting**: sortBy (single or comma-separated), sortOrder (asc/desc or comma-separated)
        /// 
        /// **Filter Examples:**
        /// - Single filter: ?status=Pending
        /// - Multiple filters: ?status=Pending&amp;minAmount=50&amp;maxAmount=200
        /// - Date range: ?prescribedAfter=2024-01-01&amp;prescribedBefore=2024-12-31
        /// 
        /// **Sorting Examples:**
        /// - Single column: ?sortBy=createdAt&amp;sortOrder=desc
        /// - Multi-column: ?sortBy=status,createdAt&amp;sortOrder=asc,desc
        /// </remarks>
        /// <param name="status">Filter by prescription status (exact match). Example: "Pending", "Dispensed", "Cancelled"</param>
        /// <param name="pageNumber">Page number (1-based). Default: 1, Minimum: 1</param>
        /// <param name="pageSize">Number of items per page. Default: 10, Range: 1-100</param>
        /// <param name="sortBy">Field(s) to sort by. Single: "createdAt", Multi: "status,createdAt". Supported: createdAt, totalAmount, prescriptionNumber, status, prescribedDate</param>
        /// <param name="sortOrder">Sort order(s). Single: "desc", Multi: "asc,desc". Default: "desc"</param>
        /// <returns>Paginated response containing prescriptions and pagination metadata</returns>
        /// <response code="200">Returns paginated prescriptions successfully</response>
        /// <response code="400">Invalid query parameters</response>
        /// <response code="401">Unauthorized - JWT token required</response>
        /// <response code="403">Forbidden - Pharmacist role required</response>
        [HttpGet("prescriptions")]
        [Authorize(Roles = "Pharmacist")]
        [ProducesResponseType(typeof(PaginatedResponse<PrescriptionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPrescriptions([FromQuery] PharmacyQueryParameters parameters)
        {
            var stopwatch = Stopwatch.StartNew();

            var filterValidationError = ValidateFilterParameters(parameters, PharmacyListEndpoint.Prescriptions);
            if (filterValidationError != null)
            {
                return filterValidationError;
            }

            NormalizePagination(parameters);
            
            // Use AsNoTracking and AsSplitQuery for optimal read-only performance
            var query = _context.Prescriptions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Patient)
                    .ThenInclude(pat => pat.AppUser)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.AppUser)
                .Include(p => p.Pharmacist)
                    .ThenInclude(ph => ph.AppUser)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .AsQueryable();

            query = _pharmacyService.GetFilteredPrescriptions(query, parameters);

            // Get total count BEFORE pagination (for performance optimization)
            var totalCount = await query.CountAsync();

            var pageNumber = parameters.PageNumber;
            var pageSize = parameters.PageSize;

            // Edge case: Handle empty results
            if (totalCount == 0)
            {
                return Ok(new PaginatedResponse<PrescriptionDto>(
                    new List<PrescriptionDto>(),
                    totalCount: 0,
                    pageNumber: pageNumber,
                    pageSize: pageSize
                ));
            }

            // Edge case: Handle pageNumber out of range - adjust to last valid page
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = totalPages;
            }

            // Apply sorting using PharmacyService
            query = _pharmacyService.ApplySorting(query, parameters.SortBy, parameters.SortOrder);

            // Calculate Skip and Take values for server-side pagination
            // Skip = (pageNumber - 1) * pageSize
            var skipValue = (pageNumber - 1) * pageSize;
            var takeValue = pageSize;

            // Apply pagination and projection at database level for optimal performance
            var dtoList = await query
                .Skip(skipValue)
                .Take(takeValue)
                .Select(p => new PrescriptionDto
            {
                Id = p.Id,
                PrescriptionNumber = p.PrescriptionNumber,
                MedicalReportId = p.MedicalReportId,
                PatientId = p.PatientId,
                Patient = new PatientDataDto
                {
                    Id = p.Patient.Id,
                    FirstName = p.Patient.FirstName,
                    LastName = p.Patient.LastName
                },
                DoctorId = p.DoctorId,
                Doctor = new DoctorDataDto
                {
                    FirstName = p.Doctor.FirstName,
                    LastName = p.Doctor.LastName,
                    PhoneNumber = p.Doctor.PhoneNumber,
                    Specialization = p.Doctor.Specialization,
                    LicenseNumber = p.Doctor.LicenseNumber,
                    BirthDate = p.Doctor.BirthDate ?? DateTime.MinValue,
                    Address = p.Doctor.Address,
                    Email = p.Doctor.AppUser != null ? p.Doctor.AppUser.Email ?? "" : ""
                },
                PharmacistId = p.PharmacistId,
                Pharmacist = p.Pharmacist != null ? new PharmacistDataDto
                {
                    Id = p.Pharmacist.Id,
                    FirstName = p.Pharmacist.FirstName,
                    LastName = p.Pharmacist.LastName,
                    LicenseNumber = p.Pharmacist.LicenseNumber,
                    PhoneNumber = p.Pharmacist.PhoneNumber,
                    Address = p.Pharmacist.Address,
                    HireDate = p.Pharmacist.HireDate,
                    Email = p.Pharmacist.AppUser != null ? p.Pharmacist.AppUser.Email ?? "" : "",
                    UserName = p.Pharmacist.AppUser != null ? p.Pharmacist.AppUser.UserName ?? "" : ""
                } : null,
                Status = p.Status,
                PrescribedDate = p.PrescribedDate,
                DispensedDate = p.DispensedDate,
                TotalAmount = p.TotalAmount,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                PrescriptionItems = p.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    PrescriptionId = pi.PrescriptionId,
                    MedicationId = pi.MedicationId,
                    MedicationName = pi.Medication.Name,
                    Quantity = pi.Quantity,
                    Instructions = pi.Instructions,
                    UnitPrice = pi.UnitPrice
                }).ToList()
                })
                .ToListAsync();

            stopwatch.Stop();

            // Log performance metrics
            var activeFilters = CountPrescriptionFilters(parameters);
            _logger.LogInformation(
                "Prescriptions query executed in {ElapsedMs}ms. Filters: {FilterCount}, Results: {ResultCount}, Page: {Page}, PageSize: {PageSize}",
                stopwatch.ElapsedMilliseconds,
                activeFilters,
                dtoList.Count,
                pageNumber,
                pageSize);

            // Return paginated response using PaginatedResponse<T> class
            var response = new PaginatedResponse<PrescriptionDto>(
                dtoList,
                totalCount,
                pageNumber,
                pageSize
            );

            return Ok(response);
        }

        [HttpGet("prescriptions/{id}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetPrescription(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                    .ThenInclude(pat => pat.AppUser)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.AppUser)
                .Include(p => p.Pharmacist)
                    .ThenInclude(ph => ph.AppUser)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            var dto = new PrescriptionDto
            {
                Id = prescription.Id,
                PrescriptionNumber = prescription.PrescriptionNumber,
                MedicalReportId = prescription.MedicalReportId,
                PatientId = prescription.PatientId,
                Patient = new PatientDataDto
                {
                    Id = prescription.Patient.Id,
                    FirstName = prescription.Patient.FirstName,
                    LastName = prescription.Patient.LastName
                },
                DoctorId = prescription.DoctorId,
                Doctor = new DoctorDataDto
                {
                    FirstName = prescription.Doctor.FirstName,
                    LastName = prescription.Doctor.LastName,
                    PhoneNumber = prescription.Doctor.PhoneNumber,
                    Specialization = prescription.Doctor.Specialization,
                    LicenseNumber = prescription.Doctor.LicenseNumber,
                    BirthDate = prescription.Doctor.BirthDate ?? DateTime.MinValue,
                    Address = prescription.Doctor.Address,
                    Email = prescription.Doctor.AppUser != null ? prescription.Doctor.AppUser.Email ?? "" : ""
                },
                PharmacistId = prescription.PharmacistId,
                Pharmacist = prescription.Pharmacist != null ? new PharmacistDataDto
                {
                    Id = prescription.Pharmacist.Id,
                    FirstName = prescription.Pharmacist.FirstName,
                    LastName = prescription.Pharmacist.LastName,
                    LicenseNumber = prescription.Pharmacist.LicenseNumber,
                    PhoneNumber = prescription.Pharmacist.PhoneNumber,
                    Address = prescription.Pharmacist.Address,
                    HireDate = prescription.Pharmacist.HireDate,
                    Email = prescription.Pharmacist.AppUser != null ? prescription.Pharmacist.AppUser.Email ?? "" : "",
                    UserName = prescription.Pharmacist.AppUser != null ? prescription.Pharmacist.AppUser.UserName ?? "" : ""
                } : null,
                Status = prescription.Status,
                PrescribedDate = prescription.PrescribedDate,
                DispensedDate = prescription.DispensedDate,
                TotalAmount = prescription.TotalAmount,
                Notes = prescription.Notes,
                CreatedAt = prescription.CreatedAt,
                UpdatedAt = prescription.UpdatedAt,
                PrescriptionItems = prescription.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    PrescriptionId = pi.PrescriptionId,
                    MedicationId = pi.MedicationId,
                    MedicationName = pi.Medication.Name,
                    Quantity = pi.Quantity,
                    Instructions = pi.Instructions,
                    UnitPrice = pi.UnitPrice
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost("prescriptions")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreatePrescription([FromBody] PrescriptionCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (doctorId == null)
            {
                return Unauthorized();
            }

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.AppUserId == doctorId);

            if (doctor == null)
            {
                return NotFound("Doctor not found");
            }

            if (dto.DoctorId != doctor.Id)
            {
                return Forbid("You can only create prescriptions for your own patients");
            }

            var medicalReport = await _context.MedicalReports
                .Include(mr => mr.Doctor)
                .FirstOrDefaultAsync(mr => mr.Id == dto.MedicalReportId);

            if (medicalReport == null)
            {
                return NotFound("Medical report not found");
            }

            if (medicalReport.DoctorId != doctor.Id)
            {
                return Forbid("Medical report does not belong to you");
            }

            var patient = await _context.Patients.FindAsync(dto.PatientId);

            if (patient == null)
            {
                return NotFound("Patient not found");
            }

            if (patient.DoctorId != doctor.Id)
            {
                return Forbid("Patient is not assigned to you");
            }

            if (dto.PrescriptionItems == null || !dto.PrescriptionItems.Any())
            {
                return BadRequest("At least one prescription item is required");
            }

            var prescriptionNumber = await GeneratePrescriptionNumberAsync();

            var prescription = new Prescription
            {
                PrescriptionNumber = prescriptionNumber,
                MedicalReportId = dto.MedicalReportId,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                Status = "Pending",
                PrescribedDate = DateTime.Now,
                Notes = dto.Notes,
                CreatedAt = DateTime.Now
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            decimal totalAmount = 0;

            foreach (var itemDto in dto.PrescriptionItems)
            {
                var medication = await _context.Medications.FindAsync(itemDto.MedicationId);

                if (medication == null || !medication.IsActive)
                {
                    return BadRequest($"Medication with ID {itemDto.MedicationId} not found or inactive");
                }

                var unitPrice = medication.Price;
                var itemTotalPrice = unitPrice * itemDto.Quantity;
                totalAmount += itemTotalPrice;

                var prescriptionItem = new PrescriptionItem
                {
                    PrescriptionId = prescription.Id,
                    MedicationId = itemDto.MedicationId,
                    Quantity = itemDto.Quantity,
                    Instructions = itemDto.Instructions,
                    UnitPrice = unitPrice,
                    TotalPrice = itemTotalPrice,
                    CreatedAt = DateTime.Now
                };

                _context.PrescriptionItems.Add(prescriptionItem);
            }

            prescription.TotalAmount = totalAmount;
            await _context.SaveChangesAsync();

            var result = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.AppUser)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .FirstOrDefaultAsync(p => p.Id == prescription.Id);

            var resultDto = new PrescriptionDto
            {
                Id = result.Id,
                PrescriptionNumber = result.PrescriptionNumber,
                MedicalReportId = result.MedicalReportId,
                PatientId = result.PatientId,
                Patient = new PatientDataDto
                {
                    Id = result.Patient.Id,
                    FirstName = result.Patient.FirstName,
                    LastName = result.Patient.LastName
                },
                DoctorId = result.DoctorId,
                Doctor = new DoctorDataDto
                {
                    FirstName = result.Doctor.FirstName,
                    LastName = result.Doctor.LastName,
                    PhoneNumber = result.Doctor.PhoneNumber,
                    Specialization = result.Doctor.Specialization,
                    LicenseNumber = result.Doctor.LicenseNumber,
                    BirthDate = result.Doctor.BirthDate ?? DateTime.MinValue,
                    Address = result.Doctor.Address,
                    Email = result.Doctor.AppUser?.Email ?? ""
                },
                Status = result.Status,
                PrescribedDate = result.PrescribedDate,
                TotalAmount = result.TotalAmount,
                Notes = result.Notes,
                CreatedAt = result.CreatedAt,
                PrescriptionItems = result.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    PrescriptionId = pi.PrescriptionId,
                    MedicationId = pi.MedicationId,
                    MedicationName = pi.Medication.Name,
                    Quantity = pi.Quantity,
                    Instructions = pi.Instructions,
                    UnitPrice = pi.UnitPrice
                }).ToList()
            };

            return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, resultDto);
        }

        [HttpPost("prescriptions/{id}/dispense")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> DispensePrescription(int id, [FromBody] PrescriptionDispenseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var prescription = await _context.Prescriptions
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
            {
                return NotFound("Prescription not found");
            }

            if (prescription.Status != "Pending")
            {
                return BadRequest($"Prescription is already {prescription.Status}. Only pending prescriptions can be dispensed.");
            }

            var pharmacist = await _context.Pharmacists.FindAsync(dto.PharmacistId);

            if (pharmacist == null)
            {
                return NotFound("Pharmacist not found");
            }

            foreach (var item in prescription.PrescriptionItems)
            {
                if (item.Medication.StockQuantity < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for medication {item.Medication.Name}. Available: {item.Medication.StockQuantity}, Required: {item.Quantity}");
                }
            }

            foreach (var item in prescription.PrescriptionItems)
            {
                item.Medication.StockQuantity -= item.Quantity;
                item.Medication.UpdatedAt = DateTime.Now;
            }

            prescription.Status = "Dispensed";
            prescription.PharmacistId = dto.PharmacistId;
            prescription.DispensedDate = dto.DispensedDate ?? DateTime.Now;
            prescription.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            _analyticsService.InvalidateAnalyticsCache();

            var result = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                    .ThenInclude(d => d.AppUser)
                .Include(p => p.Pharmacist)
                    .ThenInclude(ph => ph.AppUser)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medication)
                .FirstOrDefaultAsync(p => p.Id == id);

            var resultDto = new PrescriptionDto
            {
                Id = result.Id,
                PrescriptionNumber = result.PrescriptionNumber,
                MedicalReportId = result.MedicalReportId,
                PatientId = result.PatientId,
                Patient = new PatientDataDto
                {
                    Id = result.Patient.Id,
                    FirstName = result.Patient.FirstName,
                    LastName = result.Patient.LastName
                },
                DoctorId = result.DoctorId,
                Doctor = new DoctorDataDto
                {
                    FirstName = result.Doctor.FirstName,
                    LastName = result.Doctor.LastName,
                    PhoneNumber = result.Doctor.PhoneNumber,
                    Specialization = result.Doctor.Specialization,
                    LicenseNumber = result.Doctor.LicenseNumber,
                    BirthDate = result.Doctor.BirthDate ?? DateTime.MinValue,
                    Address = result.Doctor.Address,
                    Email = result.Doctor.AppUser?.Email ?? ""
                },
                PharmacistId = result.PharmacistId,
                Pharmacist = new PharmacistDataDto
                {
                    Id = result.Pharmacist.Id,
                    FirstName = result.Pharmacist.FirstName,
                    LastName = result.Pharmacist.LastName,
                    LicenseNumber = result.Pharmacist.LicenseNumber,
                    PhoneNumber = result.Pharmacist.PhoneNumber,
                    Address = result.Pharmacist.Address,
                    HireDate = result.Pharmacist.HireDate,
                    Email = result.Pharmacist.AppUser?.Email ?? "",
                    UserName = result.Pharmacist.AppUser?.UserName ?? ""
                },
                Status = result.Status,
                PrescribedDate = result.PrescribedDate,
                DispensedDate = result.DispensedDate,
                TotalAmount = result.TotalAmount,
                Notes = result.Notes,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt,
                PrescriptionItems = result.PrescriptionItems.Select(pi => new PrescriptionItemDto
                {
                    Id = pi.Id,
                    PrescriptionId = pi.PrescriptionId,
                    MedicationId = pi.MedicationId,
                    MedicationName = pi.Medication.Name,
                    Quantity = pi.Quantity,
                    Instructions = pi.Instructions,
                    UnitPrice = pi.UnitPrice
                }).ToList()
            };

            return Ok(resultDto);
        }

        #endregion

        #region Inventory & Pharmacist Data

        /// <summary>
        /// Get paginated inventory with filtering, sorting, and alerts
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of active medications with inventory alerts.
        /// Includes LowStockAlerts and ExpiryAlerts calculated from ALL matching items (not just current page).
        /// 
        /// Supports the same filtering and sorting as GetMedications endpoint.
        /// 
        /// **Response includes:**
        /// - Paginated medications list
        /// - LowStockAlerts: Medications below minimum stock level
        /// - ExpiryAlerts: Medications expiring within 30 days
        /// - Pagination metadata
        /// 
        /// **Example:**
        /// ?pageNumber=1&amp;pageSize=10&amp;category=painkiller&amp;minStock=5&amp;sortBy=name&amp;sortOrder=asc
        /// </remarks>
        /// <param name="category">Filter by medication category (exact match, case-insensitive). Example: "painkiller"</param>
        /// <param name="pageNumber">Page number (1-based). Default: 1, Minimum: 1</param>
        /// <param name="pageSize">Number of items per page. Default: 10, Range: 1-100</param>
        /// <param name="sortBy">Field(s) to sort by. Single: "name", Multi: "name,price". Supported: name, price, createdAt, stockQuantity, category, expiryDate</param>
        /// <param name="sortOrder">Sort order(s). Single: "asc", Multi: "asc,desc". Default: "desc"</param>
        /// <returns>Paginated response containing medications, alerts, and pagination metadata</returns>
        /// <response code="200">Returns paginated inventory with alerts successfully</response>
        /// <response code="400">Invalid query parameters</response>
        /// <response code="401">Unauthorized - JWT token required</response>
        /// <response code="403">Forbidden - Pharmacist role required</response>
        [HttpGet("inventory")]
        [Authorize(Roles = "Pharmacist")]
        [ProducesResponseType(typeof(InventoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetInventory([FromQuery] PharmacyQueryParameters parameters)
        {
            var stopwatch = Stopwatch.StartNew();

            var filterValidationError = ValidateFilterParameters(parameters, PharmacyListEndpoint.Inventory);
            if (filterValidationError != null)
            {
                return filterValidationError;
            }

            NormalizePagination(parameters);

            var query = _context.Medications.AsNoTracking().AsQueryable();
            query = _pharmacyService.GetFilteredInventory(query, parameters);

            var totalCount = await query.CountAsync();

            var pageNumber = parameters.PageNumber;
            var pageSize = parameters.PageSize;

            // Edge case: Handle empty results
            if (totalCount == 0)
            {
                return Ok(new InventoryResponse(
                    new List<MedicationDto>(),
                    totalCount: 0,
                    pageNumber: pageNumber,
                    pageSize: pageSize,
                    lowStockAlerts: new List<MedicationDto>(),
                    expiryAlerts: new List<MedicationDto>()));
            }

            // Edge case: Handle pageNumber out of range - adjust to last valid page
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = totalPages;
            }

            // Apply sorting using PharmacyService
            query = _pharmacyService.ApplySorting(query, parameters.SortBy, parameters.SortOrder);

            // Calculate Skip and Take values for server-side pagination
            // Skip = (pageNumber - 1) * pageSize
            var skipValue = (pageNumber - 1) * pageSize;
            var takeValue = pageSize;

            // For GetInventory, we need alerts from ALL matching items (not just current page)
            // So we execute the query twice: once for all items (alerts), once with pagination (main result)
            // Note: This is necessary because alerts must reflect all matching items regardless of pagination
            
            // First: Get all matching items for alerts calculation using projection for efficiency
            var allDtoList = await query
                .Select(m => new MedicationDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    GenericName = m.GenericName,
                    Description = m.Description,
                    Manufacturer = m.Manufacturer,
                    Price = m.Price,
                    StockQuantity = m.StockQuantity,
                    MinimumStockLevel = m.MinimumStockLevel,
                    ExpiryDate = m.ExpiryDate,
                    BatchNumber = m.BatchNumber,
                    IsActive = m.IsActive,
                    RequiresPrescription = m.RequiresPrescription,
                    Category = m.Category,
                    DosageForm = m.DosageForm,
                    Strength = m.Strength,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    PrimaryImageUrl = m.ImageUrl ?? m.Images
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => i.RelativeUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Second: Apply pagination at database level using projection for optimal performance
            var dtoList = await query
                .Skip(skipValue)
                .Take(takeValue)
                .Select(m => new MedicationDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    GenericName = m.GenericName,
                    Description = m.Description,
                    Manufacturer = m.Manufacturer,
                    Price = m.Price,
                    StockQuantity = m.StockQuantity,
                    MinimumStockLevel = m.MinimumStockLevel,
                    ExpiryDate = m.ExpiryDate,
                    BatchNumber = m.BatchNumber,
                    IsActive = m.IsActive,
                    RequiresPrescription = m.RequiresPrescription,
                    Category = m.Category,
                    DosageForm = m.DosageForm,
                    Strength = m.Strength,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    PrimaryImageUrl = m.ImageUrl ?? m.Images
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => i.RelativeUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            stopwatch.Stop();

            // Log performance metrics
            var activeFilters = CountMedicationFilters(parameters);
            _logger.LogInformation(
                "Inventory query executed in {ElapsedMs}ms. Filters: {FilterCount}, Results: {ResultCount}, Page: {Page}, PageSize: {PageSize}",
                stopwatch.ElapsedMilliseconds,
                activeFilters,
                dtoList.Count,
                pageNumber,
                pageSize);

            // Create paginated response using PaginatedResponse<T> structure
            var paginatedResponse = new PaginatedResponse<MedicationDto>(
                dtoList,
                totalCount,
                pageNumber,
                pageSize
            );

            var lowStockAlerts = allDtoList.Where(m => m.IsLowStock).ToList();
            var expiryAlerts = allDtoList
                .Where(m => m.ExpiryDate.HasValue
                    && m.ExpiryDate.Value <= DateTime.Now.AddDays(30)
                    && m.ExpiryDate.Value > DateTime.Now)
                .ToList();

            var response = new InventoryResponse(
                paginatedResponse.Items,
                paginatedResponse.TotalCount,
                paginatedResponse.CurrentPage,
                paginatedResponse.PageSize,
                lowStockAlerts,
                expiryAlerts);

            return Ok(response);
        }

        [HttpGet("pharmacist-data")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetPharmacistData()
        {
            var pharmacistId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (pharmacistId == null)
            {
                return Unauthorized();
            }

            var pharmacist = await _context.Pharmacists
                .Include(p => p.AppUser)
                .FirstOrDefaultAsync(p => p.AppUserId == pharmacistId);

            if (pharmacist == null)
            {
                return NotFound("Pharmacist not found");
            }

            var dto = new PharmacistDataDto
            {
                Id = pharmacist.Id,
                FirstName = pharmacist.FirstName,
                LastName = pharmacist.LastName,
                LicenseNumber = pharmacist.LicenseNumber,
                PhoneNumber = pharmacist.PhoneNumber,
                Address = pharmacist.Address,
                HireDate = pharmacist.HireDate,
                Email = pharmacist.AppUser?.Email ?? "",
                UserName = pharmacist.AppUser?.UserName ?? ""
            };

            return Ok(dto);
        }

        #endregion

        #region PDF Reports

        /// <summary>
        /// Generates a PDF report of inventory items with optional filtering
        /// </summary>
        /// <remarks>
        /// Generates a PDF report containing inventory items based on the provided filters and sorting options.
        /// Supports all filtering and sorting options available in the GetInventory endpoint.
        /// 
        /// **Filter Parameters:**
        /// - search: Search term for filtering by name, description, etc.
        /// - category: Filter by medication category
        /// - stockStatus: Filter by stock status (normal stock, low stock, out of stock)
        /// - minPrice, maxPrice: Price range filters
        /// - isActive: Filter by active status
        /// - requiresPrescription: Filter by prescription requirement
        /// - expiryBefore, expiryAfter: Expiry date range filters
        /// 
        /// **Sorting Parameters:**
        /// - sortBy: Column to sort by (e.g., "name", "price", "expiryDate")
        /// - sortOrder: Sort order ("asc" or "desc", default: "desc")
        /// 
        /// **PDF Options:**
        /// - includeAllData: If true, includes all matching items regardless of PageSize (default: true)
        /// - reportType: "summary" or "detailed" (default: "detailed")
        /// 
        /// **Response:**
        /// Returns a PDF file with appropriate Content-Disposition header for download.
        /// </remarks>
        /// <param name="request">Filter and sorting parameters for the report</param>
        /// <returns>PDF file containing the inventory report</returns>
        /// <response code="200">Returns the PDF file</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="401">Unauthorized - JWT token required</response>
        /// <response code="403">Forbidden - Pharmacist role required</response>
        /// <response code="422">Unprocessable Entity - Invalid filter combination</response>
        /// <response code="500">Error generating PDF</response>
        [HttpGet("reports/inventory/pdf")]
        [Authorize(Roles = "Pharmacist")]
        [Produces("application/pdf", "application/json")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateInventoryPdfReport([FromQuery] PharmacyPdfReportRequest request)
        {
            try
            {
                // 1. Validate request parameters
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate using IValidatableObject
                var validationResults = request.Validate(new ValidationContext(request));
                if (validationResults.Any())
                {
                    foreach (var error in validationResults)
                    {
                        ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? "", error.ErrorMessage ?? "");
                    }
                    return BadRequest(ModelState);
                }

                // Log PDF generation request for audit trail
                _logger.LogInformation(
                    "PDF generation requested - User: {User}, Type: {Type}, Filters: {Filters}",
                    User.Identity?.Name ?? "Unknown",
                    "inventory",
                    JsonSerializer.Serialize(request));

                // 2. Convert request to PharmacyQueryParameters for filtering
                var queryParams = ConvertPdfRequestToQueryParameters(request);

                var sortValidationError = ValidateSortParameters(queryParams, PharmacyListEndpoint.Inventory);
                if (sortValidationError != null)
                {
                    return sortValidationError;
                }

                // 3. Get filtered data using existing service methods
                var baseQuery = _context.Medications.AsQueryable();
                var filteredQuery = _pharmacyService.GetFilteredInventory(baseQuery, queryParams);
                var sortedQuery = _pharmacyService.ApplySorting(filteredQuery, queryParams.SortBy, queryParams.SortOrder);

                // If IncludeAllData is true, get all matching items (override pagination)
                var inventoryItems = request.IncludeAllData
                    ? await sortedQuery.AsNoTracking().ToListAsync()
                    : await sortedQuery.AsNoTracking()
                        .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                        .Take(queryParams.PageSize)
                        .ToListAsync();

                // 4. Generate PDF using PDF service
                var pdfBytes = await _pdfReportService.GenerateInventoryPdfAsync(inventoryItems, request);

                // 5. Return PDF file with proper headers
                var fileName = GetInventoryPdfFileName(request);
                return ReturnPdfFile(pdfBytes, fileName);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for PDF generation");
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation for PDF generation");
                return StatusCode(422, new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized PDF generation attempt");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory PDF report: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { error = $"Internal server error generating PDF: {ex.Message}" });
            }
        }

        /// <summary>
        /// Generates a PDF report of prescriptions with optional filtering
        /// </summary>
        /// <remarks>
        /// Generates a PDF report containing prescriptions based on the provided filters and sorting options.
        /// Supports all filtering and sorting options available in the GetPrescriptions endpoint.
        /// 
        /// **Filter Parameters:**
        /// - status: Filter by prescription status (Pending, Approved, Dispensed, Cancelled)
        /// - search: Search term for filtering by patient name, medication name, etc.
        /// - minPrice, maxPrice: Price range filters
        /// - prescriptionStatus: Alternative status filter
        /// 
        /// **Sorting Parameters:**
        /// - sortBy: Column to sort by (e.g., "createdAt", "prescribedDate", "totalAmount")
        /// - sortOrder: Sort order ("asc" or "desc", default: "desc")
        /// 
        /// **PDF Options:**
        /// - includeAllData: If true, includes all matching items regardless of PageSize (default: true)
        /// - reportType: "summary" or "detailed" (default: "detailed")
        /// 
        /// **Response:**
        /// Returns a PDF file with appropriate Content-Disposition header for download.
        /// </remarks>
        /// <param name="request">Filter and sorting parameters for the report</param>
        /// <returns>PDF file containing the prescriptions report</returns>
        /// <response code="200">Returns the PDF file</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="401">Unauthorized - JWT token required</response>
        /// <response code="403">Forbidden - Pharmacist role required</response>
        /// <response code="422">Unprocessable Entity - Invalid filter combination</response>
        /// <response code="500">Error generating PDF</response>
        [HttpGet("reports/prescriptions/pdf")]
        [Authorize(Roles = "Pharmacist")]
        [Produces("application/pdf", "application/json")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GeneratePrescriptionsPdfReport([FromQuery] PharmacyPdfReportRequest request)
        {
            try
            {
                // 1. Validate request parameters
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate using IValidatableObject
                var validationResults = request.Validate(new ValidationContext(request));
                if (validationResults.Any())
                {
                    foreach (var error in validationResults)
                    {
                        ModelState.AddModelError(error.MemberNames.FirstOrDefault() ?? "", error.ErrorMessage ?? "");
                    }
                    return BadRequest(ModelState);
                }

                // Log PDF generation request for audit trail
                _logger.LogInformation(
                    "PDF generation requested - User: {User}, Type: {Type}, Filters: {Filters}",
                    User.Identity?.Name ?? "Unknown",
                    "prescriptions",
                    JsonSerializer.Serialize(request));

                // 2. Convert request to PharmacyQueryParameters for filtering
                var queryParams = ConvertPdfRequestToQueryParameters(request);

                var sortValidationError = ValidateSortParameters(queryParams, PharmacyListEndpoint.Prescriptions);
                if (sortValidationError != null)
                {
                    return sortValidationError;
                }

                // 3. Get filtered data using existing service methods
                var baseQuery = _context.Prescriptions
                    .AsNoTracking()
                    .Include(p => p.Patient)
                        .ThenInclude(pat => pat.AppUser)
                    .Include(p => p.Doctor)
                        .ThenInclude(d => d.AppUser)
                    .Include(p => p.PrescriptionItems)
                        .ThenInclude(pi => pi.Medication)
                    .AsQueryable();

                var filteredQuery = _pharmacyService.GetFilteredPrescriptions(baseQuery, queryParams);
                var sortedQuery = _pharmacyService.ApplySorting(filteredQuery, queryParams.SortBy, queryParams.SortOrder);

                // If IncludeAllData is true, get all matching items (override pagination)
                var prescriptions = request.IncludeAllData
                    ? await sortedQuery.AsNoTracking().ToListAsync()
                    : await sortedQuery.AsNoTracking()
                        .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                        .Take(queryParams.PageSize)
                        .ToListAsync();

                // 4. Generate PDF using PDF service
                var pdfBytes = await _pdfReportService.GeneratePrescriptionsPdfAsync(prescriptions, request);

                // 5. Return PDF file with proper headers
                var fileName = GetPrescriptionsPdfFileName(request);
                return ReturnPdfFile(pdfBytes, fileName);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for PDF generation");
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation for PDF generation");
                return StatusCode(422, new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Unauthorized PDF generation attempt");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prescriptions PDF report: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { error = $"Internal server error generating PDF: {ex.Message}" });
            }
        }

        /// <summary>
        /// Generates a test PDF for debugging and verification
        /// </summary>
        /// <returns>Simple test PDF file</returns>
        /// <response code="200">Returns the test PDF file</response>
        /// <response code="401">Unauthorized - JWT token required</response>
        /// <response code="403">Forbidden - Pharmacist role required</response>
        [HttpGet("reports/test-pdf")]
        [Authorize(Roles = "Pharmacist")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult GenerateTestPdf()
        {
            try
            {
                var pdfBytes = _pdfReportService.GenerateSimplePdf(
                    "Test PDF Report",
                    "This is a test PDF generated by the Pharmacy module.\n\n" +
                    $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Service: {nameof(PdfReportService)}\n" +
                    "Status: Operational");

                return File(pdfBytes, "application/pdf", "test-report.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test PDF");
                return StatusCode(500, new { error = "Failed to generate test PDF" });
            }
        }

        #endregion

        #region Analytics Dashboard

        /// <summary>
        /// Get comprehensive dashboard statistics for pharmacy analytics
        /// </summary>
        /// <remarks>
        /// Returns aggregated statistics including:
        /// - Monthly revenue data (for bar chart)
        /// - Top medication categories (for pie chart)
        /// - Medication stock trends (for line chart)
        /// 
        /// **Query Parameters:**
        /// - startDate, endDate: Date range for filtering (optional)
        /// - revenueMonths: Number of months for revenue data (default: 12, range: 1-24)
        /// - topCategoriesCount: Number of top categories to return (default: 8, range: 1-50)
        /// - medicationIds: Array of medication IDs for stock trends (optional)
        /// - trendDays: Number of days for stock trends (default: 30, range: 1-365)
        /// - trendInterval: Data aggregation interval - "daily", "weekly", or "monthly" (default: "daily")
        /// 
        /// **Response includes:**
        /// - MonthlyRevenue: Revenue breakdown by month with totals and averages
        /// - TopCategories: Medication categories with counts and percentages
        /// - StockTrends: Stock level trends over time with medication summaries
        /// - Metadata: Generation timestamp, date range, and summary statistics
        /// 
        /// **Performance:**
        /// Results are cached for 5 minutes to improve performance.
        /// 
        /// **Example:**
        /// GET /api/pharmacy/analytics/dashboard-stats?revenueMonths=6&amp;topCategoriesCount=10&amp;trendDays=60
        /// </remarks>
        /// <param name="queryParams">Query parameters for filtering and customization</param>
        /// <returns>Complete dashboard statistics</returns>
        /// <response code="200">Returns dashboard statistics successfully</response>
        /// <response code="400">Invalid query parameters</response>
        /// <response code="401">Unauthorized - JWT token required</response>
        /// <response code="403">Forbidden - Pharmacist or Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("analytics/dashboard-stats")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardStats([FromQuery] DashboardStatsQueryParams queryParams)
        {
            try
            {
                // Validate date range if provided
                if (queryParams.StartDate.HasValue && queryParams.EndDate.HasValue)
                {
                    if (queryParams.StartDate.Value > queryParams.EndDate.Value)
                    {
                        return BadRequest(new { error = "Start date cannot be after end date" });
                    }

                    // Limit date range to prevent excessive queries
                    var maxDateRange = TimeSpan.FromDays(730); // 2 years max
                    if (queryParams.EndDate.Value - queryParams.StartDate.Value > maxDateRange)
                    {
                        return BadRequest(new { error = "Date range cannot exceed 730 days (2 years)" });
                    }
                }

                // Validate trend interval
                if (!string.IsNullOrEmpty(queryParams.TrendInterval))
                {
                    var validIntervals = new[] { "daily", "weekly", "monthly" };
                    if (!validIntervals.Contains(queryParams.TrendInterval.ToLower()))
                    {
                        return BadRequest(new { error = $"TrendInterval must be one of: {string.Join(", ", validIntervals)}" });
                    }
                }

                // Validate medication IDs if provided
                if (queryParams.MedicationIds != null && queryParams.MedicationIds.Length > 0)
                {
                    var validMedicationIds = await _context.Medications
                        .Where(m => queryParams.MedicationIds.Contains(m.Id) && m.IsActive)
                        .Select(m => m.Id)
                        .ToListAsync();

                    if (validMedicationIds.Count != queryParams.MedicationIds.Length)
                    {
                        var invalidIds = queryParams.MedicationIds.Except(validMedicationIds).ToList();
                        _logger.LogWarning("Invalid medication IDs provided: {InvalidIds}", string.Join(", ", invalidIds));
                        // Continue with valid IDs only
                        queryParams.MedicationIds = validMedicationIds.ToArray();
                    }
                }

                var stopwatch = Stopwatch.StartNew();
                var stats = await _analyticsService.GetDashboardStatsAsync(queryParams);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Dashboard stats generated in {ElapsedMs}ms. Revenue months: {RevenueMonths}, Categories: {Categories}, Trends days: {TrendDays}",
                    stopwatch.ElapsedMilliseconds,
                    queryParams.RevenueMonths ?? 12,
                    queryParams.TopCategoriesCount ?? 8,
                    queryParams.TrendDays ?? 30);

                return Ok(stats);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for dashboard stats");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dashboard statistics");
                return StatusCode(500, new { error = "An error occurred while generating dashboard statistics. Please try again later." });
            }
        }

        /// <summary>
        /// Get monthly revenue data for bar chart
        /// </summary>
        /// <remarks>
        /// Returns monthly revenue breakdown for the specified period.
        /// </remarks>
        /// <param name="period">Period type: "last3months", "last6months", "last12months" (default: "last12months")</param>
        /// <param name="startDate">Start date for custom range (optional)</param>
        /// <param name="endDate">End date for custom range (optional)</param>
        /// <param name="months">Number of months if period not specified (default: 12)</param>
        /// <returns>Monthly revenue data array</returns>
        [HttpGet("analytics/monthly-revenue")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(List<MonthlyRevenueItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMonthlyRevenue(
            [FromQuery] string? period = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int months = 12)
        {
            try
            {
                DateTime? revenueStartDate = startDate;
                DateTime? revenueEndDate = endDate;

                // Handle period parameter
                if (!string.IsNullOrEmpty(period) && !startDate.HasValue && !endDate.HasValue)
                {
                    revenueEndDate = DateTime.UtcNow;
                    revenueStartDate = period.ToLower() switch
                    {
                        "last3months" => revenueEndDate.Value.AddMonths(-3),
                        "last6months" => revenueEndDate.Value.AddMonths(-6),
                        "last12months" => revenueEndDate.Value.AddMonths(-12),
                        _ => revenueEndDate.Value.AddMonths(-months)
                    };
                }

                var revenueData = await _analyticsService.GetMonthlyRevenueAsync(revenueStartDate, revenueEndDate, months);
                return Ok(revenueData.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching monthly revenue");
                return StatusCode(500, new { error = "An error occurred while fetching monthly revenue data." });
            }
        }

        /// <summary>
        /// Get top medication categories for pie chart
        /// </summary>
        /// <remarks>
        /// Returns top medication categories with counts and percentages.
        /// </remarks>
        /// <param name="limit">Number of top categories to return (default: 10)</param>
        /// <returns>Category data array</returns>
        [HttpGet("analytics/top-categories")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(List<CategoryItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTopCategories([FromQuery] int limit = 10)
        {
            try
            {
                var categoriesData = await _analyticsService.GetTopCategoriesAsync(limit);
                return Ok(categoriesData.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top categories");
                return StatusCode(500, new { error = "An error occurred while fetching top categories data." });
            }
        }

        /// <summary>
        /// Get current stock levels by medication (snapshot).
        /// </summary>
        /// <remarks>
        /// Returns one data point per medication representing current stock as a percentage of
        /// estimated capacity. There is no inventory history table yet, so this is not a time series.
        /// The days and interval query parameters are accepted for compatibility but ignored.
        /// </remarks>
        /// <param name="medicationIds">Comma-separated medication IDs to include (optional)</param>
        /// <param name="days">Ignored until inventory history is available</param>
        /// <param name="interval">Ignored until inventory history is available</param>
        /// <returns>Current stock level data points</returns>
        [HttpGet("analytics/stock-trends")]
        [Authorize(Roles = "Pharmacist,Admin")]
        [ProducesResponseType(typeof(List<StockTrendItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStockTrends(
            [FromQuery] string? medicationIds = null,
            [FromQuery] int days = 30,
            [FromQuery] string interval = "daily")
        {
            try
            {
                int[]? ids = null;
                if (!string.IsNullOrEmpty(medicationIds))
                {
                    ids = medicationIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToArray();
                }

                var trendsData = await _analyticsService.GetStockTrendsAsync(ids, days, interval);
                return Ok(trendsData.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stock trends");
                return StatusCode(500, new { error = "An error occurred while fetching stock trends data." });
            }
        }

        #endregion

        #region Helper Methods

        private async Task<IActionResult?> ValidateMedicationNameAvailabilityAsync(
            string name,
            int? excludeMedicationId = null)
        {
            if (await _medicationDuplicateChecker.IsNameAvailableAsync(name, excludeMedicationId))
            {
                return null;
            }

            return Conflict(new { message = "Medication name already exists" });
        }

        /// <summary>
        /// Converts PharmacyPdfReportRequest to PharmacyQueryParameters for filtering
        /// </summary>
        private PharmacyQueryParameters ConvertPdfRequestToQueryParameters(PharmacyPdfReportRequest request)
        {
            return new PharmacyQueryParameters
            {
                PageNumber = request.PageNumber,
                PageSize = request.IncludeAllData ? int.MaxValue : request.PageSize,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder ?? "desc",
                SearchTerm = request.Search,
                Category = request.Category,
                Status = request.Status ?? request.PrescriptionStatus,
                MinPrice = request.MinPrice,
                MaxPrice = request.MaxPrice,
                IsActive = request.IsActive,
                RequiresPrescription = request.RequiresPrescription,
                StockStatus = request.StockStatus,
                ExpiryBefore = request.ExpiryBefore,
                ExpiryAfter = request.ExpiryAfter
            };
        }

        /// <summary>
        /// Returns PDF file with proper headers for download
        /// </summary>
        private IActionResult ReturnPdfFile(byte[] pdfBytes, string fileName)
        {
            // Set security and cache headers
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["X-Frame-Options"] = "DENY"; // Prevent clickjacking
            Response.Headers["Content-Length"] = pdfBytes.Length.ToString();

            // Set Content-Disposition for download
            var contentDisposition = $"attachment; filename=\"{fileName}\"; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";
            Response.Headers["Content-Disposition"] = contentDisposition;

            return File(pdfBytes, "application/pdf");
        }

        /// <summary>
        /// Returns CSV file with proper download headers.
        /// </summary>
        private IActionResult ReturnCsvFile(byte[] csvBytes, string fileName)
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Content-Length"] = csvBytes.Length.ToString();

            var contentDisposition = $"attachment; filename=\"{fileName}\"; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";
            Response.Headers["Content-Disposition"] = contentDisposition;

            return File(csvBytes, "text/csv", fileName);
        }

        /// <summary>
        /// Generates filename for inventory PDF report
        /// </summary>
        private string GetInventoryPdfFileName(PharmacyPdfReportRequest request)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var baseName = "inventory-report";

            // Add filter info to filename if applicable
            var filterParts = new List<string>();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchSnippet = request.Search.Length > 20 
                    ? request.Search.Substring(0, 20).Replace(" ", "_") 
                    : request.Search.Replace(" ", "_");
                filterParts.Add($"search-{searchSnippet}");
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                var categorySnippet = request.Category.Replace(" ", "_");
                filterParts.Add($"category-{categorySnippet}");
            }

            if (request.ExpiryBefore.HasValue)
            {
                filterParts.Add($"expiry-before-{request.ExpiryBefore.Value:yyyy-MM-dd}");
            }

            if (request.ExpiryAfter.HasValue)
            {
                filterParts.Add($"expiry-after-{request.ExpiryAfter.Value:yyyy-MM-dd}");
            }

            if (filterParts.Any())
            {
                baseName += "_" + string.Join("_", filterParts);
            }

            return $"{baseName}_{timestamp}.pdf";
        }

        /// <summary>
        /// Generates filename for prescriptions PDF report
        /// </summary>
        private string GetPrescriptionsPdfFileName(PharmacyPdfReportRequest request)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var baseName = "prescriptions-report";

            // Add filter info to filename if applicable
            var filterParts = new List<string>();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchSnippet = request.Search.Length > 20 
                    ? request.Search.Substring(0, 20).Replace(" ", "_") 
                    : request.Search.Replace(" ", "_");
                filterParts.Add($"search-{searchSnippet}");
            }

            if (!string.IsNullOrEmpty(request.Status) || !string.IsNullOrEmpty(request.PrescriptionStatus))
            {
                var status = request.Status ?? request.PrescriptionStatus ?? "";
                filterParts.Add($"status-{status.Replace(" ", "_")}");
            }

            if (filterParts.Any())
            {
                baseName += "_" + string.Join("_", filterParts);
            }

            return $"{baseName}_{timestamp}.pdf";
        }


        private static void ApplyLegacyMedicationQueryAliases(
            PharmacyQueryParameters parameters,
            string? search,
            int page)
        {
            if (string.IsNullOrEmpty(parameters.SearchTerm) && !string.IsNullOrEmpty(search))
            {
                parameters.SearchTerm = search;
            }

            if (parameters.PageNumber == 1 && page != 1)
            {
                parameters.PageNumber = page;
            }
        }

        private static void NormalizePagination(PharmacyQueryParameters parameters)
        {
            if (parameters.PageNumber < 1)
            {
                parameters.PageNumber = 1;
            }

            parameters.PageSize = Math.Clamp(parameters.PageSize, 1, 100);
        }

        private IActionResult? ValidateFilterParameters(
            PharmacyQueryParameters parameters,
            PharmacyListEndpoint endpoint)
        {
            var validationResults = PharmacyQueryParameterValidator.Validate(parameters, endpoint);
            if (validationResults.Count == 0)
            {
                return null;
            }

            foreach (var result in validationResults)
            {
                var memberNames = result.MemberNames.Any()
                    ? result.MemberNames
                    : new[] { string.Empty };

                foreach (var memberName in memberNames)
                {
                    ModelState.AddModelError(
                        memberName,
                        result.ErrorMessage ?? "Invalid filter parameter.");
                }
            }

            return ValidationProblem(ModelState);
        }

        private IActionResult? ValidateSortParameters(
            PharmacyQueryParameters parameters,
            PharmacyListEndpoint endpoint)
        {
            var validationResults = PharmacySortValidator.Validate(parameters, endpoint);
            if (validationResults.Count == 0)
            {
                return null;
            }

            foreach (var result in validationResults)
            {
                ModelState.AddModelError(
                    nameof(PharmacyQueryParameters.SortBy),
                    result.ErrorMessage ?? "Invalid sort column.");
            }

            return ValidationProblem(ModelState);
        }

        private static int CountMedicationFilters(PharmacyQueryParameters parameters)
        {
            var count = 0;
            if (!string.IsNullOrEmpty(parameters.Category)) count++;
            if (!string.IsNullOrEmpty(parameters.SearchTerm)) count++;
            if (!string.IsNullOrEmpty(parameters.StockStatus)) count++;
            if (parameters.RequiresPrescription.HasValue) count++;
            if (parameters.IsActive.HasValue) count++;
            if (parameters.MinPrice.HasValue) count++;
            if (parameters.MaxPrice.HasValue) count++;
            if (parameters.MinStock.HasValue) count++;
            if (parameters.MaxStock.HasValue) count++;
            if (!string.IsNullOrEmpty(parameters.Status)) count++;
            if (parameters.CreatedAfter.HasValue) count++;
            if (parameters.CreatedBefore.HasValue) count++;
            if (parameters.ExpiryAfter.HasValue) count++;
            if (parameters.ExpiryBefore.HasValue) count++;
            return count;
        }

        private static int CountPrescriptionFilters(PharmacyQueryParameters parameters)
        {
            var count = 0;
            if (!string.IsNullOrEmpty(parameters.Status)) count++;
            if (parameters.PatientId.HasValue) count++;
            if (parameters.DoctorId.HasValue) count++;
            if (parameters.PharmacistId.HasValue) count++;
            if (parameters.MinAmount.HasValue) count++;
            if (parameters.MaxAmount.HasValue) count++;
            if (parameters.PrescribedAfter.HasValue) count++;
            if (parameters.PrescribedBefore.HasValue) count++;
            if (parameters.DispensedAfter.HasValue) count++;
            if (parameters.DispensedBefore.HasValue) count++;
            return count;
        }

        private async Task<string> GeneratePrescriptionNumberAsync()
        {
            var year = DateTime.Now.Year;
            var lastPrescription = await _context.Prescriptions
                .Where(p => p.PrescriptionNumber.StartsWith($"RX-{year}-"))
                .OrderByDescending(p => p.PrescriptionNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastPrescription != null)
            {
                var parts = lastPrescription.PrescriptionNumber.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSeq))
                {
                    sequence = lastSeq + 1;
                }
            }

            return $"RX-{year}-{sequence.ToString("D4")}";
        }


        #endregion
    }
}
