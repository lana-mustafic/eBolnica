import { inject, Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';

@Injectable({ providedIn: 'root' })
export class MedicationImageUrlService {
  private readonly pharmacyApi = inject(PharmacyApiService);
  private readonly cache = new Map<string, string>();

  getAuthenticatedUrl(medicationId: number, imageId: number): Observable<string> {
    const key = this.cacheKey(medicationId, imageId);
    const cached = this.cache.get(key);
    if (cached) {
      return of(cached);
    }

    return this.pharmacyApi.getMedicationImageBlob(medicationId, imageId).pipe(
      map((blob) => {
        if (!this.isDisplayableImageBlob(blob)) {
          throw new Error('Image file response is not a displayable image.');
        }
        return URL.createObjectURL(blob);
      }),
      tap((url) => this.cache.set(key, url))
    );
  }

  /** Reuse a local preview after upload so the next screen can show the image immediately. */
  prime(medicationId: number, imageId: number, objectUrl: string): void {
    const key = this.cacheKey(medicationId, imageId);
    const existing = this.cache.get(key);
    if (existing && existing !== objectUrl) {
      URL.revokeObjectURL(existing);
    }
    this.cache.set(key, objectUrl);
  }

  owns(url: string): boolean {
    for (const cached of this.cache.values()) {
      if (cached === url) {
        return true;
      }
    }
    return false;
  }

  revoke(medicationId: number, imageId: number): void {
    const key = this.cacheKey(medicationId, imageId);
    const url = this.cache.get(key);
    if (!url) {
      return;
    }
    URL.revokeObjectURL(url);
    this.cache.delete(key);
  }

  /**
   * Do not revoke cached object URLs here. Form/list/detail share this singleton;
   * destroying the upload form after save was revoking the URL the detail page just set.
   */
  revokeAll(): void {
    return;
  }

  private cacheKey(medicationId: number, imageId: number): string {
    return `${medicationId}:${imageId}`;
  }

  private isDisplayableImageBlob(blob: Blob | null): boolean {
    if (!blob || blob.size === 0) {
      return false;
    }
    const type = (blob.type || '').toLowerCase();
    if (type.includes('json') || type.includes('html') || type.startsWith('text/')) {
      return false;
    }
    return true;
  }
}
