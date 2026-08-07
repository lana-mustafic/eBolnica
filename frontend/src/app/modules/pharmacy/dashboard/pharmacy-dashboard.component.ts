import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, of, Subject, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import {
  CategoryItemDto,
  MonthlyRevenueItemDto,
  StatisticsSummaryDto,
  StockTrendItemDto,
} from '../../../api-services/pharmacy/pharmacy-api.models';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { PharmacyIconName } from '../shared/pharmacy-icon/pharmacy-icon.component';

interface DashboardActivityItem {
  icon: PharmacyIconName;
  tone: 'success' | 'warning' | 'info';
  text: string;
  time: string;
}

@Component({
  selector: 'app-pharmacy-dashboard',
  standalone: false,
  templateUrl: './pharmacy-dashboard.component.html',
  styleUrl: './pharmacy-dashboard.component.scss',
})
export class PharmacyDashboardComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private destroyRef = inject(DestroyRef);
  private loadTrigger$ = new Subject<void>();

  auth = inject(AuthFacadeService);

  isLoading = true;
  loadError = false;
  summary: StatisticsSummaryDto | null = null;
  revenueItems: MonthlyRevenueItemDto[] = [];
  categories: CategoryItemDto[] = [];
  stockItems: StockTrendItemDto[] = [];
  revenueChange = 0;
  todayShortLabel = this.formatTodayShort();

  ngOnInit(): void {
    this.loadTrigger$
      .pipe(
        switchMap(() => {
          this.isLoading = true;
          this.loadError = false;
          return this.pharmacyApi.getDashboardStats().pipe(
            catchError(() => {
              this.loadError = true;
              this.summary = null;
              return of(null);
            })
          );
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

    this.loadTrigger$.next();
  }

  reload(): void {
    this.loadTrigger$.next();
  }

  get revenueTrendLabel(): string | undefined {
    if (this.revenueChange === 0) {
      return undefined;
    }

    return `${Math.abs(this.revenueChange).toFixed(1)}% u odnosu na prethodni mjesec`;
  }

  get inventoryValueLabel(): string {
    if (!this.summary) {
      return '0 KM';
    }

    return `${this.summary.inventoryValue.toFixed(2)} KM`;
  }

  get inventoryKpiSubtitle(): string {
    if (!this.summary) {
      return 'Trenutna procjena';
    }

    if (this.revenueTrendLabel) {
      return `Prihod: ${this.summary.totalRevenue.toFixed(2)} KM`;
    }

    return 'Trenutna procjena';
  }

  get recentActivities(): DashboardActivityItem[] {
    if (!this.summary) {
      return [];
    }

    const items: DashboardActivityItem[] = [
      {
        icon: 'check-circle',
        tone: 'success',
        text: `${this.summary.totalMedications} aktivnih lijekova u sistemu`,
        time: 'Upravo sada',
      },
    ];

    if (this.summary.pendingPrescriptions > 0) {
      items.push({
        icon: 'clipboard',
        tone: 'info',
        text: `${this.summary.pendingPrescriptions} recept(a) čeka izdavanje`,
        time: 'Danas',
      });
    }

    if (this.summary.lowStockAlerts > 0) {
      items.push({
        icon: 'triangle-alert',
        tone: 'warning',
        text: `${this.summary.lowStockAlerts} lijek(ova) ispod minimuma zaliha`,
        time: 'Danas',
      });
    }

    if (this.summary.expiringSoon > 0 || this.summary.expiredMedications > 0) {
      items.push({
        icon: 'calendar',
        tone: 'warning',
        text: `Rok trajanja: ${this.summary.expiringSoon} uskoro / ${this.summary.expiredMedications} isteklo`,
        time: 'Inventar',
      });
    }

    items.push({
      icon: 'activity',
      tone: 'success',
      text: `Vrijednost inventara ${this.summary.inventoryValue.toFixed(2)} KM`,
      time: 'Ažurirano',
    });

    return items.slice(0, 5);
  }

  private formatTodayShort(): string {
    return new Date().toLocaleDateString('bs-BA', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  }
}
