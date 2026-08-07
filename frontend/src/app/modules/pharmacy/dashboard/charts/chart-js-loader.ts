type ChartJsModule = typeof import('chart.js/auto');

let chartModulePromise: Promise<ChartJsModule> | null = null;

export function loadChartJs(): Promise<ChartJsModule> {
  if (!chartModulePromise) {
    chartModulePromise = import('chart.js/auto');
  }
  return chartModulePromise;
}
