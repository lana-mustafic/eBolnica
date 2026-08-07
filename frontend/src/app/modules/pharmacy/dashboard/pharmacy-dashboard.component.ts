import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, of, Subject, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import {
  CategoryItemDto,
  MonthlyRevenueItemDto,
  StatisticsSummaryDto,
  StockTrendItemDto,
} from '../../../api-services/pharmacy/pharmacy-api.models';
import { PharmacyIconName } from '../shared/pharmacy-icon/pharmacy-icon.component';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyDashboardComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private destroyRef = inject(DestroyRef);
  private loadTrigger$ = new Subject<void>();

  auth = inject(AuthFacadeService);

  isLoading = signal(true);
  loadError = signal(false);
  summary = signal<StatisticsSummaryDto | null>(null);
  revenueItems = signal<MonthlyRevenueItemDto[]>([]);
  categories = signal<CategoryItemDto[]>([]);
  stockItems = signal<StockTrendItemDto[]>([]);
  stockTimeline = signal<string[]>([]);
  stockMedications = signal<{ id: number; name: string; color: string; currentStock: number }[]>(
    []
  );
  stockMetricType = signal('current-stock-snapshot');
  stockNote = signal<string | undefined>(undefined);
  revenueChange = signal(0);
  readonly todayShortLabel = new Date().toLocaleDateString('bs-BA', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });

  stockChartTitle = computed(() =>
    this.stockMetricType() === 'stock-history-trend' ? 'Trend zaliha' : 'Stanje zaliha'
  );

  stockChartSubtitle = computed(() => {
    const note = this.stockNote();
    if (note) {
      return note;
    }

    return this.stockMetricType() === 'stock-history-trend'
      ? 'Promjene zaliha u zadnjih 30 dana'
      : 'Trenutni nivoi zaliha po lijekovima';
  });

  revenueTrendLabel = computed(() => {
    const change = this.revenueChange();
    if (change === 0) {
      return undefined;
    }

    return `${Math.abs(change).toFixed(1)}% u odnosu na prethodni mjesec`;
  });

  inventoryValueLabel = computed(() => {
    const summary = this.summary();
    if (!summary) {
      return '0 KM';
    }

    return `${summary.inventoryValue.toFixed(2)} KM`;
  });

  inventoryKpiSubtitle = computed(() => {
    const summary = this.summary();
    if (!summary) {
      return 'Trenutna procjena';
    }

    if (this.revenueTrendLabel()) {
      return `Prihod: ${summary.totalRevenue.toFixed(2)} KM`;
    }

    return 'Trenutna procjena';
  });

  recentActivities = computed((): DashboardActivityItem[] => {
    const summary = this.summary();
    if (!summary) {
      return [];
    }

    const items: DashboardActivityItem[] = [
      {
        icon: 'check-circle',
        tone: 'success',
        text: `${summary.totalMedications} aktivnih lijekova u sistemu`,
        time: 'Upravo sada',
      },
    ];

    if (summary.pendingPrescriptions > 0) {
      items.push({
        icon: 'clipboard',
        tone: 'info',
        text: `${summary.pendingPrescriptions} recept(a) čeka izdavanje`,
        time: 'Danas',
      });
    }

    if (summary.lowStockAlerts > 0) {
      items.push({
        icon: 'triangle-alert',
        tone: 'warning',
        text: `${summary.lowStockAlerts} lijek(ova) ispod minimuma zaliha`,
        time: 'Danas',
      });
    }

    if (summary.expiringSoon > 0 || summary.expiredMedications > 0) {
      items.push({
        icon: 'calendar',
        tone: 'warning',
        text: `Rok trajanja: ${summary.expiringSoon} uskoro / ${summary.expiredMedications} isteklo`,
        time: 'Inventar',
      });
    }

    items.push({
      icon: 'activity',
      tone: 'success',
      text: `Vrijednost inventara ${summary.inventoryValue.toFixed(2)} KM`,
      time: 'Ažurirano',
    });

    return items.slice(0, 5);
  });

  ngOnInit(): void {
    this.loadTrigger$
      .pipe(
        switchMap(() => {
          this.isLoading.set(true);
          this.loadError.set(false);
          return this.pharmacyApi.getDashboardStats().pipe(
            catchError(() => {
              this.loadError.set(true);
              this.summary.set(null);
              return of(null);
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((stats) => {
        this.isLoading.set(false);
        if (!stats) return;

        this.summary.set(stats.metadata.summary);
        this.revenueItems.set(stats.monthlyRevenue.data.slice(-6));
        this.categories.set(stats.topCategories.data);
        this.stockItems.set(stats.stockTrends.data);
        this.stockTimeline.set(stats.stockTrends.timeline ?? []);
        this.stockMedications.set(stats.stockTrends.medications ?? []);
        this.stockMetricType.set(stats.stockTrends.metricType);
        this.stockNote.set(stats.stockTrends.note ?? undefined);
        this.revenueChange.set(stats.monthlyRevenue.revenueChangePercentage);
      });

    this.loadTrigger$.next();
  }

  reload(): void {
    this.loadTrigger$.next();
  }
}
