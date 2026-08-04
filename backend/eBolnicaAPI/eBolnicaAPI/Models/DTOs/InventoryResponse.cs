using System.Collections.Generic;

namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Paginated inventory response including stock and expiry alerts.
    /// Serialized as camelCase JSON: items, totalCount, lowStockAlerts, expiryAlerts, etc.
    /// </summary>
    public class InventoryResponse : PaginatedResponse<MedicationDto>
    {
        /// <summary>
        /// Active medications with stock below minimum across the filtered result set.
        /// </summary>
        public List<MedicationDto> LowStockAlerts { get; set; } = new();

        /// <summary>
        /// Active medications expiring within the next 30 days across the filtered result set.
        /// </summary>
        public List<MedicationDto> ExpiryAlerts { get; set; } = new();

        public InventoryResponse()
        {
        }

        public InventoryResponse(
            List<MedicationDto> items,
            int totalCount,
            int pageNumber,
            int pageSize,
            List<MedicationDto> lowStockAlerts,
            List<MedicationDto> expiryAlerts)
            : base(items, totalCount, pageNumber, pageSize)
        {
            LowStockAlerts = lowStockAlerts ?? new List<MedicationDto>();
            ExpiryAlerts = expiryAlerts ?? new List<MedicationDto>();
        }
    }
}
