# Chart.js Setup Documentation

## Overview
This document describes how Chart.js is configured and used in the eBolnica Angular application.

## Installation

Chart.js and ng2-charts have been installed:
- **chart.js**: ^4.5.1 - Core Chart.js library
- **ng2-charts**: ^8.0.0 - Angular wrapper for Chart.js

```bash
npm install chart.js ng2-charts --save
```

## Configuration

### Angular 19 Standalone Components Setup

This application uses Angular 19 with standalone components. Chart.js is configured to work with standalone components without requiring module providers.

**No global configuration needed** - Chart.js can be imported directly in components.

## Usage in Components

### Basic Setup

1. **Import required modules** in your component:

```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
```

2. **Add BaseChartDirective to imports** in your component decorator:

```typescript
@Component({
  selector: 'app-my-component',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './my-component.html',
  styleUrl: './my-component.css'
})
```

3. **Use the chart directive** in your template:

```html
<canvas baseChart
  [data]="chartData"
  [type]="chartType"
  [options]="chartOptions">
</canvas>
```

4. **Define chart properties** in your component:

```typescript
export class MyComponent {
  // Chart type: 'line', 'bar', 'pie', 'doughnut', 'radar', 'polarArea', 'bubble', 'scatter'
  public chartType: ChartType = 'bar';

  // Chart data
  public chartData: ChartData<'bar'> = {
    labels: ['Label 1', 'Label 2', 'Label 3'],
    datasets: [{
      label: 'Dataset',
      data: [10, 20, 30],
      backgroundColor: 'rgba(54, 162, 235, 0.6)',
      borderColor: 'rgba(54, 162, 235, 1)',
      borderWidth: 1
    }]
  };

  // Chart options
  public chartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true
      }
    }
  };
}
```

## Chart Types

Supported chart types:
- `'line'` - Line chart
- `'bar'` - Bar chart
- `'pie'` - Pie chart
- `'doughnut'` - Doughnut chart
- `'radar'` - Radar chart
- `'polarArea'` - Polar area chart
- `'bubble'` - Bubble chart
- `'scatter'` - Scatter chart

## Example: Pharmacy Dashboard Chart

Here's an example for displaying prescription statistics:

```typescript
import { Component, OnInit } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { PharmacyService } from '../../shared/services/pharmacy/pharmacy.service';

@Component({
  selector: 'app-prescription-chart',
  standalone: true,
  imports: [BaseChartDirective],
  template: `
    <div class="chart-wrapper">
      <canvas baseChart
        [data]="chartData"
        [type]="chartType"
        [options]="chartOptions">
      </canvas>
    </div>
  `
})
export class PrescriptionChartComponent implements OnInit {
  public chartType: ChartType = 'bar';
  
  public chartData: ChartData<'bar'> = {
    labels: [],
    datasets: [{
      label: 'Prescriptions by Status',
      data: [],
      backgroundColor: [
        'rgba(255, 99, 132, 0.6)',  // Pending - Red
        'rgba(54, 162, 235, 0.6)',  // Dispensed - Blue
        'rgba(75, 192, 192, 0.6)'   // Cancelled - Teal
      ]
    }]
  };

  public chartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true,
        position: 'top'
      },
      title: {
        display: true,
        text: 'Prescription Status Overview'
      }
    }
  };

  constructor(private pharmacyService: PharmacyService) {}

  ngOnInit(): void {
    this.loadChartData();
  }

  loadChartData(): void {
    this.pharmacyService.getPrescriptions().subscribe(prescriptions => {
      const statusCounts = this.calculateStatusCounts(prescriptions.items);
      
      this.chartData = {
        labels: Object.keys(statusCounts),
        datasets: [{
          label: 'Prescriptions by Status',
          data: Object.values(statusCounts),
          backgroundColor: [
            'rgba(255, 99, 132, 0.6)',
            'rgba(54, 162, 235, 0.6)',
            'rgba(75, 192, 192, 0.6)'
          ]
        }]
      };
    });
  }

  private calculateStatusCounts(prescriptions: any[]): { [key: string]: number } {
    return prescriptions.reduce((acc, prescription) => {
      acc[prescription.status] = (acc[prescription.status] || 0) + 1;
      return acc;
    }, {});
  }
}
```

## Advanced Configuration

### Custom Colors

You can customize colors per dataset or per data point:

```typescript
datasets: [{
  data: [10, 20, 30],
  backgroundColor: [
    'rgba(255, 99, 132, 0.6)',
    'rgba(54, 162, 235, 0.6)',
    'rgba(75, 192, 192, 0.6)'
  ]
}]
```

### Responsive Charts

Charts are responsive by default. You can control sizing:

```typescript
chartOptions: ChartConfiguration<'bar'>['options'] = {
  responsive: true,
  maintainAspectRatio: false, // Set to false for custom height
  // ... other options
};
```

### Updating Chart Data

To update chart data dynamically:

```typescript
updateChartData(newData: number[]): void {
  this.chartData = {
    ...this.chartData,
    datasets: [{
      ...this.chartData.datasets[0],
      data: newData
    }]
  };
}
```

## TypeScript Types

Chart.js provides full TypeScript support:

```typescript
import { 
  ChartConfiguration, 
  ChartData, 
  ChartType,
  ChartOptions,
  ChartDataset
} from 'chart.js';
```

## Testing

A test component has been created at:
`src/app/shared/components/chart-test/chart-test.component.ts`

You can use this component to verify Chart.js is working correctly.

## Production Build

Chart.js is configured for production builds with tree-shaking support. The library will only include the chart types and features you actually use.

## Resources

- [Chart.js Documentation](https://www.chartjs.org/docs/latest/)
- [ng2-charts Documentation](https://valor-software.com/ng2-charts/)
- [Chart.js TypeScript Types](https://www.chartjs.org/docs/latest/getting-started/typescript.html)

## Troubleshooting

### Chart not displaying
- Ensure `BaseChartDirective` is imported in component imports
- Check that `[data]`, `[type]`, and `[options]` are properly bound
- Verify Chart.js and ng2-charts are installed: `npm list chart.js ng2-charts`

### Type errors
- Ensure you're importing types from `chart.js`: `import { ChartType, ChartData } from 'chart.js'`
- Use proper generic types: `ChartData<'bar'>` instead of `ChartData`

### Build errors
- Clear node_modules and reinstall: `rm -rf node_modules package-lock.json && npm install`
- Check Angular version compatibility (requires Angular 15+)

## Version Compatibility

- Angular: ^19.2.0
- TypeScript: ~5.7.2
- Chart.js: ^4.5.1
- ng2-charts: ^8.0.0

All versions are compatible and tested.
