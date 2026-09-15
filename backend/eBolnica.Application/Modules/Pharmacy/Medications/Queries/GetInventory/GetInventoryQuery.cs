using eBolnica.Application.Modules.Pharmacy.Medications.Queries.ListMedications;

namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;

public sealed class GetInventoryQuery : IRequest<GetInventoryQueryDto>
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public string? StockStatus { get; init; }
    public bool? RequiresPrescription { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}

public sealed class GetInventoryQueryDto
{
    public IReadOnlyList<MedicationDto> Items { get; init; } = Array.Empty<MedicationDto>();
    public IReadOnlyList<MedicationDto> LowStockAlerts { get; init; } = Array.Empty<MedicationDto>();
    public IReadOnlyList<MedicationDto> ExpiryAlerts { get; init; } = Array.Empty<MedicationDto>();
    public int LowStockAlertCount { get; init; }
    public int ExpiryAlertCount { get; init; }
    public int TotalMedications { get; init; }
    public decimal InventoryValue { get; init; }
    public int TotalCount { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyList<MedicationSupplierDto> Suppliers { get; init; } = Array.Empty<MedicationSupplierDto>();
    public IReadOnlyList<PurchaseOrderSummaryDto> PurchaseOrders { get; init; } = Array.Empty<PurchaseOrderSummaryDto>();
    public IReadOnlyList<StockReceiptSummaryDto> RecentReceipts { get; init; } = Array.Empty<StockReceiptSummaryDto>();
}

public sealed class MedicationSupplierDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? TaxNumber { get; init; }
    public bool IsActive { get; init; }
}

public sealed class PurchaseOrderSummaryDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime OrderedAtUtc { get; init; }
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<PurchaseOrderItemDto> Items { get; init; } = Array.Empty<PurchaseOrderItemDto>();
}

public sealed class PurchaseOrderItemDto
{
    public int MedicationId { get; init; }
    public string MedicationName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class StockReceiptSummaryDto
{
    public int Id { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime ReceivedAtUtc { get; init; }
    public int TotalQuantity { get; init; }
}
