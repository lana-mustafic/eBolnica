type ChartJsModule = typeof import('chart.js/auto');

const MAX_IMPORT_ATTEMPTS = 3;
const IMPORT_RETRY_DELAY_MS = 400;

let chartModulePromise: Promise<ChartJsModule> | null = null;

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function importChartJsModule(attempt = 1): Promise<ChartJsModule> {
  try {
    return await import('chart.js/auto');
  } catch (error) {
    if (attempt >= MAX_IMPORT_ATTEMPTS) {
      throw error;
    }

    await delay(IMPORT_RETRY_DELAY_MS * attempt);
    return importChartJsModule(attempt + 1);
  }
}

export function loadChartJs(): Promise<ChartJsModule> {
  if (!chartModulePromise) {
    chartModulePromise = importChartJsModule().catch((error) => {
      chartModulePromise = null;
      throw error;
    });
  }

  return chartModulePromise;
}
