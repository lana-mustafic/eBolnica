import type { Chart } from 'chart.js';

/** Defer chart init until after layout so Chart.js reads a non-zero container size. */
export function scheduleChartRender(task: () => void | Promise<void>): void {
  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      void task();
    });
  });
}

/** Coordinates deferred renders when inputs change before/after view init or during async Chart.js load. */
export class ChartRenderQueue {
  private viewReady = false;
  private renderQueued = false;
  private pendingRender = false;
  private generation = 0;

  constructor(private readonly render: () => void | Promise<void>) {}

  markViewReady(): void {
    this.viewReady = true;
    this.queueRender();
  }

  queueRender(): void {
    if (!this.viewReady) {
      this.pendingRender = true;
      return;
    }

    if (this.renderQueued) {
      this.pendingRender = true;
      return;
    }

    this.renderQueued = true;
    this.pendingRender = false;

    scheduleChartRender(async () => {
      this.renderQueued = false;
      const generation = ++this.generation;
      await this.render();
      if (generation === this.generation && this.pendingRender) {
        this.queueRender();
      }
    });
  }

  invalidate(): void {
    this.generation++;
  }
}

export function observeChartResize(
  element: HTMLElement | undefined,
  getChart: () => Chart | undefined,
  onDisconnect: () => void
): (() => void) | undefined {
  if (!element || typeof ResizeObserver === 'undefined') {
    return undefined;
  }

  const observer = new ResizeObserver(() => {
    getChart()?.resize();
  });
  observer.observe(element);

  return () => {
    observer.disconnect();
    onDisconnect();
  };
}
