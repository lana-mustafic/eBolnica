# Categories Pie Chart Component - Usage Examples

## Basic Usage

### Simple Implementation

```typescript
import { Component } from '@angular/core';
import { CategoriesPieChartComponent } from './features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CategoriesPieChartComponent],
  template: `
    <app-categories-pie-chart></app-categories-pie-chart>
  `
})
export class DashboardComponent {}
```

### With Custom Title and Chart Type

```html
<app-categories-pie-chart 
  [title]="'Medication Categories Distribution'"
  [chartType]="'doughnut'"
  [height]="400">
</app-categories-pie-chart>
```

### With Maximum Categories Limit

```html
<app-categories-pie-chart 
  [title]="'Top 5 Categories'"
  [maxCategories]="5"
  [chartType]="'pie'">
</app-categories-pie-chart>
```

### With Event Handlers

```typescript
import { Component } from '@angular/core';
import { CategoriesPieChartComponent } from './features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';
import { MedicationCategoryData } from './models/analytics.dto';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CategoriesPieChartComponent],
  template: `
    <app-categories-pie-chart 
      [title]="'Medication Categories'"
      (dataLoaded)="onDataLoaded($event)"
      (errorOccurred)="onError($event)"
      (segmentClicked)="onSegmentClick($event)">
    </app-categories-pie-chart>
  `
})
export class DashboardComponent {
  onDataLoaded(data: MedicationCategoryData[]): void {
    console.log('Category data loaded:', data);
    // Process data or update other components
  }

  onError(error: Error): void {
    console.error('Error loading categories:', error);
    // Show notification or handle error
  }

  onSegmentClick(category: MedicationCategoryData): void {
    console.log(`Clicked on ${category.category}: ${category.count} medications`);
    // Navigate to category detail page or show modal
  }
}
```

## Integration in Pharmacy Dashboard

### Full Dashboard Example

```typescript
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CategoriesPieChartComponent } from '../../features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';

@Component({
  selector: 'app-pharmacy-dashboard',
  standalone: true,
  imports: [CommonModule, CategoriesPieChartComponent],
  templateUrl: './pharmacy-dashboard.component.html',
  styleUrl: './pharmacy-dashboard.component.css'
})
export class PharmacyDashboardComponent implements OnInit {
  chartType: 'pie' | 'doughnut' = 'doughnut';
  maxCategories: number = 8;

  ngOnInit(): void {
    // Component initialization
  }

  onChartTypeChange(type: 'pie' | 'doughnut'): void {
    this.chartType = type;
  }
}
```

```html
<!-- pharmacy-dashboard.component.html -->
<div class="dashboard-content">
  <h2>Pharmacy Analytics Dashboard</h2>
  
  <!-- Chart Type Selector -->
  <div class="chart-type-selector">
    <button 
      [class.active]="chartType === 'pie'"
      (click)="onChartTypeChange('pie')">
      Pie Chart
    </button>
    <button 
      [class.active]="chartType === 'doughnut'"
      (click)="onChartTypeChange('doughnut')">
      Doughnut Chart
    </button>
  </div>

  <!-- Categories Chart -->
  <div class="chart-section">
    <app-categories-pie-chart 
      [title]="'Medication Categories Distribution'"
      [chartType]="chartType"
      [maxCategories]="maxCategories"
      [height]="400"
      (dataLoaded)="onCategoryDataLoaded($event)"
      (segmentClicked)="onCategoryClick($event)">
    </app-categories-pie-chart>
  </div>
</div>
```

## Grid Layout Example

```html
<div class="analytics-grid">
  <div class="chart-card">
    <app-categories-pie-chart 
      [title]="'Categories Distribution'"
      [chartType]="'doughnut'"
      [height]="350">
    </app-categories-pie-chart>
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

## Toggle Chart Type Example

```typescript
import { Component } from '@angular/core';
import { CategoriesPieChartComponent } from './features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CategoriesPieChartComponent],
  template: `
    <div class="controls">
      <button (click)="toggleChartType()">
        Switch to {{ currentChartType === 'pie' ? 'Doughnut' : 'Pie' }} Chart
      </button>
    </div>

    <app-categories-pie-chart 
      [title]="'Medication Categories'"
      [chartType]="currentChartType"
      [height]="400">
    </app-categories-pie-chart>
  `
})
export class AnalyticsComponent {
  currentChartType: 'pie' | 'doughnut' = 'doughnut';

