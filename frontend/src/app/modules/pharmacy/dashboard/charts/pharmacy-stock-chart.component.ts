import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import type { Chart, TooltipItem } from 'chart.js';
import { StockTrendItemDto } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { loadChartJs } from './chart-js-loader';
import { scheduleChartRender } from './chart-render.util';

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

  private chart?: Chart;
  private viewReady = false;
  private renderQueued = false;
  private renderGeneration = 0;

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.queueRender();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items'] || changes['timeline'] || changes['medications'] || changes['metricType']) {
      this.queueRender();
    }
  }

  ngOnDestroy(): void {
    this.renderGeneration++;
    this.chart?.destroy();
  }

  private queueRender(): void {
    if (!this.viewReady || this.renderQueued) {
      return;
    }

    this.renderQueued = true;
    scheduleChartRender(() => {
      this.renderQueued = false;
      void this.renderChart();
    });
  }

  private async renderChart(): Promise<void> {
    if (!this.canvas?.nativeElement) {
      return;
    }

    this.chart?.destroy();
    this.chart = undefined;

    if (this.items.length === 0) {
      return;
    }

    const generation = ++this.renderGeneration;
    const { Chart: ChartCtor } = await loadChartJs();
    if (generation !== this.renderGeneration || !this.canvas?.nativeElement) {
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
            padding: 16,
            font: { size: 12, weight: 500 as const },
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
          ticks: { color: '#374151', font: { size: 12, weight: 500 } },
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
