import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
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
  private destroyRef = inject(DestroyRef);

  isLoading = true;
  loadError = false;
  summary: StatisticsSummaryDto | null = null;
  revenueItems: MonthlyRevenueItemDto[] = [];
  categories: CategoryItemDto[] = [];
  stockItems: StockTrendItemDto[] = [];
  revenueChange = 0;

  ngOnInit(): void {
    this.loadDashboard();
  }

  reload(): void {
    this.loadDashboard();
  }

  private loadDashboard(): void {
    this.isLoading = true;
    this.loadError = false;
    this.pharmacyApi
      .getDashboardStats()
      .pipe(
        catchError(() => {
          this.loadError = true;
          this.summary = null;
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((stats) => {
        this.isLoading = false;
        if (!stats) return;

        this.summary = stats.metadata.summary;
        this.revenueItems = stats.monthlyRevenue.data.slice(-6);
        this.categories = stats.topCategories.data;
        this.stockItems = stats.stockTrends.data;
        this.revenueChange = stats.monthlyRevenue.revenueChangePercentage;
      });
  }
}
