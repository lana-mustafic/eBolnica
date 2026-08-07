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
import { catchError, forkJoin, of, Subject, switchMap } from 'rxjs';
import { resolvePharmacyApiErrorMessage } from '../shared/utils/pharmacy-api-error.util';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { PharmacyDashboardCacheService } from '../services/pharmacy-dashboard-cache.service';
import {
  CategoryItemDto,
  MonthlyRevenueItemDto,
  PharmacyActivityDto,
  StatisticsSummaryDto,
  StockTrendItemDto,
} from '../../../api-services/pharmacy/pharmacy-api.models';
import { PharmacyIconName } from '../shared/pharmacy-icon/pharmacy-icon.component';
import { ToasterService } from '../../../core/services/toaster.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { formatRelativeTime } from '../../../core/utils/relative-time.util';
import {
  mapPharmacyActivityIcon,
  mapPharmacyActivityTone,
  PharmacyActivityTone,
} from '../shared/pharmacy-activity.util';

interface DashboardActivityItem {
  id: number;
  icon: PharmacyIconName;
  tone: PharmacyActivityTone;
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
  private dashboardCache = inject(PharmacyDashboardCacheService);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);
  private loadTrigger$ = new Subject<boolean>();

  auth = inject(AuthFacadeService);

  isLoading = signal(true);
  loadError = signal(false);
  summary = signal<StatisticsSummaryDto | null>(null);
  activities = signal<PharmacyActivityDto[]>([]);
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

  recentActivities = computed((): DashboardActivityItem[] =>
    this.activities().map((activity) => ({
      id: activity.id,
      icon: mapPharmacyActivityIcon(activity),
      tone: mapPharmacyActivityTone(activity),
      text: activity.message,
      time: formatRelativeTime(activity.occurredAt),
    }))
  );

  ngOnInit(): void {
    this.loadTrigger$
      .pipe(
        switchMap((forceRefresh) => {
          this.isLoading.set(true);
          this.loadError.set(false);
          return forkJoin({
            stats: this.dashboardCache.getStats(forceRefresh).pipe(
              catchError((err) => {
                this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri učitavanju dashboard podataka.'));
                return of(null);
              })
            ),
            activities: this.pharmacyApi.listRecentActivities({ limit: 10 }).pipe(
              catchError(() => of([] as PharmacyActivityDto[]))
            ),
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(({ stats, activities }) => {
        this.isLoading.set(false);

        if (!stats) {
          this.loadError.set(true);
          this.summary.set(null);
          this.activities.set([]);
          return;
        }

        this.loadError.set(false);
        this.summary.set(stats.metadata.summary);
        this.activities.set(activities);
        this.revenueItems.set(stats.monthlyRevenue.data.slice(-6));
        this.categories.set(stats.topCategories.data ?? []);
        this.stockItems.set(stats.stockTrends.data ?? []);
        this.stockTimeline.set(stats.stockTrends.timeline ?? []);
        this.stockMedications.set(stats.stockTrends.medications ?? []);
        this.stockMetricType.set(stats.stockTrends.metricType ?? 'current-stock-snapshot');
        this.stockNote.set(stats.stockTrends.note ?? undefined);
        this.revenueChange.set(stats.monthlyRevenue.revenueChangePercentage);
      });

    this.loadTrigger$.next(false);
  }

  reload(): void {
    this.loadTrigger$.next(true);
  }
}
