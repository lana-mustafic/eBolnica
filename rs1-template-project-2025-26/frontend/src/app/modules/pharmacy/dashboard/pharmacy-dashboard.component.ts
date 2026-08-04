import { Component, inject, OnInit } from '@angular/core';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import {
  CategoryItemDto,
  MonthlyRevenueItemDto,
  StatisticsSummaryDto,
  StockTrendItemDto,
} from '../../../api-services/pharmacy/pharmacy-api.models';

@Component({
  selector: 'app-pharmacy-dashboard',
  standalone: false,
  templateUrl: './pharmacy-dashboard.component.html',
  styleUrl: './pharmacy-dashboard.component.scss',
})
export class PharmacyDashboardComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);

  isLoading = true;
  summary: StatisticsSummaryDto | null = null;
  revenueItems: MonthlyRevenueItemDto[] = [];
  categories: CategoryItemDto[] = [];
  stockItems: StockTrendItemDto[] = [];
  revenueChange = 0;

  ngOnInit(): void {
    this.pharmacyApi.getDashboardStats().subscribe({
      next: (stats) => {
        this.summary = stats.metadata.summary;
        this.revenueItems = stats.monthlyRevenue.data.slice(-6);
        this.categories = stats.topCategories.data;
        this.stockItems = stats.stockTrends.data;
        this.revenueChange = stats.monthlyRevenue.revenueChangePercentage;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  maxRevenue(): number {
    return Math.max(...this.revenueItems.map((r) => r.revenue), 1);
  }
}
