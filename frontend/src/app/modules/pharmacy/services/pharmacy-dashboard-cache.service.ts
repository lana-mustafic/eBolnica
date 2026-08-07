import { inject, Injectable } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { DashboardStatsResponseDto } from '../../../api-services/pharmacy/pharmacy-api.models';

@Injectable({ providedIn: 'root' })
export class PharmacyDashboardCacheService {
  private static readonly TTL_MS = 5 * 60 * 1000;

  private readonly pharmacyApi = inject(PharmacyApiService);
  private statsCache: { value: DashboardStatsResponseDto; expiresAt: number } | null = null;

  getStats(forceRefresh = false): Observable<DashboardStatsResponseDto> {
    const now = Date.now();
    if (!forceRefresh && this.statsCache && this.statsCache.expiresAt > now) {
      return of(this.statsCache.value);
    }

    return this.pharmacyApi.getDashboardStats().pipe(
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
