import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostBinding,
  inject,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import type { Chart, TooltipItem } from 'chart.js';
import { StockTrendItemDto } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { loadChartJs } from './chart-js-loader';
import {
  ChartRenderQueue,
  chartHostHasLayout,
  PharmacyChartHostBindings,
  scheduleChartRender,
  setupPharmacyChartHost,
} from './chart-render.util';

interface StockChartMedication {
  id: number;
  name: string;
  color: string;
}

const FALLBACK_COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6'];

@Component({
  selector: 'app-pharmacy-stock-chart',
  standalone: false,
  templateUrl: './pharmacy-stock-chart.component.html',
  styleUrl: './pharmacy-stock-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyStockChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas') canvas?: ElementRef<HTMLCanvasElement>;
  @Input() items: StockTrendItemDto[] = [];
  @Input() timeline: string[] = [];
  @Input() medications: StockChartMedication[] = [];
  @Input() metricType = 'current-stock-snapshot';
  @Input() emptyMessage = 'Nema podataka za prikaz grafikona.';

  @HostBinding('style.--chart-host-height')
  get chartHostHeight(): string {
    if (this.metricType === 'stock-history-trend') {
      return '300px';
    }

    const count = Math.max(this.items.length, 1);
    const height = Math.min(520, Math.max(280, count * 36 + 56));
    return `${height}px`;
  }

  get hasData(): boolean {
    return this.items.length > 0;
  }

  private readonly host = inject(ElementRef<HTMLElement>);
  private chart?: Chart;
  private hostBindings?: PharmacyChartHostBindings;
  private readonly renderQueue = new ChartRenderQueue(() => this.renderChart());

  ngAfterViewInit(): void {
    this.hostBindings = setupPharmacyChartHost(
      this.host.nativeElement,
      this.canvas?.nativeElement?.parentElement ?? undefined,
      () => this.chart,
      this.renderQueue
    );
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items'] || changes['timeline'] || changes['medications'] || changes['metricType']) {
      this.renderQueue.queueRender();
    }
  }

  ngOnDestroy(): void {
    this.renderQueue.invalidate();
    this.hostBindings?.disconnect();
    this.chart?.destroy();
    this.chart = undefined;
  }

  private async renderChart(): Promise<void> {
    const canvas = this.canvas?.nativeElement;
    const chartHost = canvas?.parentElement;
    if (!canvas) {
      return;
    }

    if (!chartHostHasLayout(chartHost)) {
      scheduleChartRender(() => this.renderQueue.queueRender());
      return;
    }

    this.chart?.destroy();
    this.chart = undefined;

    if (this.items.length === 0) {
      return;
    }

    const { Chart: ChartCtor } = await loadChartJs();
    if (!this.canvas?.nativeElement) {
      return;
    }

    if (this.metricType === 'stock-history-trend' && this.timeline.length > 0) {
      this.renderTrendChart(ChartCtor);
      return;
    }

    this.renderSnapshotChart(ChartCtor);
  }

  private renderTrendChart(
    ChartCtor: Awaited<ReturnType<typeof loadChartJs>>['Chart']
  ): void {
    const labels = this.timeline.map((day) =>
      new Date(`${day}T00:00:00`).toLocaleDateString('bs-BA', { day: '2-digit', month: '2-digit' })
    );

    const datasets = this.medications.map((medication, index) => {
      const color = medication.color || FALLBACK_COLORS[index % FALLBACK_COLORS.length];
      return {
        label: medication.name,
        data: this.timeline.map((day) => {
          const point = this.items.find(
            (item) => item.medicationId === medication.id && item.date.startsWith(day)
          );
          return point?.quantity ?? null;
        }),
        borderColor: color,
        backgroundColor: color,
        pointRadius: 3,
        pointHoverRadius: 5,
        borderWidth: 2,
        tension: 0.3,
        spanGaps: true,
      };
    });

    this.chart = new ChartCtor(this.canvas!.nativeElement, {
      type: 'line',
      data: { labels, datasets },
      options: this.buildSharedOptions('line'),
    });
  }

  private renderSnapshotChart(
    ChartCtor: Awaited<ReturnType<typeof loadChartJs>>['Chart']
  ): void {
    this.chart = new ChartCtor(this.canvas!.nativeElement, {
      type: 'bar',
      data: {
        labels: this.items.map((item) => item.medicationName),
        datasets: [
          {
            label: 'Količina',
            data: this.items.map((item) => item.quantity),
            backgroundColor: this.items.map((item) => this.colorForStatus(item.status)),
            borderRadius: 8,
            borderSkipped: false,
          },
        ],
      },
      options: this.buildSharedOptions('bar'),
    });
  }

  private buildSharedOptions(type: 'line' | 'bar') {
    return {
      indexAxis: type === 'bar' ? ('y' as const) : undefined,
      responsive: true,
      maintainAspectRatio: false,
      animation: {
        duration: 700,
        easing: 'easeOutQuart' as const,
      },
      interaction: {
        mode: 'index' as const,
        intersect: false,
      },
      plugins: {
        legend: {
          display: type === 'line',
          position: 'bottom' as const,
          labels: {
            color: '#6B7280',
            boxWidth: 12,
            boxHeight: 12,
            padding: 12,
            font: { size: 12, weight: 500 as const },
            usePointStyle: true,
          },
        },
        tooltip: {
          backgroundColor: '#111827',
          padding: 12,
          cornerRadius: 8,
          callbacks: {
            label: (ctx: TooltipItem<'line' | 'bar'>) => {
              const value = type === 'bar' ? ctx.parsed.x : ctx.parsed.y;
              return ` ${value ?? 0} kom`;
            },
          },
        },
      },
      scales: {
        x: {
          beginAtZero: type === 'bar',
          grid: { color: type === 'line' ? 'rgba(148, 163, 184, 0.18)' : undefined, display: type === 'line' },
          ticks: { color: '#6B7280' },
          border: { display: false },
        },
        y: {
          beginAtZero: true,
          grid: {
            color: 'rgba(148, 163, 184, 0.18)',
            display: type === 'line' || type === 'bar',
          },
          ticks: {
            color: '#374151',
            font: { size: 12, weight: 500 },
            autoSkip: type === 'bar',
          },
          border: { display: false },
        },
      },
    };
  }

  private colorForStatus(status: string): string {
    const normalized = status?.toLowerCase() ?? '';
    if (normalized.includes('critical') || normalized.includes('out')) {
      return '#EF4444';
    }
    if (normalized.includes('low') || normalized.includes('warn')) {
      return '#F59E0B';
    }
    return '#22C55E';
  }
}
