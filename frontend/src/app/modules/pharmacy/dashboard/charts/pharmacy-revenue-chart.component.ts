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
import { MonthlyRevenueItemDto } from '../../../../api-services/pharmacy/pharmacy-api.models';

@Component({
  selector: 'app-pharmacy-revenue-chart',
  standalone: false,
  templateUrl: './pharmacy-revenue-chart.component.html',
  styleUrl: './pharmacy-revenue-chart.component.scss',
})
export class PharmacyRevenueChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas') canvas?: ElementRef<HTMLCanvasElement>;
  @Input() items: MonthlyRevenueItemDto[] = [];

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
      type: 'bar',
      data: {
        labels: this.items.map((item) => item.monthShort),
        datasets: [
          {
            label: 'Prihod (KM)',
            data: this.items.map((item) => item.revenue),
            backgroundColor: '#3b82f6',
            borderRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              callback: (value) => `${value} KM`,
            },
          },
        },
      },
    });
  }
}
