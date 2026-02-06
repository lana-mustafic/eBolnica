# Revenue Bar Chart Component - Usage Examples

## Basic Usage

### Simple Implementation

```typescript
import { Component } from '@angular/core';
import { RevenueBarChartComponent } from './features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RevenueBarChartComponent],
  template: `
    <app-revenue-bar-chart></app-revenue-bar-chart>
  `
})
export class DashboardComponent {}
```

### With Custom Title and Height

```html
<app-revenue-bar-chart 
  [title]="'Monthly Revenue Overview'"
  [height]="400">
</app-revenue-bar-chart>
```

### With Custom Period

```html
<app-revenue-bar-chart 
  [title]="'Last 6 Months Revenue'"
  [period]="'last6months'"
  [height]="350">
</app-revenue-bar-chart>
```

### With Custom Date Range

```typescript
import { Component } from '@angular/core';
import { RevenueBarChartComponent } from './features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RevenueBarChartComponent],
  template: `
    <app-revenue-bar-chart 
      [title]="'Q1 2024 Revenue'"
      [dateRange]="q1DateRange"
      [height]="400">
    </app-revenue-bar-chart>
  `
})
export class DashboardComponent {
  q1DateRange = {
    startDate: new Date('2024-01-01'),
    endDate: new Date('2024-03-31')
  };
}
```

### With Custom Colors

```html
<app-revenue-bar-chart 
  [title]="'Monthly Revenue'"
  [barColor]="'#10b981'"
  [barHoverColor]="'#059669'"
  [height]="400">
</app-revenue-bar-chart>
```

### With Event Handlers

```typescript
import { Component } from '@angular/core';
import { RevenueBarChartComponent } from './features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';
import { MonthlyRevenueData } from './models/analytics.dto';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RevenueBarChartComponent],
  template: `
    <app-revenue-bar-chart 
      [title]="'Monthly Revenue'"
      (dataLoaded)="onDataLoaded($event)"
      (errorOccurred)="onError($event)"
      (barClicked)="onBarClick($event)">
    </app-revenue-bar-chart>
  `
})
export class DashboardComponent {
  onDataLoaded(data: MonthlyRevenueData[]): void {
    console.log('Revenue data loaded:', data);
    // Process data or update other components
  }

  onError(error: Error): void {
    console.error('Error loading revenue:', error);
    // Show notification or handle error
  }

  onBarClick(event: { month: string; revenue: number }): void {
    console.log(`Clicked on ${event.month}: $${event.revenue}`);
    // Navigate to detail page or show modal
  }
}
```

## Integration in Pharmacy Dashboard

### Full Dashboard Example

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RevenueBarChartComponent } from '../../features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';

@Component({
  selector: 'app-pharmacy-dashboard',
  standalone: true,
  imports: [CommonModule, RevenueBarChartComponent],
  templateUrl: './pharmacy-dashboard.component.html',
  styleUrl: './pharmacy-dashboard.component.css'
})
export class PharmacyDashboardComponent implements OnInit {
  selectedPeriod: 'last6months' | 'last12months' = 'last12months';

  ngOnInit(): void {
    // Component initialization
  }

  onPeriodChange(period: 'last6months' | 'last12months'): void {
    this.selectedPeriod = period;
  }
}
```

```html
<!-- pharmacy-dashboard.component.html -->
<div class="dashboard-content">
  <h2>Pharmacy Analytics Dashboard</h2>
  
  <!-- Period Selector -->
  <div class="period-selector">
    <button 
      [class.active]="selectedPeriod === 'last6months'"
      (click)="onPeriodChange('last6months')">
      Last 6 Months
    </button>
    <button 
      [class.active]="selectedPeriod === 'last12months'"
      (click)="onPeriodChange('last12months')">
      Last 12 Months
    </button>
  </div>

  <!-- Revenue Chart -->
  <div class="chart-section">
    <app-revenue-bar-chart 
      [title]="'Monthly Revenue Overview'"
      [period]="selectedPeriod"
      [height]="400"
      (dataLoaded)="onRevenueDataLoaded($event)">
    </app-revenue-bar-chart>
  </div>
