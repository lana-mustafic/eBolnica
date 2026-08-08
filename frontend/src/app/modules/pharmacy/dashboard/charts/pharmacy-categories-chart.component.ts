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
import { getMedicationCategoryLabel } from '../../constants/medication-categories.constant';
import { CategoryItemDto } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { loadChartJs } from './chart-js-loader';
import { ChartRenderQueue, observeChartResize } from './chart-render.util';

const CHART_COLORS = ['#7C3AED', '#22C55E', '#3B82F6', '#F59E0B', '#EF4444', '#06B6D4', '#EC4899', '#84CC16'];

@Component({
  selector: 'app-pharmacy-categories-chart',
  standalone: false,
  templateUrl: './pharmacy-categories-chart.component.html',
  styleUrl: './pharmacy-categories-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyCategoriesChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas') canvas?: ElementRef<HTMLCanvasElement>;
  @Input() items: CategoryItemDto[] = [];

  @HostBinding('style.--chart-host-height')
  readonly chartHostHeight = '300px';

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
      type: 'doughnut',
      data: {
        labels: this.items.map((item) => getMedicationCategoryLabel(item.category)),
        datasets: [
          {
            data: this.items.map((item) => item.medicationCount),
            backgroundColor: this.items.map((_, index) => CHART_COLORS[index % CHART_COLORS.length]),
            borderWidth: 0,
            hoverOffset: 8,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: {
          animateRotate: true,
          animateScale: true,
          duration: 700,
        },
        cutout: '68%',
        plugins: {
          legend: {
            position: 'bottom',
            labels: {
              color: '#6B7280',
              boxWidth: 12,
              boxHeight: 12,
              padding: 12,
              font: { size: 12, weight: 500 },
              usePointStyle: true,
            },
          },
          tooltip: {
            backgroundColor: '#111827',
            padding: 12,
            cornerRadius: 8,
            callbacks: {
              label: (ctx: TooltipItem<'doughnut'>) => {
                const item = this.items[ctx.dataIndex];
                const count = ctx.parsed ?? 0;
                const lines = [` ${ctx.label}: ${count} lijekova`];
                if (item?.percentage != null) {
                  lines.push(` ${item.percentage.toFixed(1)}% udjela`);
                }
                if (item?.totalValue != null) {
                  lines.push(` ${item.totalValue.toFixed(2)} KM vrijednosti`);
                }
                return lines;
              },
            },
          },
        },
      },
    });
  }
}
