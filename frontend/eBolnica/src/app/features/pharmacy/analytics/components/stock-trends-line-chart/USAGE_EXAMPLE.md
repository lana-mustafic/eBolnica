# Stock Trends Line Chart Component - Usage Examples

## Basic Usage

### Simple Implementation

```typescript
import { Component } from '@angular/core';
import { StockTrendsLineChartComponent } from './features/pharmacy/analytics/components/stock-trends-line-chart/stock-trends-line-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [StockTrendsLineChartComponent],
  template: `
    <app-stock-trends-line-chart></app-stock-trends-line-chart>
  `
})
export class DashboardComponent {}
```

### With Pre-selected Medications

```html
<app-stock-trends-line-chart 
  [title]="'Medication Stock Trends'"
  [medicationIds]="[101, 102, 103]"
  [days]="30">
</app-stock-trends-line-chart>
```

### With Custom Thresholds

```html
<app-stock-trends-line-chart 
  [title]="'Stock Level Monitoring'"
  [showThresholds]="true"
  [lowThreshold]="15"
  [criticalThreshold]="40"
  [idealThreshold]="85">
</app-stock-trends-line-chart>
```

### With Fill Area

```html
<app-stock-trends-line-chart 
  [title]="'Stock Trends with Fill'"
  [showFill]="true"
  [lineTension]="0.4">
</app-stock-trends-line-chart>
```

## Integration in Pharmacy Dashboard

### Full Dashboard Example

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StockTrendsLineChartComponent } from '../../features/pharmacy/analytics/components/stock-trends-line-chart/stock-trends-line-chart.component';
import { StockTrendData } from '../../models/analytics.dto';

@Component({
  selector: 'app-pharmacy-dashboard',
  standalone: true,
  imports: [CommonModule, StockTrendsLineChartComponent],
  templateUrl: './pharmacy-dashboard.component.html',
  styleUrl: './pharmacy-dashboard.component.css'
})
export class PharmacyDashboardComponent implements OnInit {
  selectedMedicationIds: number[] = [101, 102, 103];
  days: number = 30;

  ngOnInit(): void {
    // Component initialization
  }

  onStockDataLoaded(data: StockTrendData[]): void {
    console.log('Stock trends loaded:', data.length, 'data points');
  }

  onPointClick(point: StockTrendData): void {
    console.log('Clicked point:', point);
    // Navigate to medication detail or show modal
  }

  onMedicationsChanged(ids: number[]): void {
    console.log('Medications changed:', ids);
    this.selectedMedicationIds = ids;
  }
}
```

```html
<!-- pharmacy-dashboard.component.html -->
<div class="dashboard-content">
  <h2>Pharmacy Analytics Dashboard</h2>

  <!-- Stock Trends Chart -->
  <div class="chart-section">
    <app-stock-trends-line-chart 
      [title]="'Medication Stock Trends'"
      [medicationIds]="selectedMedicationIds"
      [days]="days"
      [showThresholds]="true"
      (dataLoaded)="onStockDataLoaded($event)"
      (pointClicked)="onPointClick($event)"
      (medicationsChanged)="onMedicationsChanged($event)">
    </app-stock-trends-line-chart>
  </div>
</div>
```

## Dynamic Medication Selection

```typescript
import { Component } from '@angular/core';
import { StockTrendsLineChartComponent } from './features/pharmacy/analytics/components/stock-trends-line-chart/stock-trends-line-chart.component';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [StockTrendsLineChartComponent],
  template: `
    <div class="controls">
      <label>
        Time Period:
        <select [(ngModel)]="days" (change)="updateChart()">
          <option [value]="7">Last 7 Days</option>
          <option [value]="14">Last 14 Days</option>
          <option [value]="30">Last 30 Days</option>
          <option [value]="60">Last 60 Days</option>
          <option [value]="90">Last 90 Days</option>
        </select>
      </label>
    </div>

    <app-stock-trends-line-chart 
      [title]="'Stock Trends - Last ' + days + ' Days'"
      [days]="days"
      [showThresholds]="true">
    </app-stock-trends-line-chart>
  `
})
export class AnalyticsComponent {
  days: number = 30;

  updateChart(): void {
    // Chart will automatically update when days changes
  }
}
```

## Drill-Down Example

```typescript
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { StockTrendsLineChartComponent } from './features/pharmacy/analytics/components/stock-trends-line-chart/stock-trends-line-chart.component';
import { StockTrendData } from './models/analytics.dto';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [StockTrendsLineChartComponent],
  template: `
    <app-stock-trends-line-chart 
      [title]="'Medication Stock Trends'"
      (pointClicked)="onPointClick($event)">
    </app-stock-trends-line-chart>
  `
})
export class AnalyticsComponent {
  constructor(private router: Router) {}

