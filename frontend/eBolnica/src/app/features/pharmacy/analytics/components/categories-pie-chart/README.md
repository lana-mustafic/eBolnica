# Categories Pie Chart Component

A reusable Angular component for displaying medication category distribution in a pie or doughnut chart format. Integrates with the Pharmacy Analytics Service to fetch and visualize category data.

## Features

✅ **Chart Type Toggle** - Switch between pie and doughnut charts  
✅ **Real-time Data** - Fetches data from analytics service  
✅ **Interactive Legend** - Click legend items to hide/show categories  
✅ **Percentage Labels** - Shows percentages in legend and tooltips  
✅ **Color Palette** - Pharmacy design system colors  
✅ **Loading States** - Shows spinner while loading  
✅ **Error Handling** - Displays error messages with retry option  
✅ **Empty States** - Handles no data and insufficient data scenarios  
✅ **Responsive Design** - Adapts to mobile, tablet, and desktop  
✅ **Accessibility** - ARIA labels, keyboard navigation, screen reader support  
✅ **Clickable Segments** - Click events for drill-down functionality  
✅ **Center Label** - Shows total categories in doughnut center  

## Installation

The component is already created and ready to use. No additional installation required.

## Quick Start

### Basic Usage

```typescript
import { CategoriesPieChartComponent } from './features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';

@Component({
  imports: [CategoriesPieChartComponent],
  template: `
    <app-categories-pie-chart></app-categories-pie-chart>
  `
})
```

### With Customization

```html
<app-categories-pie-chart 
  [title]="'Medication Categories Distribution'"
  [chartType]="'doughnut'"
  [maxCategories]="8"
  [height]="400">
</app-categories-pie-chart>
```

## Component Structure

```
categories-pie-chart/
├── categories-pie-chart.component.ts    # Component logic
├── categories-pie-chart.component.html   # Template
├── categories-pie-chart.component.scss   # Styles
├── README.md                            # This file
└── USAGE_EXAMPLE.md                     # Usage examples
```

## Input Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `title` | `string` | `'Medication Categories Distribution'` | Chart title |
| `height` | `number` | `400` | Chart height in pixels |
| `chartType` | `'pie' \| 'doughnut'` | `'doughnut'` | Chart type |
| `maxCategories` | `number` | `10` | Maximum categories to display |
| `useCache` | `boolean` | `true` | Enable caching |
| `showLabels` | `boolean` | `true` | Show percentage labels |
| `showCenterLabel` | `boolean` | `true` | Show center label (doughnut only) |

## Output Events

| Event | Type | Description |
|-------|------|-------------|
| `dataLoaded` | `EventEmitter<MedicationCategoryData[]>` | Emitted when data loads |
| `errorOccurred` | `EventEmitter<Error>` | Emitted on error |
| `segmentClicked` | `EventEmitter<MedicationCategoryData>` | Emitted on segment click |

## Chart Configuration

The component uses Chart.js with the following configuration:

- **Type**: Pie or Doughnut chart
- **Responsive**: Yes
- **Cutout**: 60% for doughnut (configurable)
- **Legend**: Right position, interactive, shows percentages
- **Tooltips**: Category name, count, and percentage
- **Colors**: Pharmacy design system palette (12 colors)
- **Hover Effects**: Segment highlighting

## States

### Loading State
Shows a spinner and "Loading category data..." message.

### Error State
Shows error icon, message, and retry button.

### Insufficient Data State
Shows warning when less than 2 categories available.

### Empty State
Shows empty icon and "No category data available" message.

### Success State
Displays the pie/doughnut chart with data.

## Color Palette

The component uses a 12-color palette from the pharmacy design system:

1. #3b82f6 - Pharmacy blue
2. #10b981 - Green
3. #f59e0b - Amber
4. #ef4444 - Red
5. #8b5cf6 - Purple
6. #ec4899 - Pink
7. #06b6d4 - Cyan
8. #84cc16 - Lime
9. #f97316 - Orange
10. #6366f1 - Indigo
11. #14b8a6 - Teal
12. #a855f7 - Violet

Colors repeat if more than 12 categories are displayed.

## Accessibility

- ARIA labels on all interactive elements
- Screen reader support with descriptive labels
- Keyboard navigation (Enter/Space to select)
- Accessible category list below chart
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

## API Integration

The component uses `PharmacyService.getTopMedicationCategories()` which expects:

**Endpoint**: `GET /api/pharmacy/analytics/top-categories`

**Query Parameters**:
- `limit` (optional): Maximum number of categories (default: 10)

**Response Format**:
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

## Troubleshooting

### Chart not displaying
- Check if data is being fetched (check browser console)
- Verify at least 2 categories are available
- Ensure `PharmacyService` is properly injected
- Check Chart.js installation: `npm list chart.js ng2-charts`

### Legend not interactive
- Ensure Chart.js version supports legend onClick
- Check browser console for errors
- Verify chart options are properly configured

### Colors not displaying correctly
- Check color palette array in component
- Verify category data structure
- Ensure colors array matches data length

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Performance

- Caches data for 1 hour (configurable)
- Optimized re-renders
- Lazy loads chart library
- Efficient color generation

## Best Practices

1. **Use doughnut for better readability** - Center label shows summary
2. **Limit categories** - Use `maxCategories` to keep chart readable (8-10 recommended)
3. **Handle events** - Listen to `segmentClicked` for drill-down functionality
4. **Enable caching** - Keep `useCache` as `true` for better performance
5. **Accessibility** - Use accessible category list for screen readers

## License

Part of the eBolnica pharmacy management system.
