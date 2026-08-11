import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';
import { PharmacyDashboardCacheService } from '../services/pharmacy-dashboard-cache.service';

export const pharmacyDashboardCacheInterceptor: HttpInterceptorFn = (req, next) => {
  if (!shouldInvalidateDashboardCache(req)) {
    return next(req);
  }

  const cache = inject(PharmacyDashboardCacheService);

  return next(req).pipe(
    tap((event) => {
      if (event instanceof HttpResponse && event.ok) {
        cache.invalidate();
      }
    })
  );
};

function shouldInvalidateDashboardCache(req: { method: string; url: string }): boolean {
  if (req.method === 'GET' || !req.url.includes('/api/pharmacy/')) {
    return false;
  }

  // Image operations do not affect dashboard analytics aggregates.
  if (req.url.includes('/images')) {
    return false;
  }

  return true;
}
