import { inject, Injectable } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { DashboardStatsResponseDto, GetDashboardStatsRequest } from '../../../api-services/pharmacy/pharmacy-api.models';

@Injectable({ providedIn: 'root' })
export class PharmacyDashboardCacheService {
  private static readonly TTL_MS = 5 * 60 * 1000;
  private static readonly DEFAULT_REQUEST: GetDashboardStatsRequest = {
    revenueMonths: 12,
    topCategoriesCount: 8,
  };

  private readonly pharmacyApi = inject(PharmacyApiService);
  private statsCache: { value: DashboardStatsResponseDto; expiresAt: number } | null = null;

  getStats(
    forceRefresh = false,
    request: GetDashboardStatsRequest = PharmacyDashboardCacheService.DEFAULT_REQUEST
  ): Observable<DashboardStatsResponseDto> {
    const now = Date.now();
    if (!forceRefresh && this.statsCache && this.statsCache.expiresAt > now) {
      return of(this.statsCache.value);
    }

    return this.pharmacyApi.getDashboardStats(request).pipe(
      tap((stats) => {
        this.statsCache = {
          value: stats,
          expiresAt: Date.now() + PharmacyDashboardCacheService.TTL_MS,
        };
      })
    );
  }

  invalidate(): void {
    this.statsCache = null;
  }
}
