/** Defer chart init until after layout so Chart.js reads a non-zero container size. */
export function scheduleChartRender(task: () => void | Promise<void>): void {
  requestAnimationFrame(() => {
    void task();
  });
}
