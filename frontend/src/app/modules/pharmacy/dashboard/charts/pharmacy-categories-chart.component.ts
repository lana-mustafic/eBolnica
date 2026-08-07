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
import { Chart, TooltipItem } from 'chart.js/auto';
import { getMedicationCategoryLabel } from '../../constants/medication-categories.constant';
import { CategoryItemDto } from '../../../../api-services/pharmacy/pharmacy-api.models';

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

  private chart?: Chart;
  private renderQueued = false;

  ngAfterViewInit(): void {
    this.queueRender();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items']) {
      this.queueRender();
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private queueRender(): void {
    if (this.renderQueued) {
      return;
    }

    this.renderQueued = true;
    queueMicrotask(() => {
      this.renderQueued = false;
      this.renderChart();
    });
  }

  private renderChart(): void {
    if (!this.canvas?.nativeElement) {
      return;
    }

    this.chart?.destroy();
    this.chart = undefined;

    if (this.items.length === 0) {
      return;
    }

    this.chart = new Chart(this.canvas.nativeElement, {
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
              padding: 16,
              font: { size: 12, weight: 500 },
            },
          },
          tooltip: {
            backgroundColor: '#111827',
            padding: 12,
            cornerRadius: 8,
            callbacks: {
              label: (ctx: TooltipItem<'doughnut'>) => ` ${ctx.label}: ${ctx.parsed} lijekova`,
            },
          },
        },
      },
    });
  }
}
