using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Query parameters for Pharmacy module endpoints supporting pagination, filtering, and sorting.
    /// ASP.NET Core automatically binds query string parameters to this DTO.
    /// Example: /api/pharmacy/medications?pageNumber=1&amp;pageSize=10&amp;category=antibiotics&amp;minPrice=10&amp;maxPrice=50
    /// </summary>
    public class PharmacyQueryParameters : IValidatableObject
    {
        #region Pagination Parameters

        /// <summary>
        /// Page number (1-based). Default: 1
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page. Default: 10, Maximum: 100
        /// </summary>
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        #endregion

        #region Sorting Parameters

        /// <summary>
        /// Field to sort by (e.g., "name", "price", "createdAt", "stockQuantity")
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort order: "asc" or "desc". Default: "desc"
        /// </summary>
        [RegularExpression("^(asc|desc)$", ErrorMessage = "SortOrder must be 'asc' or 'desc'")]
        public string? SortOrder { get; set; } = "desc";

        #endregion

        #region Common Filters

        /// <summary>
        /// Search term for searching across name, generic name, or manufacturer
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by medication category (exact match, case-insensitive)
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Filter by status (e.g., "active", "inactive", "Pending", "Dispensed")
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
        /// Filter by stock status: "low stock", "out of stock", "normal stock"
        /// </summary>
        public string? StockStatus { get; set; }

        #endregion

        #region Date Filters

        /// <summary>
        /// Filter records created after this date
        /// </summary>
        public DateTime? CreatedAfter { get; set; }

        /// <summary>
        /// Filter records created before this date
        /// </summary>
        public DateTime? CreatedBefore { get; set; }

        /// <summary>
        /// Filter medications expiring after this date
        /// </summary>
        public DateTime? ExpiryAfter { get; set; }

        /// <summary>
        /// Filter medications expiring before this date
        /// </summary>
        public DateTime? ExpiryBefore { get; set; }

        #endregion

        #region Prescription-Specific Filters

        /// <summary>
        /// Filter prescriptions by patient ID
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "PatientId must be greater than 0")]
        public int? PatientId { get; set; }

        /// <summary>
        /// Filter prescriptions by doctor ID
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "DoctorId must be greater than 0")]
        public int? DoctorId { get; set; }

        /// <summary>
        /// Filter prescriptions by pharmacist ID
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "PharmacistId must be greater than 0")]
        public int? PharmacistId { get; set; }

        /// <summary>
        /// Minimum total amount filter for prescriptions
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "MinAmount must be greater than or equal to 0")]
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Maximum total amount filter for prescriptions
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "MaxAmount must be greater than or equal to 0")]
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Filter prescriptions prescribed after this date
        /// </summary>
        public DateTime? PrescribedAfter { get; set; }

        /// <summary>
        /// Filter prescriptions prescribed before this date
        /// </summary>
        public DateTime? PrescribedBefore { get; set; }

        /// <summary>
        /// Filter prescriptions dispensed after this date
        /// </summary>
        public DateTime? DispensedAfter { get; set; }

        /// <summary>
        /// Filter prescriptions dispensed before this date
        /// </summary>
        public DateTime? DispensedBefore { get; set; }

        #endregion

        #region Stock Quantity Filters

        /// <summary>
        /// Minimum stock quantity filter
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "MinStock must be greater than or equal to 0")]
        public int? MinStock { get; set; }

        /// <summary>
        /// Maximum stock quantity filter
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "MaxStock must be greater than or equal to 0")]
        public int? MaxStock { get; set; }

        #endregion

        #region Validation

        /// <summary>
        /// Custom validation logic for complex rules
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate price range
            if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            {
                results.Add(new ValidationResult(
                    "MinPrice must be less than or equal to MaxPrice",
                    new[] { nameof(MinPrice), nameof(MaxPrice) }));
            }

            // Validate amount range
            if (MinAmount.HasValue && MaxAmount.HasValue && MinAmount > MaxAmount)
            {
                results.Add(new ValidationResult(
                    "MinAmount must be less than or equal to MaxAmount",
                    new[] { nameof(MinAmount), nameof(MaxAmount) }));
            }

            // Validate stock range
            if (MinStock.HasValue && MaxStock.HasValue && MinStock > MaxStock)
            {
                results.Add(new ValidationResult(
                    "MinStock must be less than or equal to MaxStock",
                    new[] { nameof(MinStock), nameof(MaxStock) }));
            }

            // Validate date ranges
            if (CreatedAfter.HasValue && CreatedBefore.HasValue && CreatedAfter > CreatedBefore)
            {
                results.Add(new ValidationResult(
                    "CreatedAfter must be less than or equal to CreatedBefore",
                    new[] { nameof(CreatedAfter), nameof(CreatedBefore) }));
            }

            if (ExpiryAfter.HasValue && ExpiryBefore.HasValue && ExpiryAfter > ExpiryBefore)
            {
                results.Add(new ValidationResult(
                    "ExpiryAfter must be less than or equal to ExpiryBefore",
                    new[] { nameof(ExpiryAfter), nameof(ExpiryBefore) }));
            }

            if (PrescribedAfter.HasValue && PrescribedBefore.HasValue && PrescribedAfter > PrescribedBefore)
            {
                results.Add(new ValidationResult(
                    "PrescribedAfter must be less than or equal to PrescribedBefore",
                    new[] { nameof(PrescribedAfter), nameof(PrescribedBefore) }));
            }

            if (DispensedAfter.HasValue && DispensedBefore.HasValue && DispensedAfter > DispensedBefore)
            {
                results.Add(new ValidationResult(
                    "DispensedAfter must be less than or equal to DispensedBefore",
                    new[] { nameof(DispensedAfter), nameof(DispensedBefore) }));
            }

            // Validate stock status values
            if (!string.IsNullOrEmpty(StockStatus))
            {
                var validStockStatuses = new[] { "low stock", "out of stock", "normal stock", "in stock" };
                if (!validStockStatuses.Contains(StockStatus.ToLower()))
                {
                    results.Add(new ValidationResult(
                        $"StockStatus must be one of: {string.Join(", ", validStockStatuses)}",
                        new[] { nameof(StockStatus) }));
                }
            }

            return results;
        }

        #endregion
    }
}
