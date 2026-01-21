# Pharmacy Analytics Service Documentation

## Overview

The Pharmacy Analytics Service provides methods to fetch analytics data for the pharmacy dashboard charts. It includes retry logic, caching, error handling, and fallback mock data.

## Location

- **Service**: `src/app/shared/services/pharmacy/pharmacy.service.ts`
- **DTOs**: `src/app/models/analytics.dto.ts`
- **Example Component**: `src/app/pages/pharmacy/analytics/analytics-example.component.ts`

## Installation & Setup

The analytics methods are part of the existing `PharmacyService`. No additional setup is required.

### Dependencies

- `@angular/common/http` - HttpClient
- `rxjs` - Observable operators (retry, catchError, tap, forkJoin)
- Chart.js & ng2-charts (already installed)

## API Endpoints

The service expects the following backend endpoints:

- `GET /api/pharmacy/analytics/monthly-revenue` - Monthly revenue data
- `GET /api/pharmacy/analytics/top-categories` - Top medication categories
- `GET /api/pharmacy/analytics/stock-trends` - Stock level trends
- `GET /api/pharmacy/analytics/dashboard-stats` - All stats combined (optional)

**Note**: If endpoints don't exist yet, the service will return mock data as fallback.

## Methods

### 1. getMonthlyRevenue()

Fetches monthly revenue data for bar chart visualization.

**Signature:**
```typescript
getMonthlyRevenue(
  period?: AnalyticsPeriod, 
  dateRange?: AnalyticsDateRange,
  useCache: boolean = true
): Observable<MonthlyRevenueData[]>
```

**Parameters:**
- `period` (optional): Predefined period - `'last7days' | 'last30days' | 'last3months' | 'last6months' | 'last12months' | 'thisMonth' | 'thisYear' | 'custom'`
- `dateRange` (optional): Custom date range object with `startDate` and `endDate`
- `useCache` (default: true): Whether to use cached data if available

**Returns:** Observable of `MonthlyRevenueData[]`

**Example:**
```typescript
// Using predefined period
this.pharmacyService.getMonthlyRevenue('last12months').subscribe({
  next: (data) => {
    console.log('Monthly revenue:', data);
    // data = [{ month: 'January', revenue: 45000, ... }, ...]
  },
  error: (error) => {
    console.error('Error:', error);
  }
});

// Using custom date range
const dateRange = {
  startDate: new Date('2024-01-01'),
  endDate: new Date('2024-12-31')
};
this.pharmacyService.getMonthlyRevenue('custom', dateRange).subscribe(...);

// Force fresh data (skip cache)
this.pharmacyService.getMonthlyRevenue('last12months', undefined, false).subscribe(...);
```

---

### 2. getTopMedicationCategories()

Fetches top medication categories data for pie chart visualization.

**Signature:**
```typescript
getTopMedicationCategories(
  limit: number = 10,
  useCache: boolean = true
): Observable<MedicationCategoryData[]>
```

**Parameters:**
- `limit` (default: 10): Maximum number of categories to return
- `useCache` (default: true): Whether to use cached data if available

**Returns:** Observable of `MedicationCategoryData[]`

**Example:**
```typescript
// Get top 10 categories (default)
this.pharmacyService.getTopMedicationCategories().subscribe({
  next: (categories) => {
    console.log('Top categories:', categories);
    // categories = [{ category: 'Antibiotics', count: 45, percentage: 25.5, ... }, ...]
  }
});

// Get top 5 categories
this.pharmacyService.getTopMedicationCategories(5).subscribe(...);
```

---

### 3. getStockTrends()

Fetches stock level trends data for line chart visualization.

**Signature:**
```typescript
getStockTrends(
  medicationIds?: number[],
  days: number = 30,
  useCache: boolean = true
): Observable<StockTrendData[]>
```

**Parameters:**
- `medicationIds` (optional): Array of medication IDs to filter. If empty/undefined, returns all medications
- `days` (default: 30): Number of days to look back
- `useCache` (default: true): Whether to use cached data if available

**Returns:** Observable of `StockTrendData[]`

**Example:**
```typescript
// Get trends for all medications (last 30 days)
this.pharmacyService.getStockTrends().subscribe({
  next: (trends) => {
    console.log('Stock trends:', trends);
    // trends = [{ date: '2024-01-01', medicationId: 1, medicationName: 'Paracetamol', stockLevel: 150, ... }, ...]
  }
});

// Get trends for specific medications
this.pharmacyService.getStockTrends([1, 2, 3], 60).subscribe(...);

// Get trends for last 7 days
this.pharmacyService.getStockTrends(undefined, 7).subscribe(...);
```

