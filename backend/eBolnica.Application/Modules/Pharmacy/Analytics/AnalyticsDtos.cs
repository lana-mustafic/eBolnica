namespace eBolnica.Application.Modules.Pharmacy.Analytics;

public sealed class DashboardStatsResponseDto
{
    public RevenueDataDto MonthlyRevenue { get; set; } = new();
    public CategoriesDataDto TopCategories { get; set; } = new();
    public StockTrendsDataDto StockTrends { get; set; } = new();
    public AnalyticsMetadataDto Metadata { get; set; } = new();
}

public sealed class RevenueDataDto
{
    public IReadOnlyList<MonthlyRevenueItemDto> Data { get; set; } = Array.Empty<MonthlyRevenueItemDto>();
    public decimal TotalRevenue { get; set; }
    public decimal AverageMonthlyRevenue { get; set; }
    public decimal RevenueChangePercentage { get; set; }
}

public sealed class MonthlyRevenueItemDto
{
    public string Month { get; set; } = string.Empty;
    public string MonthShort { get; set; } = string.Empty;
    public string YearMonth { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int PrescriptionCount { get; set; }
}

public sealed class CategoriesDataDto
{
    public IReadOnlyList<CategoryItemDto> Data { get; set; } = Array.Empty<CategoryItemDto>();
    public int TotalCategories { get; set; }
    public int TotalMedications { get; set; }
}

public sealed class CategoryItemDto
{
    public string Category { get; set; } = string.Empty;
    public int MedicationCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal TotalValue { get; set; }
}

public sealed class StockTrendsDataDto
{
    public IReadOnlyList<StockTrendItemDto> Data { get; set; } = Array.Empty<StockTrendItemDto>();
    public IReadOnlyList<MedicationSummaryDto> Medications { get; set; } = Array.Empty<MedicationSummaryDto>();
    public IReadOnlyList<string> Timeline { get; set; } = Array.Empty<string>();
    public string MetricType { get; set; } = "current-stock-snapshot";
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;
}

public sealed class StockTrendItemDto
{
    public DateTime Date { get; set; }
    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public decimal StockLevel { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = "Normal";
}

public sealed class MedicationSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
    public decimal CurrentStock { get; set; }
    public decimal TrendDirection { get; set; }
}

public sealed class AnalyticsMetadataDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public AnalyticsDateRangeDto? DateRange { get; set; }
    public StatisticsSummaryDto Summary { get; set; } = new();
}

public sealed class AnalyticsDateRangeDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public sealed class StatisticsSummaryDto
{
    public int TotalPrescriptions { get; set; }
    public int TotalMedications { get; set; }
    public int TotalCategories { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingPrescriptions { get; set; }
    public int LowStockAlerts { get; set; }
    public int ExpiringSoon { get; set; }
    public int ExpiredMedications { get; set; }
    public decimal InventoryValue { get; set; }
}

public sealed class PdfReportResultDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "report.pdf";
    public int RowCount { get; set; }
}
