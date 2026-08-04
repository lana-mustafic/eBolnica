# Stock Trends Line Chart Component

A reusable Angular component for displaying medication stock level trends over time using a line chart. Supports multiple medication comparison, threshold lines, and time-series visualization.

## Features

✅ **Time-Series Visualization** - Date-based X-axis with automatic formatting  
✅ **Multiple Medication Comparison** - Compare up to 5 medications simultaneously  
✅ **Threshold Lines** - Visual indicators for low, critical, and ideal stock levels  
✅ **Interactive Medication Selector** - Add/remove medications for comparison  
✅ **Real-time Data** - Fetches data from analytics service  
✅ **Percentage Change** - Shows trend direction in tooltips  
✅ **Loading States** - Shows spinner while loading  
✅ **Error Handling** - Displays error messages with retry option  
✅ **Empty States** - Handles no data scenarios gracefully  
✅ **Responsive Design** - Adapts to mobile, tablet, and desktop  
✅ **Accessibility** - ARIA labels, keyboard navigation, screen reader support  
✅ **Performance Optimized** - Handles large datasets efficiently  

## Installation

The component is already created and ready to use. No additional installation required.

## Quick Start

### Basic Usage

```typescript
import { StockTrendsLineChartComponent } from './features/pharmacy/analytics/components/stock-trends-line-chart/stock-trends-line-chart.component';

@Component({
  imports: [StockTrendsLineChartComponent],
  template: `
    <app-stock-trends-line-chart></app-stock-trends-line-chart>
  `
})
```

### With Medication Selection

```html
<app-stock-trends-line-chart 
  [title]="'Medication Stock Trends'"
  [medicationIds]="[101, 102, 103]"
  [days]="30"
  [showThresholds]="true">
</app-stock-trends-line-chart>
```

## Component Structure

```
stock-trends-line-chart/
├── stock-trends-line-chart.component.ts    # Component logic
├── stock-trends-line-chart.component.html  # Template
├── stock-trends-line-chart.component.scss  # Styles
├── README.md                               # This file
└── USAGE_EXAMPLE.md                        # Usage examples
```

## Input Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `title` | `string` | `'Medication Stock Trends'` | Chart title |
| `height` | `number` | `400` | Chart height in pixels |
| `medicationIds` | `number[]` | `undefined` | Array of medication IDs (max 5) |
| `days` | `number` | `30` | Number of days to look back |
| `useCache` | `boolean` | `true` | Enable caching |
| `showThresholds` | `boolean` | `true` | Show threshold lines |
| `compareMode` | `boolean` | `true` | Enable comparison mode |
| `lowThreshold` | `number` | `20` | Low stock threshold (%) |
| `criticalThreshold` | `number` | `50` | Critical stock threshold (%) |
| `idealThreshold` | `number` | `80` | Ideal stock threshold (%) |
| `showFill` | `boolean` | `false` | Show fill area under lines |
| `lineTension` | `number` | `0.4` | Line curvature (0-1) |

## Output Events

| Event | Type | Description |
|-------|------|-------------|
| `dataLoaded` | `EventEmitter<StockTrendData[]>` | Emitted when data loads |
| `errorOccurred` | `EventEmitter<Error>` | Emitted on error |
| `pointClicked` | `EventEmitter<StockTrendData>` | Emitted on data point click |
| `medicationsChanged` | `EventEmitter<number[]>` | Emitted when selection changes |

## Chart Configuration

The component uses Chart.js with the following configuration:

- **Type**: Line chart with tension (curved lines)
- **Responsive**: Yes
- **X-Axis**: Time scale with date formatting
- **Y-Axis**: Linear scale (0-100%) with percentage labels
- **Threshold Lines**: Horizontal dashed lines at critical levels
- **Tooltips**: Date, medication name, stock level, percentage change
- **Legend**: Interactive, click to hide/show medications
- **Colors**: Distinct colors for each medication (5-color palette)

## Threshold Lines

Three threshold lines are displayed when enabled:

1. **Low Stock** (default: 20%) - Red dashed line
2. **Critical** (default: 50%) - Amber dashed line
3. **Ideal** (default: 80%) - Green dashed line

Thresholds are customizable via input properties.

## States

### Loading State
Shows a spinner and "Loading stock trend data..." message.

### Error State
Shows error icon, message, and retry button.

### Empty State
Shows empty icon and "No stock trend data available" message.

### Success State
Displays the line chart with selected medications.

## Medication Selection

The component includes an interactive medication selector:

- **Select Medications**: Click medication buttons to add/remove from comparison
- **Select All**: Button to select all available medications (max 5)
- **Clear**: Button to clear all selections
- **Keyboard Navigation**: Use Enter/Space to toggle selections
- **Visual Feedback**: Selected medications are highlighted

## Accessibility

- ARIA labels on all interactive elements
- Screen reader support with descriptive labels
- Keyboard navigation (Enter/Space to select)
- High contrast mode support
- Reduced motion support
- Accessible medication selector

## Dependencies

- `@angular/core` - Angular framework
- `@angular/common` - Common directives
- `@angular/forms` - Forms module for selectors
- `ng2-charts` - Chart.js wrapper
- `chart.js` - Charting library
- `PharmacyService` - Analytics service

## Examples

See `USAGE_EXAMPLE.md` for detailed usage examples.

## API Integration

The component uses `PharmacyService.getStockTrends()` which expects:

**Endpoint**: `GET /api/pharmacy/analytics/stock-trends`

**Query Parameters**:
- `days` (optional): Number of days to look back (default: 30)
- `medicationIds` (optional): Array of medication IDs

**Response Format**:
```json
[
  {
    "date": "2024-01-01",
    "medicationId": 1,
    "medicationName": "Paracetamol 500mg",
    "stockLevel": 75,
    "minimumStockLevel": 100
  },
  ...
]
```

## Performance

- Efficient data grouping by medication
- Optimized chart rendering
- Handles 30+ days of data for 5+ medications
- Caches data for 1 hour (configurable)
- Lazy loads chart library

## Troubleshooting

### Chart not displaying
- Check if medications are selected
- Verify data is being fetched (check browser console)
- Ensure `PharmacyService` is properly injected
- Check Chart.js installation: `npm list chart.js ng2-charts`

### No medications available
- Verify backend endpoint returns medication data
- Check medication IDs are valid
- Ensure data includes medication names

### Threshold lines not showing
- Verify `showThresholds` is set to `true`
- Check threshold values are within 0-100 range
- Ensure chart has data loaded

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Best Practices

1. **Limit medications** - Compare 3-5 medications for best readability
2. **Use thresholds** - Enable threshold lines for quick visual reference
3. **Handle events** - Listen to `pointClicked` for drill-down functionality
4. **Enable caching** - Keep `useCache` as `true` for better performance
5. **Responsive design** - Component is responsive, adjust `height` for mobile
6. **Date ranges** - Use appropriate `days` value (7-90 recommended)

## License

Part of the eBolnica pharmacy management system.