---

### 4. getDashboardStats()

Fetches all dashboard statistics at once (recommended for dashboard initialization).

**Signature:**
```typescript
getDashboardStats(
  period?: AnalyticsPeriod,
  dateRange?: AnalyticsDateRange,
  categoryLimit: number = 10,
  stockTrendDays: number = 30,
  useCache: boolean = true
): Observable<DashboardStats>
```

**Parameters:**
- `period` (optional): Period for revenue data
- `dateRange` (optional): Custom date range for revenue data
- `categoryLimit` (default: 10): Limit for categories
- `stockTrendDays` (default: 30): Days for stock trends
- `useCache` (default: true): Whether to use cached data

**Returns:** Observable of `DashboardStats` with all data plus summary statistics

**Example:**
```typescript
// Get all stats with defaults
this.pharmacyService.getDashboardStats().subscribe({
  next: (stats) => {
    console.log('Monthly revenue:', stats.monthlyRevenue);
    console.log('Top categories:', stats.topCategories);
    console.log('Stock trends:', stats.stockTrends);
    console.log('Summary:', stats.summary);
    // stats.summary = { totalRevenue: 500000, totalMedications: 200, ... }
  }
});

// Custom parameters
this.pharmacyService.getDashboardStats(
  'last6months',
  undefined,
  15,  // top 15 categories
  60   // 60 days of stock trends
).subscribe(...);
```

---

### 5. clearAnalyticsCache()

Clears all cached analytics data. Useful when data needs to be refreshed.

**Signature:**
```typescript
clearAnalyticsCache(): void
```

**Example:**
```typescript
// Clear cache and reload data
this.pharmacyService.clearAnalyticsCache();
this.loadData();
```

---

## Data Types

### MonthlyRevenueData
```typescript
interface MonthlyRevenueData {
  month: string;              // "January", "February", etc.
  monthAbbr?: string;         // "Jan", "Feb", etc.
  revenue: number;             // Revenue amount
  transactionCount?: number;   // Number of transactions
}
```

### MedicationCategoryData
```typescript
interface MedicationCategoryData {
  category: string;            // "Antibiotics", "Pain Relief", etc.
  count: number;              // Number of medications
  percentage: number;         // Percentage of total
  totalStock?: number;         // Total stock quantity
}
```

### StockTrendData
```typescript
interface StockTrendData {
  date: string;               // ISO date string
  medicationId: number;       // Medication ID
  medicationName: string;      // Medication name
  stockLevel: number;         // Current stock level
  minimumStockLevel?: number; // Minimum threshold
  category?: string;          // Medication category
}
```

### DashboardStats
```typescript
interface DashboardStats {
  monthlyRevenue: MonthlyRevenueData[];
  topCategories: MedicationCategoryData[];
  stockTrends: StockTrendData[];
  summary?: {
    totalRevenue?: number;
    totalMedications?: number;
    totalCategories?: number;
    averageStockLevel?: number;
  };
}
```

---

## Features

### 1. Retry Logic

All methods include automatic retry logic:
- **3 retry attempts** for failed requests
- Uses RxJS `retry()` operator
- Logs retry attempts to console

### 2. Caching

- **1-hour cache** for all analytics data
- Stored in `localStorage`
- Cache keys include parameters for uniqueness
- Automatic cache expiration
- Can be disabled per request with `useCache: false`

### 3. Error Handling

- Graceful error handling with fallback to mock data
- User-friendly error messages
- Console logging for debugging
- Never throws errors - always returns data (mock if needed)

### 4. Mock Data

If the backend API is unavailable or returns errors, the service automatically falls back to mock data:
- Realistic sample data
- Same structure as real API responses
- Useful for development and testing

---

## Usage in Components

### Basic Example

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { MonthlyRevenueData } from '../../../models/analytics.dto';

@Component({
  selector: 'app-my-component',
  standalone: true,
  // ...
})
export class MyComponent implements OnInit {
  private pharmacyService = inject(PharmacyService);
  
  monthlyRevenue: MonthlyRevenueData[] = [];
  isLoading = false;

  ngOnInit(): void {
    this.loadRevenue();
  }

