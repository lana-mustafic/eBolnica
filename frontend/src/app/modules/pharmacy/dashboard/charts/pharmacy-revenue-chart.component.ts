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
import { Chart, TooltipItem } from 'chart.js/auto';
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

    const context = this.canvas.nativeElement.getContext('2d');
    if (!context) {
      return;
    }

    const gradient = context.createLinearGradient(0, 0, 0, 280);
    gradient.addColorStop(0, 'rgba(124, 58, 237, 0.28)');
    gradient.addColorStop(1, 'rgba(124, 58, 237, 0)');

    this.chart = new Chart(this.canvas.nativeElement, {
      type: 'line',
      data: {
        labels: this.items.map((item) => item.monthShort),
        datasets: [
          {
            label: 'Prihod (KM)',
            data: this.items.map((item) => item.revenue),
            borderColor: '#7C3AED',
            backgroundColor: gradient,
            fill: true,
            tension: 0.4,
            pointRadius: 4,
            pointHoverRadius: 6,
            pointBackgroundColor: '#7C3AED',
            pointBorderColor: '#ffffff',
            pointBorderWidth: 2,
            borderWidth: 3,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        animation: {
          duration: 700,
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
              label: (ctx: TooltipItem<'line'>) => {
                const value = ctx.parsed.y ?? 0;
                return ` ${value.toFixed(2)} KM`;
              },
            },
          },
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { color: '#6B7280', font: { size: 12, weight: 500 } },
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
