using eBolnicaAPI.Models.DTOs;

namespace eBolnicaAPI.Services
{
    /// <summary>
    /// Service interface for Pharmacy Analytics operations
    /// </summary>
    public interface IPharmacyAnalyticsService
    {
        /// <summary>
        /// Gets complete dashboard statistics including revenue, categories, and stock trends
        /// </summary>
        /// <param name="queryParams">Query parameters for filtering and customization</param>
        /// <returns>Dashboard statistics response</returns>
        Task<DashboardStatsResponse> GetDashboardStatsAsync(DashboardStatsQueryParams queryParams);

        /// <summary>
        /// Gets monthly revenue data for the specified period
        /// </summary>
        /// <param name="startDate">Start date for revenue calculation</param>
        /// <param name="endDate">End date for revenue calculation</param>
        /// <param name="months">Number of months to include (if date range not specified)</param>
        /// <returns>Revenue data with monthly breakdown</returns>
        Task<RevenueData> GetMonthlyRevenueAsync(DateTime? startDate = null, DateTime? endDate = null, int months = 12);

        /// <summary>
        /// Gets top medication categories with counts and percentages
        /// </summary>
        /// <param name="topCount">Number of top categories to return</param>
        /// <returns>Categories data with medication counts</returns>
        Task<CategoriesData> GetTopCategoriesAsync(int topCount = 8);

        /// <summary>
        /// Gets current stock levels by medication (snapshot).
        /// There is no inventory history table yet; days/interval are accepted for API
        /// compatibility but do not produce a synthetic timeline.
        /// </summary>
        /// <param name="medicationIds">Array of medication IDs to track (null for default top items)</param>
        /// <param name="days">Ignored until inventory history is available</param>
        /// <param name="interval">Ignored until inventory history is available</param>
        /// <returns>One data point per medication representing current stock levels</returns>
        Task<StockTrendsData> GetStockTrendsAsync(int[]? medicationIds = null, int days = 30, string interval = "daily");
    }
}