  loadRevenue(): void {
    this.isLoading = true;
    this.pharmacyService.getMonthlyRevenue('last12months').subscribe({
      next: (data) => {
        this.monthlyRevenue = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error:', error);
        this.isLoading = false;
      }
    });
  }
}
```

### Using with Chart.js

See `analytics-example.component.ts` for complete Chart.js integration examples.

### Loading All Stats

```typescript
loadDashboard(): void {
  this.isLoading = true;
  
  this.pharmacyService.getDashboardStats(
    'last12months',
    undefined,
    10,
    30
  ).subscribe({
    next: (stats) => {
      this.monthlyRevenue = stats.monthlyRevenue;
      this.topCategories = stats.topCategories;
      this.stockTrends = stats.stockTrends;
      this.summary = stats.summary;
      this.isLoading = false;
    },
    error: (error) => {
      console.error('Error:', error);
      this.isLoading = false;
    }
  });
}
```

---

## Best Practices

1. **Use `getDashboardStats()`** when loading multiple charts - it's more efficient than individual calls
2. **Enable caching** for better performance (default behavior)
3. **Clear cache** when data needs to be refreshed (e.g., after updates)
4. **Handle loading states** - show loading indicators while fetching
5. **Handle errors gracefully** - the service provides fallback data, but you should still handle errors
6. **Use appropriate periods** - don't fetch more data than needed
7. **Limit category results** - use reasonable limits (5-15) for better chart readability

---

## Testing

### Unit Testing

```typescript
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { PharmacyService } from './pharmacy.service';

describe('PharmacyService Analytics', () => {
  let service: PharmacyService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(PharmacyService);
  });

  it('should fetch monthly revenue', (done) => {
    service.getMonthlyRevenue('last12months', undefined, false).subscribe({
      next: (data) => {
        expect(data).toBeDefined();
        expect(Array.isArray(data)).toBe(true);
        done();
      }
    });
  });
});
```

### Mock Data Testing

The service includes mock data methods that can be used for testing:
- `getMockMonthlyRevenue()`
- `getMockTopCategories()`
- `getMockStockTrends()`

---

## Troubleshooting

### Charts not updating
- Clear cache: `pharmacyService.clearAnalyticsCache()`
- Check if `useCache` is set to `false`
- Verify data is being received: check console logs

### API errors
- Check backend endpoint availability
- Verify authentication token is valid
- Check network connectivity
- Service will fallback to mock data automatically

### Cache issues
- Clear browser localStorage
- Call `clearAnalyticsCache()`
- Check browser console for storage quota errors

### Performance issues
- Use `getDashboardStats()` instead of multiple individual calls
- Enable caching (default)
- Reduce data range (fewer days/months)
- Limit category results

---

## Backend Integration

### Expected Request Format

**Monthly Revenue:**
```
GET /api/pharmacy/analytics/monthly-revenue?period=last12months
GET /api/pharmacy/analytics/monthly-revenue?startDate=2024-01-01&endDate=2024-12-31
```

**Top Categories:**
```
GET /api/pharmacy/analytics/top-categories?limit=10
```

**Stock Trends:**
```
GET /api/pharmacy/analytics/stock-trends?days=30
GET /api/pharmacy/analytics/stock-trends?days=30&medicationIds=1&medicationIds=2
```

### Expected Response Format

**Monthly Revenue Response:**
```json
[
  {
    "month": "January",
    "monthAbbr": "Jan",
    "revenue": 45000,
    "transactionCount": 150
  },
  ...
]
```

**Top Categories Response:**
```json
[
  {
    "category": "Antibiotics",
    "count": 45,
    "percentage": 25.5,
    "totalStock": 2250
  },
  ...
]
```

**Stock Trends Response:**
```json
[
  {
    "date": "2024-01-01",
    "medicationId": 1,
    "medicationName": "Paracetamol 500mg",
    "stockLevel": 150,
    "minimumStockLevel": 100,
    "category": "Pain Relief"
  },
  ...
]
```

---

## Example Component

See `src/app/pages/pharmacy/analytics/analytics-example.component.ts` for a complete working example with:
- All three chart types (bar, pie, line)
- Loading states
- Error handling
- Period selection
- Cache management
- Summary statistics

---

## Version History

- **v1.0.0** (Current)
  - Initial implementation
  - All four analytics methods
  - Caching support
  - Retry logic
  - Mock data fallback
  - Complete TypeScript types

---

## Support

For issues or questions:
1. Check this documentation
2. Review example component
3. Check browser console for error logs
4. Verify backend endpoints are available
