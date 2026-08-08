import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import {
  CategoryItemDto,
  MonthlyRevenueItemDto,
  StockTrendItemDto,
} from '../../../../api-services/pharmacy/pharmacy-api.models';

@Component({
  selector: 'app-pharmacy-analytics-charts',
  standalone: false,
  templateUrl: './pharmacy-analytics-charts.component.html',
  styleUrl: './pharmacy-analytics-charts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyAnalyticsChartsComponent {
  @Input() revenueItems: MonthlyRevenueItemDto[] = [];
  @Input() totalRevenue = 0;
  @Input() averageMonthlyRevenue = 0;
  @Input() revenueChange = 0;

  @Input() categories: CategoryItemDto[] = [];
  @Input() categoriesTotal = 0;
  @Input() categoriesMedicationTotal = 0;

  @Input() stockItems: StockTrendItemDto[] = [];
  @Input() stockTimeline: string[] = [];
  @Input() stockMedications: {
    id: number;
    name: string;
    color: string;
    currentStock: number;
    trendDirection?: number;
  }[] = [];
  @Input() stockMetricType = 'current-stock-snapshot';
  @Input() stockNote?: string;

  get stockChartTitle(): string {
    return this.stockMetricType === 'stock-history-trend' ? 'Trend zaliha' : 'Stanje zaliha';
  }

  get stockChartSubtitle(): string {
    if (this.stockNote) {
      return this.stockNote;
    }

    return this.stockMetricType === 'stock-history-trend'
      ? 'Promjene zaliha u zadnjih 30 dana'
      : 'Trenutni nivoi zaliha po lijekovima';
  }

  get revenueTrendLabel(): string | undefined {
    if (this.revenueChange === 0) {
      return undefined;
    }

    return `${Math.abs(this.revenueChange).toFixed(1)}% u odnosu na prethodni mjesec`;
  }

  get revenueChartSubtitle(): string {
    if (this.totalRevenue <= 0) {
      return 'Pregled mjesečnog prihoda apoteke';
    }

    return `Ukupno ${this.totalRevenue.toFixed(2)} KM · prosjek ${this.averageMonthlyRevenue.toFixed(2)} KM/mj.`;
  }

  get categoriesChartSubtitle(): string {
    if (this.categoriesTotal <= 0) {
      return 'Raspodjela inventara po kategorijama';
    }

    return `${this.categoriesTotal} kategorija · ${this.categoriesMedicationTotal} lijekova u prikazu`;
  }
}
