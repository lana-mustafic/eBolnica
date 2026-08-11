import { inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';

@Injectable({ providedIn: 'root' })
export class MedicationImageUrlService {
  private readonly pharmacyApi = inject(PharmacyApiService);
  private readonly cache = new Map<string, string>();

  getAuthenticatedUrl(medicationId: number, imageId: number): Observable<string> {
    const key = `${medicationId}:${imageId}`;
    const cached = this.cache.get(key);
    if (cached) {
      return of(cached);
    }

    return this.pharmacyApi.getMedicationImageBlob(medicationId, imageId).pipe(
      map((blob) => URL.createObjectURL(blob)),
      tap((url) => this.cache.set(key, url))
    );
  }

  revoke(medicationId: number, imageId: number): void {
    const key = `${medicationId}:${imageId}`;
    const url = this.cache.get(key);
    if (!url) {
      return;
    }
    URL.revokeObjectURL(url);
    this.cache.delete(key);
  }

  revokeAll(): void {
    for (const url of this.cache.values()) {
      URL.revokeObjectURL(url);
    }
    this.cache.clear();
  }
}
