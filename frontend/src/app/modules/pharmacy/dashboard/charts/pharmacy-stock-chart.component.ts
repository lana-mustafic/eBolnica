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
            backgroundColor: '#10b981',
            borderRadius: 4,
          },
        ],
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          subtitle: {
            display: true,
            text: 'Trenutni snapshot zaliha (nije historijski trend)',
          },
        },
        scales: {
          x: {
            beginAtZero: true,
          },
        },
      },
    });
  }
}
