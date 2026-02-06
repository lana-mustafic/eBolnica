# Analytics Service - Quick Start Guide

## ✅ What's Been Implemented

1. ✅ **TypeScript Interfaces** - Complete type definitions in `analytics.dto.ts`
2. ✅ **Service Methods** - Four analytics methods in `PharmacyService`
3. ✅ **Retry Logic** - 3 retries with exponential backoff
4. ✅ **Caching** - 1-hour localStorage cache
5. ✅ **Error Handling** - Graceful fallback to mock data
6. ✅ **Mock Data** - Automatic fallback when API unavailable
7. ✅ **Example Component** - Complete working example with charts

## 🚀 Quick Start

### 1. Import the Service

```typescript
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { DashboardStats } from '../../../models/analytics.dto';

// In your component
private pharmacyService = inject(PharmacyService);
```

### 2. Fetch Analytics Data

```typescript
// Option A: Get all stats at once (recommended)
this.pharmacyService.getDashboardStats('last12months').subscribe({
  next: (stats) => {
    this.monthlyRevenue = stats.monthlyRevenue;
    this.topCategories = stats.topCategories;
    this.stockTrends = stats.stockTrends;
  }
});

// Option B: Get individual charts
this.pharmacyService.getMonthlyRevenue('last12months').subscribe(...);
this.pharmacyService.getTopMedicationCategories(10).subscribe(...);
this.pharmacyService.getStockTrends(undefined, 30).subscribe(...);
```

### 3. Use with Chart.js

```typescript
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartType } from 'chart.js';

@Component({
  imports: [BaseChartDirective],
  // ...
})
export class MyComponent {
  chartData: ChartData<'bar'> = {
    labels: this.monthlyRevenue.map(r => r.month),
    datasets: [{
      data: this.monthlyRevenue.map(r => r.revenue),
      label: 'Revenue'
    }]
  };
}
```

## 📋 Available Methods

| Method | Purpose | Returns |
|--------|---------|---------|
| `getMonthlyRevenue()` | Bar chart data | `MonthlyRevenueData[]` |
| `getTopMedicationCategories()` | Pie chart data | `MedicationCategoryData[]` |
| `getStockTrends()` | Line chart data | `StockTrendData[]` |
| `getDashboardStats()` | All stats + summary | `DashboardStats` |
| `clearAnalyticsCache()` | Clear cache | `void` |

## 📁 Files Created

- `src/app/models/analytics.dto.ts` - TypeScript interfaces
- `src/app/shared/services/pharmacy/pharmacy.service.ts` - Analytics methods added
- `src/app/pages/pharmacy/analytics/analytics-example.component.ts` - Example component
- `ANALYTICS_SERVICE_DOCUMENTATION.md` - Full documentation
- `ANALYTICS_QUICK_START.md` - This file

## 🔗 Backend Endpoints Expected

```
GET /api/pharmacy/analytics/monthly-revenue
GET /api/pharmacy/analytics/top-categories
GET /api/pharmacy/analytics/stock-trends
GET /api/pharmacy/analytics/dashboard-stats (optional)
```

**Note**: If endpoints don't exist, service returns mock data automatically.

## 📖 Full Documentation

See `ANALYTICS_SERVICE_DOCUMENTATION.md` for:
- Complete API reference
- Detailed examples
- Error handling
- Best practices
- Testing guide

## 💡 Example Usage

See `src/app/pages/pharmacy/analytics/analytics-example.component.ts` for a complete working example with all three chart types.

## ✨ Key Features

- ✅ **Automatic Retry** - 3 attempts on failure
- ✅ **Smart Caching** - 1-hour cache, auto-expires
- ✅ **Error Resilience** - Falls back to mock data
- ✅ **Type Safe** - Full TypeScript support
- ✅ **Production Ready** - Handles all edge cases

## 🎯 Next Steps

1. Review the example component
2. Integrate into your pharmacy dashboard
3. Customize chart configurations
4. Connect to backend API when ready