  onPointClick(point: StockTrendData): void {
    // Navigate to medication detail page
    this.router.navigate(['/pharmacy/medications', point.medicationId], {
      queryParams: { date: point.date }
    });
    
    // Or show a modal with detailed information
    // this.showMedicationDetailModal(point);
  }
}
```

## Multiple Charts Example

```html
<div class="analytics-grid">
  <!-- Short-term trends -->
  <div class="chart-card">
    <app-stock-trends-line-chart 
      [title]="'Last 7 Days'"
      [days]="7"
      [height]="300">
    </app-stock-trends-line-chart>
  </div>

  <!-- Medium-term trends -->
  <div class="chart-card">
    <app-stock-trends-line-chart 
      [title]="'Last 30 Days'"
      [days]="30"
      [height]="300">
    </app-stock-trends-line-chart>
  </div>

  <!-- Long-term trends -->
  <div class="chart-card">
    <app-stock-trends-line-chart 
      [title]="'Last 90 Days'"
      [days]="90"
      [height]="300">
    </app-stock-trends-line-chart>
  </div>
</div>
```

## Custom Styling Example

```html
<app-stock-trends-line-chart 
  [title]="'Stock Trends'"
  [showFill]="true"
  [lineTension]="0.5"
  [height]="450"
  class="custom-stock-chart">
</app-stock-trends-line-chart>
```

```scss
.custom-stock-chart {
  border: 2px solid #3b82f6;
  
  ::ng-deep .chart-wrapper {
    background: #f7fafc;
  }
}
```

## Event Handling Example

```typescript
import { Component } from '@angular/core';
import { StockTrendsLineChartComponent } from './features/pharmacy/analytics/components/stock-trends-line-chart/stock-trends-line-chart.component';
import { StockTrendData } from './models/analytics.dto';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [StockTrendsLineChartComponent],
  template: `
    <app-stock-trends-line-chart 
      [title]="'Stock Trends'"
      (dataLoaded)="onDataLoaded($event)"
      (errorOccurred)="onError($event)"
      (pointClicked)="onPointClick($event)"
      (medicationsChanged)="onMedicationsChanged($event)">
    </app-stock-trends-line-chart>
  `
})
export class AnalyticsComponent {
  onDataLoaded(data: StockTrendData[]): void {
    console.log('Stock trends loaded:', data);
    // Process data or update other components
  }

  onError(error: Error): void {
    console.error('Error loading stock trends:', error);
    // Show notification or handle error
  }

  onPointClick(point: StockTrendData): void {
    console.log(`Clicked on ${point.medicationName} at ${point.date}: ${point.stockLevel}%`);
    // Navigate to detail page or show modal
  }

  onMedicationsChanged(ids: number[]): void {
    console.log('Medications selected:', ids);
    // Update other components or save selection
  }
}
```

## Input Properties Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `title` | `string` | `'Medication Stock Trends'` | Chart title |
| `height` | `number` | `400` | Chart height in pixels |
| `medicationIds` | `number[]` | `undefined` | Pre-selected medication IDs |
| `days` | `number` | `30` | Number of days to look back |
| `useCache` | `boolean` | `true` | Enable caching |
| `showThresholds` | `boolean` | `true` | Show threshold lines |
| `compareMode` | `boolean` | `true` | Enable comparison mode |
| `lowThreshold` | `number` | `20` | Low stock threshold (%) |
| `criticalThreshold` | `number` | `50` | Critical threshold (%) |
| `idealThreshold` | `number` | `80` | Ideal threshold (%) |
| `showFill` | `boolean` | `false` | Show fill area |
| `lineTension` | `number` | `0.4` | Line curvature |

## Output Events Reference

| Event | Type | Description |
|-------|------|-------------|
| `dataLoaded` | `EventEmitter<StockTrendData[]>` | Emitted when data loads |
| `errorOccurred` | `EventEmitter<Error>` | Emitted on error |
| `pointClicked` | `EventEmitter<StockTrendData>` | Emitted on point click |
| `medicationsChanged` | `EventEmitter<number[]>` | Emitted when selection changes |

## Best Practices

1. **Limit medications** - Compare 3-5 medications for best readability
2. **Use thresholds** - Enable threshold lines for quick visual reference
3. **Appropriate time ranges** - Use 7-90 days depending on analysis needs
4. **Handle events** - Listen to `pointClicked` for drill-down functionality
5. **Enable caching** - Keep `useCache` as `true` for better performance
6. **Responsive design** - Component is responsive, adjust `height` for mobile
7. **Pre-select medications** - Use `medicationIds` input for default selection
