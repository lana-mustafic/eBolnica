using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Request model for PDF report generation with filtering and sorting options
    /// </summary>
    public class PharmacyPdfReportRequest : IValidatableObject
    {
        // Pagination (optional for PDF, might override)
        /// <summary>
        /// Page number for pagination (optional for PDF reports)
        /// </summary>
        [Range(1, 10000, ErrorMessage = "PageNumber must be between 1 and 10000")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size for pagination (default: 1000, use int.MaxValue for all data)
        /// </summary>
        [Range(1, 10000, ErrorMessage = "PageSize must be between 1 and 10000")]
        public int PageSize { get; set; } = 1000;

        // Sorting
        /// <summary>
        /// Column name to sort by
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort order: 'asc' or 'desc' (default: 'desc')
        /// </summary>
        public string? SortOrder { get; set; } = "desc";

        // Common filters
        /// <summary>
        /// Search term for filtering by name, description, etc.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Category filter
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Status filter (generic)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Minimum price filter
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "MinPrice must be greater than or equal to 0")]
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Maximum price filter
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "MaxPrice must be greater than or equal to 0")]
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Filter by active status
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Filter by prescription requirement
        /// </summary>
        public bool? RequiresPrescription { get; set; }

        /// <summary>
        /// Stock status filter (e.g., "normal stock", "low stock", "out of stock")
        /// </summary>
        public string? StockStatus { get; set; }

        // Prescription-specific filters
        /// <summary>
        /// Prescription status filter (Pending, Approved, Dispensed, Cancelled)
        /// </summary>
        public string? PrescriptionStatus { get; set; }

        /// <summary>
        /// Prescription urgency filter
        /// </summary>
        public string? Urgency { get; set; }

        // Inventory-specific filters
        /// <summary>
        /// Supplier filter for inventory items
        /// </summary>
        public string? Supplier { get; set; }

        /// <summary>
        /// Filter items expiring before this date
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? ExpiryBefore { get; set; }

        /// <summary>
        /// Filter items expiring after this date
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? ExpiryAfter { get; set; }

        // PDF-specific options
        /// <summary>
        /// Whether to include all matching data (ignore pagination)
        /// </summary>
        public bool IncludeAllData { get; set; } = true;

        /// <summary>
        /// Report type: "summary" or "detailed" (default: "detailed")
        /// </summary>
        [RegularExpression("summary|detailed", ErrorMessage = "ReportType must be 'summary' or 'detailed'")]
        public string ReportType { get; set; } = "detailed";

        /// <summary>
        /// Whether to include charts in the PDF
        /// </summary>
        public bool IncludeCharts { get; set; } = false;

        /// <summary>
        /// Custom validation
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            {
                yield return new ValidationResult(
                    "MinPrice cannot be greater than MaxPrice",
                    new[] { nameof(MinPrice), nameof(MaxPrice) });
            }

            if (ExpiryAfter.HasValue && ExpiryBefore.HasValue && ExpiryAfter > ExpiryBefore)
            {
                yield return new ValidationResult(
                    "ExpiryAfter cannot be after ExpiryBefore",
                    new[] { nameof(ExpiryAfter), nameof(ExpiryBefore) });
            }

            if (!string.IsNullOrWhiteSpace(SortOrder))
            {
                var normalizedSortOrder = SortOrder.Trim().ToLowerInvariant();
                if (normalizedSortOrder is not ("asc" or "desc"))
                {
                    yield return new ValidationResult(
                        "SortOrder must be 'asc' or 'desc'",
                        new[] { nameof(SortOrder) });
                }
                else
                {
                    SortOrder = normalizedSortOrder;
                }
            }
        }
    }
}
