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

/** Re-render charts that mount below the fold once their card enters the viewport. */
export function observeChartVisibility(
  element: HTMLElement | undefined,
  onVisible: () => void
): (() => void) | undefined {
  if (!element) {
    onVisible();
    return undefined;
  }

  if (typeof IntersectionObserver === 'undefined') {
    onVisible();
    return undefined;
  }

  const observer = new IntersectionObserver(
    (entries) => {
      if (entries.some((entry) => entry.isIntersecting)) {
        onVisible();
      }
    },
    { rootMargin: '48px', threshold: 0.01 }
  );
  observer.observe(element);

  return () => observer.disconnect();
}

export interface PharmacyChartHostBindings {
  disconnect(): void;
}

export function setupPharmacyChartHost(
  hostElement: HTMLElement | undefined,
  chartHostElement: HTMLElement | null | undefined,
  getChart: () => Chart | undefined,
  renderQueue: ChartRenderQueue
): PharmacyChartHostBindings {
  const cleanups: Array<() => void> = [];

  const disconnectResize = observeChartResize(chartHostElement ?? undefined, getChart, () => undefined);
  if (disconnectResize) {
    cleanups.push(disconnectResize);
  }

  const triggerRender = (): void => {
    renderQueue.markViewReady();
    renderQueue.queueRender();
  };

  const disconnectVisibility = observeChartVisibility(hostElement, triggerRender);
  if (disconnectVisibility) {
    cleanups.push(disconnectVisibility);
  }

  scheduleChartRender(triggerRender);

  return {
    disconnect() {
      cleanups.forEach((cleanup) => cleanup());
    },
  };
}

export function chartHostHasLayout(chartHostElement: HTMLElement | null | undefined): boolean {
  return (chartHostElement?.clientHeight ?? 0) > 0 && (chartHostElement?.clientWidth ?? 0) > 0;
}
