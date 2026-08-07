namespace eBolnica.Application.Modules.Pharmacy.Analytics;

public sealed class InventoryPdfSummary
{
    public int TotalCount { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }
    public int ExpiringSoonCount { get; init; }
}

public sealed class PrescriptionsPdfSummary
{
    public int TotalCount { get; init; }
}
