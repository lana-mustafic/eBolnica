/**
 * Analytics Data Transfer Objects
 * Used for pharmacy dashboard analytics and chart data
 */

/**
 * Monthly revenue data point for bar chart
 */
export interface MonthlyRevenueData {
  /** Month name (e.g., "January", "February") */
  month: string;
  /** Month abbreviation (e.g., "Jan", "Feb") */
  monthAbbr?: string;
  /** Revenue amount for the month */
  revenue: number;
  /** Number of transactions/orders for the month */
  transactionCount?: number;
}

/**
 * Medication category data for pie chart
 */
export interface MedicationCategoryData {
  /** Category name (e.g., "Antibiotics", "Pain Relief") */
  category: string;
  /** Number of medications in this category */
  count: number;
  /** Percentage of total medications */
  percentage: number;
  /** Total stock quantity for this category */
  totalStock?: number;
}

/**
 * Stock trend data point for line chart
 */
export interface StockTrendData {
  /** Date string in ISO format or display format */
  date: string;
  /** Medication ID */
  medicationId: number;
  /** Medication name */
  medicationName: string;
  /** Current stock level */
  stockLevel: number;
  /** Minimum stock level threshold */
  minimumStockLevel?: number;
  /** Category of the medication */
  category?: string;
}

/**
 * Date range parameters for analytics queries
 */
export interface AnalyticsDateRange {
  /** Start date (ISO string or Date) */
  startDate: string | Date;
  /** End date (ISO string or Date) */
  endDate: string | Date;
}

/**
 * Period options for analytics queries
 */
export type AnalyticsPeriod = 
  | 'last7days' 
  | 'last30days' 
  | 'last3months' 
  | 'last6months' 
  | 'last12months' 
  | 'thisMonth' 
  | 'thisYear' 
  | 'custom';

/**
 * Summary statistics from dashboard-stats metadata
 */
export interface DashboardStatisticsSummary {
  totalRevenue?: number;
  totalMedications?: number;
  totalCategories?: number;
  totalPrescriptions?: number;
  pendingPrescriptions?: number;
  lowStockAlerts?: number;
  expiringSoon?: number;
  expiredMedications?: number;
}

/**
 * Raw API response from GET /api/pharmacy/analytics/dashboard-stats
 */
export interface DashboardStatsApiResponse {
  monthlyRevenue?: {
    data?: unknown[];
    totalRevenue?: number;
  };
  topCategories?: {
    data?: unknown[];
    totalCategories?: number;
    totalMedications?: number;
  };
  stockTrends?: {
    data?: unknown[];
    medications?: unknown[];
    timeline?: string[];
  };
  metadata?: {
    generatedAt?: string;
    summary?: DashboardStatisticsSummary;
  };
}

/**
 * Complete dashboard statistics response
 */
export interface DashboardStats {
  /** Monthly revenue data for bar chart */
  monthlyRevenue: MonthlyRevenueData[];
  /** Top medication categories for pie chart */
  topCategories: MedicationCategoryData[];
  /** Stock trends for line chart */
  stockTrends: StockTrendData[];
  /** Additional summary statistics */
  summary?: DashboardStatisticsSummary & {
    averageStockLevel?: number;
  };
}

/**
 * Analytics API response wrapper
 */
export interface AnalyticsResponse<T> {
  /** Response data */
  data: T;
  /** Timestamp of the response */
  timestamp: string;
  /** Cache expiration timestamp */
  cacheExpiry?: string;
}
