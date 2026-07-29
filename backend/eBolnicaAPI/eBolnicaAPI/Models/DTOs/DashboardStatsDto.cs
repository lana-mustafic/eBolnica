using System.ComponentModel.DataAnnotations;

namespace eBolnicaAPI.Models.DTOs
{
    /// <summary>
    /// Complete dashboard statistics response containing all analytics data
    /// </summary>
    public class DashboardStatsResponse
    {
        public RevenueData MonthlyRevenue { get; set; } = new();
        public CategoriesData TopCategories { get; set; } = new();
        public StockTrendsData StockTrends { get; set; } = new();
        public Metadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Monthly revenue data with aggregated statistics
    /// </summary>
    public class RevenueData
    {
        public List<MonthlyRevenueItem> Data { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal AverageMonthlyRevenue { get; set; }
        public decimal RevenueChangePercentage { get; set; } // vs previous period
    }

    /// <summary>
    /// Individual monthly revenue item
    /// </summary>
    public class MonthlyRevenueItem
    {
        public string Month { get; set; } = string.Empty; // "January 2024"
        public string MonthShort { get; set; } = string.Empty; // "Jan"
        public string YearMonth { get; set; } = string.Empty; // "2024-01"
        public decimal Revenue { get; set; }
        public int PrescriptionCount { get; set; }
    }

    /// <summary>
    /// Medication categories data with aggregated statistics
    /// </summary>
    public class CategoriesData
    {
        public List<CategoryItem> Data { get; set; } = new();
        public int TotalCategories { get; set; }
        public int TotalMedications { get; set; }
    }

    /// <summary>
    /// Individual category item with medication count and percentage
    /// </summary>
    public class CategoryItem
    {
        public string Category { get; set; } = string.Empty; // "Antibiotics", "Pain Relief"
        public int MedicationCount { get; set; }
        public decimal Percentage { get; set; } // 0-100
        public decimal TotalValue { get; set; } // Total stock value in category (Price * StockQuantity)
    }

    /// <summary>
    /// Stock trends data with timeline and medication summaries
    /// </summary>
    public class StockTrendsData
    {
        public List<StockTrendItem> Data { get; set; } = new();
        public List<MedicationSummary> Medications { get; set; } = new();
        public List<string> Timeline { get; set; } = new(); // Dates for X-axis
    }

    /// <summary>
    /// Individual stock trend data point
    /// </summary>
    public class StockTrendItem
    {
        public DateTime Date { get; set; }
        public int MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public decimal StockLevel { get; set; } // 0-100 percentage
        public int Quantity { get; set; }
        public string Status { get; set; } = "Normal"; // "Normal", "Low", "Critical"
    }

    /// <summary>
    /// Medication summary for stock trends
    /// </summary>
    public class MedicationSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#3b82f6"; // Hex color for chart
        public decimal CurrentStock { get; set; }
        public decimal TrendDirection { get; set; } // -1 to 1 (negative = decreasing, positive = increasing)
    }

    /// <summary>
    /// Metadata about the dashboard statistics generation
    /// </summary>
    public class Metadata
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateRange? DateRange { get; set; }
        public StatisticsSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Date range for filtering
    /// </summary>
    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// Summary statistics for dashboard metric cards (authoritative DB counts).
    /// </summary>
    public class StatisticsSummary
    {
        public int TotalPrescriptions { get; set; }
        /// <summary>Count of active medications in inventory.</summary>
        public int TotalMedications { get; set; }
        public int TotalCategories { get; set; }
        public decimal TotalRevenue { get; set; }
        /// <summary>Prescriptions awaiting dispense.</summary>
        public int PendingPrescriptions { get; set; }
        /// <summary>Active medications below minimum stock level.</summary>
        public int LowStockAlerts { get; set; }
        /// <summary>Active medications expiring within the next 30 days.</summary>
        public int ExpiringSoon { get; set; }
        /// <summary>Active medications past expiry date.</summary>
        public int ExpiredMedications { get; set; }
        /// <summary>Total inventory value (sum of Price * StockQuantity for active medications).</summary>
        public decimal InventoryValue { get; set; }
    }

    /// <summary>
    /// Query parameters for dashboard statistics endpoint
    /// </summary>
    public class DashboardStatsQueryParams
    {
        // Date range for filtering
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        // Revenue chart specific
        [Range(1, 24, ErrorMessage = "RevenueMonths must be between 1 and 24")]
        public int? RevenueMonths { get; set; } = 12; // Last N months
        
        // Categories chart specific  
        [Range(1, 50, ErrorMessage = "TopCategoriesCount must be between 1 and 50")]
        public int? TopCategoriesCount { get; set; } = 8; // Top N categories
        
        // Stock trends specific
        public int[]? MedicationIds { get; set; } // Specific medications to include
        [Range(1, 365, ErrorMessage = "TrendDays must be between 1 and 365")]
        public int? TrendDays { get; set; } = 30; // Last N days of trends
        public string? TrendInterval { get; set; } = "daily"; // daily, weekly, monthly
        
        // General filtering
        public int? PharmacyId { get; set; } // For multi-pharmacy systems (future use)
    }
}