</div>
```

## Grid Layout Example

```html
<div class="analytics-grid">
  <div class="chart-card">
    <app-revenue-bar-chart 
      [title]="'Monthly Revenue'"
      [height]="350">
    </app-revenue-bar-chart>
  </div>
  
  <div class="chart-card">
    <!-- Other chart components -->
  </div>
</div>
```

```scss
.analytics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 1.5rem;
  margin: 2rem 0;
}

.chart-card {
  background: white;
  border-radius: 12px;
  overflow: hidden;
}
```

## Responsive Example

```html
<div class="dashboard-container">
  <!-- Desktop: Full width -->
  <div class="chart-desktop">
    <app-revenue-bar-chart 
      [title]="'Monthly Revenue'"
      [height]="400">
    </app-revenue-bar-chart>
  </div>

  <!-- Mobile: Stacked -->
  <div class="chart-mobile">
    <app-revenue-bar-chart 
      [title]="'Monthly Revenue'"
      [height]="300">
    </app-revenue-bar-chart>
  </div>
</div>
```

```scss
.chart-desktop {
  display: block;
  
  @media (max-width: 768px) {
    display: none;
  }
}

.chart-mobile {
  display: none;
  
  @media (max-width: 768px) {
    display: block;
  }
}
```

## Advanced: Dynamic Period Selection

```typescript
import { Component } from '@angular/core';
import { RevenueBarChartComponent } from './features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';
import { AnalyticsPeriod } from './models/analytics.dto';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [RevenueBarChartComponent],
  template: `
    <div class="controls">
      <select [(ngModel)]="selectedPeriod" (change)="updateChart()">
        <option value="last7days">Last 7 Days</option>
        <option value="last30days">Last 30 Days</option>
        <option value="last3months">Last 3 Months</option>
        <option value="last6months">Last 6 Months</option>
        <option value="last12months">Last 12 Months</option>
        <option value="thisYear">This Year</option>
      </select>
    </div>

    <app-revenue-bar-chart 
      [title]="getChartTitle()"
      [period]="selectedPeriod"
      [height]="400">
    </app-revenue-bar-chart>
  `
})
export class AnalyticsComponent {
  selectedPeriod: AnalyticsPeriod = 'last12months';

  getChartTitle(): string {
    const titles: Record<AnalyticsPeriod, string> = {
      'last7days': 'Last 7 Days Revenue',
      'last30days': 'Last 30 Days Revenue',
      'last3months': 'Last 3 Months Revenue',
      'last6months': 'Last 6 Months Revenue',
      'last12months': 'Last 12 Months Revenue',
      'thisMonth': 'This Month Revenue',
      'thisYear': 'This Year Revenue',
      'custom': 'Custom Period Revenue'
    };
    return titles[this.selectedPeriod] || 'Monthly Revenue';
  }

  updateChart(): void {
    // Chart will automatically update when period changes
  }
}
```

## Input Properties Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `title` | `string` | `'Monthly Revenue'` | Chart title displayed above |
| `height` | `number` | `400` | Chart container height in pixels |
| `period` | `AnalyticsPeriod` | `'last12months'` | Predefined period for data |
| `dateRange` | `{startDate: Date, endDate: Date}` | `undefined` | Custom date range (overrides period) |
| `useCache` | `boolean` | `true` | Whether to use cached data |
| `barColor` | `string` | `'#3b82f6'` | Bar color (pharmacy blue) |
| `barHoverColor` | `string` | `'#2563eb'` | Bar hover color |

## Output Events Reference

| Event | Type | Description |
|-------|------|-------------|
| `dataLoaded` | `EventEmitter<MonthlyRevenueData[]>` | Emitted when data loads successfully |
| `errorOccurred` | `EventEmitter<Error>` | Emitted when an error occurs |
| `barClicked` | `EventEmitter<{month: string, revenue: number}>` | Emitted when a bar is clicked |

## Best Practices

1. **Use appropriate height**: 300-400px for dashboards, 400-500px for detail pages
2. **Handle events**: Listen to `dataLoaded` and `errorOccurred` for better UX
3. **Customize colors**: Match your brand colors using `barColor` and `barHoverColor`
4. **Use caching**: Keep `useCache` as `true` for better performance (default)
5. **Responsive design**: Component is responsive, but adjust `height` for mobile if needed
6. **Accessibility**: Component includes ARIA labels, but ensure parent containers are accessible
