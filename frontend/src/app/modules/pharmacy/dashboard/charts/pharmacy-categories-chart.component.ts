import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import { Chart } from 'chart.js/auto';
import { CategoryItemDto } from '../../../../api-services/pharmacy/pharmacy-api.models';

const CHART_COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16'];

@Component({
  selector: 'app-pharmacy-categories-chart',
  standalone: false,
  templateUrl: './pharmacy-categories-chart.component.html',
  styleUrl: './pharmacy-categories-chart.component.scss',
})
export class PharmacyCategoriesChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas') canvas?: ElementRef<HTMLCanvasElement>;
  @Input() items: CategoryItemDto[] = [];

  private chart?: Chart;

  ngAfterViewInit(): void {
    this.renderChart();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items'] && !changes['items'].firstChange) {
      this.renderChart();
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private renderChart(): void {
    if (!this.canvas) {
      return;
    }

    this.chart?.destroy();

    this.chart = new Chart(this.canvas.nativeElement, {
      type: 'doughnut',
      data: {
        labels: this.items.map((item) => item.category),
        datasets: [
          {
            data: this.items.map((item) => item.medicationCount),
            backgroundColor: this.items.map((_, index) => CHART_COLORS[index % CHART_COLORS.length]),
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'bottom',
          },
        },
      },
    });
  }
}
