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
import { StockTrendItemDto } from '../../../../api-services/pharmacy/pharmacy-api.models';

@Component({
  selector: 'app-pharmacy-stock-chart',
  standalone: false,
  templateUrl: './pharmacy-stock-chart.component.html',
  styleUrl: './pharmacy-stock-chart.component.scss',
})
export class PharmacyStockChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('canvas') canvas?: ElementRef<HTMLCanvasElement>;
  @Input() items: StockTrendItemDto[] = [];

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
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        animation: {
          duration: 700,
          easing: 'easeOutQuart',
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#111827',
            padding: 12,
            cornerRadius: 8,
            callbacks: {
              label: (ctx: TooltipItem<'bar'>) => ` ${ctx.parsed.x} kom`,
            },
          },
        },
        scales: {
          x: {
            beginAtZero: true,
            grid: { color: 'rgba(148, 163, 184, 0.18)' },
            ticks: { color: '#6B7280' },
            border: { display: false },
          },
          y: {
            grid: { display: false },
            ticks: { color: '#374151', font: { size: 12, weight: 500 } },
            border: { display: false },
          },
        },
      },
    });
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
