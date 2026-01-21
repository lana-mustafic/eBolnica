# Revenue Bar Chart Component

A reusable Angular component for displaying monthly revenue data in a bar chart format. Integrates with the Pharmacy Analytics Service to fetch and visualize revenue trends.

## Features

✅ **Real-time Data** - Fetches data from analytics service  
✅ **Loading States** - Shows spinner while loading  
✅ **Error Handling** - Displays error messages with retry option  
✅ **Empty States** - Handles no data scenarios gracefully  
✅ **Responsive Design** - Adapts to mobile, tablet, and desktop  
✅ **Accessibility** - ARIA labels and keyboard navigation  
✅ **Customizable** - Title, height, colors, and period selection  
✅ **Currency Formatting** - Automatic $ formatting on y-axis and tooltips  
✅ **Interactive** - Click events on bars for drill-down  
✅ **Caching** - Uses service cache for better performance  

## Installation

The component is already created and ready to use. No additional installation required.

## Quick Start

### Basic Usage

```typescript
import { RevenueBarChartComponent } from './features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';

@Component({
  imports: [RevenueBarChartComponent],
  template: `
    <app-revenue-bar-chart></app-revenue-bar-chart>
  `
})
```

### With Customization

```html
<app-revenue-bar-chart 
  [title]="'Monthly Revenue Overview'"
  [period]="'last12months'"
  [height]="400"
  [barColor]="'#3b82f6'">
</app-revenue-bar-chart>
```

## Component Structure

```
revenue-bar-chart/
├── revenue-bar-chart.component.ts    # Component logic
├── revenue-bar-chart.component.html  # Template
├── revenue-bar-chart.component.scss  # Styles
├── README.md                          # This file
├── USAGE_EXAMPLE.md                  # Usage examples
└── INTEGRATION_EXAMPLE.ts            # Integration guide
```

## Input Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `title` | `string` | `'Monthly Revenue'` | Chart title |
| `height` | `number` | `400` | Chart height in pixels |
| `period` | `AnalyticsPeriod` | `'last12months'` | Data period |
| `dateRange` | `{startDate: Date, endDate: Date}` | `undefined` | Custom date range |
| `useCache` | `boolean` | `true` | Enable caching |
| `barColor` | `string` | `'#3b82f6'` | Bar color (pharmacy blue) |
| `barHoverColor` | `string` | `'#2563eb'` | Bar hover color |

## Output Events

| Event | Type | Description |
|-------|------|-------------|
| `dataLoaded` | `EventEmitter<MonthlyRevenueData[]>` | Emitted when data loads |
| `errorOccurred` | `EventEmitter<Error>` | Emitted on error |
| `barClicked` | `EventEmitter<{month: string, revenue: number}>` | Emitted on bar click |

## Chart Configuration

The component uses Chart.js with the following configuration:

- **Type**: Bar chart
- **Responsive**: Yes
- **Aspect Ratio**: Maintained (configurable)
- **Y-Axis**: Currency formatted ($)
- **X-Axis**: Month labels
- **Tooltips**: Currency formatted with month and value
- **Colors**: Pharmacy brand colors (#3b82f6)

## States

### Loading State
Shows a spinner and "Loading revenue data..." message.

### Error State
Shows error icon, message, and retry button.

### Empty State
Shows empty icon and "No revenue data available" message.

### Success State
Displays the bar chart with data.

## Styling

The component follows the pharmacy design system:
- White background with subtle shadow
- Pharmacy blue (#3b82f6) for bars
- Responsive breakpoints for mobile/tablet
- Accessible color contrasts
- Smooth transitions and animations

## Accessibility

- ARIA labels on all interactive elements
- Screen reader support
- Keyboard navigation
- High contrast mode support
- Reduced motion support

## Dependencies

- `@angular/core` - Angular framework
- `@angular/common` - Common directives
- `ng2-charts` - Chart.js wrapper
- `chart.js` - Charting library
- `PharmacyService` - Analytics service

## Examples

See `USAGE_EXAMPLE.md` for detailed usage examples.

See `INTEGRATION_EXAMPLE.ts` for integration guide.

## API Integration

The component uses `PharmacyService.getMonthlyRevenue()` which expects:

**Endpoint**: `GET /api/pharmacy/analytics/monthly-revenue`

**Query Parameters**:
- `period` (optional): Predefined period
- `startDate` (optional): Custom start date
- `endDate` (optional): Custom end date

**Response Format**:
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

## Troubleshooting

### Chart not displaying
- Check if data is being fetched (check browser console)
- Verify `PharmacyService` is properly injected
- Ensure Chart.js is installed: `npm list chart.js ng2-charts`

### Data not loading
- Check network tab for API calls
- Verify backend endpoint is available
- Check service cache: `pharmacyService.clearAnalyticsCache()`

### Styling issues
- Ensure SCSS is compiled correctly
- Check for CSS conflicts with parent components
- Verify responsive breakpoints

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Performance

- Uses OnPush change detection (removed for chart compatibility)
- Caches data for 1 hour (configurable)
- Lazy loads chart library
- Optimized re-renders

## License

Part of the eBolnica pharmacy management system.
