# Chart.js Test Component

This component serves as a verification that Chart.js is properly installed and configured in the Angular application.

## Purpose

- Verify Chart.js installation
- Test ng2-charts integration with Angular 19 standalone components
- Provide a reference implementation for creating charts

## Usage

To use this component in your application:

1. Import the component in your module/component:

```typescript
import { ChartTestComponent } from './shared/components/chart-test/chart-test.component';

@Component({
  imports: [ChartTestComponent],
  // ...
})
```

2. Add to your template:

```html
<app-chart-test></app-chart-test>
```

## Expected Result

If Chart.js is properly configured, you should see a bar chart displaying sample data with:
- 5 bars (January through May)
- Colorful bars with labels
- Legend and title displayed

## Troubleshooting

If the chart doesn't display:
1. Check browser console for errors
2. Verify `chart.js` and `ng2-charts` are installed: `npm list chart.js ng2-charts`
3. Ensure `BaseChartDirective` is imported in component imports
4. Check that Chart.js types are available: `import { ChartType } from 'chart.js'`

## Next Steps

Once verified, you can:
- Use this component as a template for your own charts
- Remove this test component if not needed
- Refer to `CHARTJS_SETUP.md` for detailed documentation
