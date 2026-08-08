import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostBinding,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import type { Chart, TooltipItem } from 'chart.js';
import { MonthlyRevenueItemDto } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { loadChartJs } from './chart-js-loader';
import { ChartRenderQueue, observeChartResize } from './chart-render.util';

@Component({
  selector: 'app-pharmacy-revenue-chart',
  standalone: false,
  templateUrl: './pharmacy-revenue-chart.component.html',
  styleUrl: './pharmacy-revenue-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyRevenueChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas') canvas?: ElementRef<HTMLCanvasElement>;
  @Input() items: MonthlyRevenueItemDto[] = [];

  @HostBinding('style.--chart-host-height')
  readonly chartHostHeight = '320px';

  private chart?: Chart;
  private disconnectResize?: () => void;
  private readonly renderQueue = new ChartRenderQueue(() => this.renderChart());

  ngAfterViewInit(): void {
    this.disconnectResize = observeChartResize(
      this.canvas?.nativeElement?.parentElement ?? undefined,
      () => this.chart,
      () => {
        this.disconnectResize = undefined;
      }
    );
    this.renderQueue.markViewReady();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items']) {
      this.renderQueue.queueRender();
    }
  }

  ngOnDestroy(): void {
    this.renderQueue.invalidate();
    this.disconnectResize?.();
    this.chart?.destroy();
    this.chart = undefined;
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

    const { Chart: ChartCtor } = await loadChartJs();
    if (!this.canvas?.nativeElement) {
      return;
    }

    this.chart = new ChartCtor(this.canvas.nativeElement, {
      type: 'bar',
      data: {
        labels: this.items.map((item) => item.monthShort),
        datasets: [
          {
            label: 'Prihod (KM)',
            data: this.items.map((item) => item.revenue),
            backgroundColor: '#3B82F6',
            borderRadius: { topLeft: 6, topRight: 6, bottomLeft: 0, bottomRight: 0 },
            borderSkipped: false,
            maxBarThickness: 48,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: {
          duration: 800,
          easing: 'easeOutQuart',
        },
        interaction: {
          mode: 'index',
          intersect: false,
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#111827',
            titleColor: '#F9FAFB',
            bodyColor: '#E5E7EB',
            padding: 12,
            cornerRadius: 8,
            callbacks: {
              label: (ctx: TooltipItem<'bar'>) => {
                const item = this.items[ctx.dataIndex];
                const value = ctx.parsed.y ?? 0;
                const lines = [` ${value.toFixed(2)} KM`];
                if (item?.prescriptionCount != null) {
                  lines.push(` ${item.prescriptionCount} recepta`);
                }
                return lines;
              },
            },
          },
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: {
              color: '#6B7280',
              font: { size: 12, weight: 500 },
              maxRotation: 0,
              autoSkip: true,
              maxTicksLimit: 12,
            },
            border: { display: false },
          },
          y: {
            beginAtZero: true,
            grid: { color: 'rgba(148, 163, 184, 0.18)' },
            ticks: {
              color: '#6B7280',
              font: { size: 12 },
              callback: (value) => `${value} KM`,
            },
            border: { display: false },
          },
        },
      },
    });
  }
}