  toggleChartType(): void {
    this.currentChartType = this.currentChartType === 'pie' ? 'doughnut' : 'pie';
  }
}
```

## Dynamic Category Limit Example

```typescript
import { Component } from '@angular/core';
import { CategoriesPieChartComponent } from './features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CategoriesPieChartComponent],
  template: `
    <div class="controls">
      <label>
        Show Top Categories:
        <select [(ngModel)]="maxCategories" (change)="updateChart()">
          <option [value]="5">Top 5</option>
          <option [value]="8">Top 8</option>
          <option [value]="10">Top 10</option>
          <option [value]="15">Top 15</option>
        </select>
      </label>
    </div>

    <app-categories-pie-chart 
      [title]="'Top ' + maxCategories + ' Categories'"
      [maxCategories]="maxCategories"
      [height]="400">
    </app-categories-pie-chart>
  `
})
export class AnalyticsComponent {
  maxCategories: number = 8;

  updateChart(): void {
    // Chart will automatically update when maxCategories changes
  }
}
```

## Drill-Down Example

```typescript
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CategoriesPieChartComponent } from './features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';
import { MedicationCategoryData } from './models/analytics.dto';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CategoriesPieChartComponent],
  template: `
    <app-categories-pie-chart 
      [title]="'Medication Categories'"
      (segmentClicked)="onCategoryClick($event)">
    </app-categories-pie-chart>
  `
})
export class AnalyticsComponent {
  constructor(private router: Router) {}

  onCategoryClick(category: MedicationCategoryData): void {
    // Navigate to category detail page
    this.router.navigate(['/pharmacy/medications'], {
      queryParams: { category: category.category }
    });
    
    // Or show a modal with category details
    // this.showCategoryModal(category);
  }
}
```

## Responsive Example

```html
<div class="dashboard-container">
  <!-- Desktop: Full width -->
  <div class="chart-desktop">
    <app-categories-pie-chart 
      [title]="'Categories Distribution'"
      [chartType]="'doughnut'"
      [height]="400">
    </app-categories-pie-chart>
  </div>

  <!-- Mobile: Smaller height -->
  <div class="chart-mobile">
    <app-categories-pie-chart 
      [title]="'Categories Distribution'"
      [chartType]="'pie'"
      [height]="300">
    </app-categories-pie-chart>
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

## Input Properties Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `title` | `string` | `'Medication Categories Distribution'` | Chart title |
| `height` | `number` | `400` | Chart height in pixels |
| `chartType` | `'pie' \| 'doughnut'` | `'doughnut'` | Chart type |
| `maxCategories` | `number` | `10` | Maximum categories to display |
| `useCache` | `boolean` | `true` | Enable caching |
| `showLabels` | `boolean` | `true` | Show percentage labels |
| `showCenterLabel` | `boolean` | `true` | Show center label (doughnut) |

## Output Events Reference

| Event | Type | Description |
|-------|------|-------------|
| `dataLoaded` | `EventEmitter<MedicationCategoryData[]>` | Emitted when data loads |
| `errorOccurred` | `EventEmitter<Error>` | Emitted on error |
| `segmentClicked` | `EventEmitter<MedicationCategoryData>` | Emitted on segment click |

## Best Practices

1. **Use doughnut for better UX** - Center label provides quick summary
2. **Limit categories** - 8-10 categories for best readability
3. **Handle segment clicks** - Use for drill-down navigation
4. **Enable caching** - Keep `useCache` as `true` for performance
5. **Responsive design** - Component is responsive, adjust `height` for mobile
6. **Accessibility** - Component includes accessible category list
