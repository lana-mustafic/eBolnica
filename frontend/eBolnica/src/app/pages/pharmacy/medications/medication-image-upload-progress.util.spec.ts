import { HttpEventType, HttpResponse } from '@angular/common/http';
import {
  extractMedicationImageUploadResponse,
  getHttpUploadProgressPercent
} from './medication-image-upload-progress.util';

describe('medication-image-upload-progress.util', () => {
  it('derives upload percent from UploadProgress events', () => {
    expect(getHttpUploadProgressPercent({
      type: HttpEventType.UploadProgress,
      loaded: 50,
      total: 100
    })).toBe(50);
  });

  it('returns 100 for Response events', () => {
    expect(getHttpUploadProgressPercent(new HttpResponse({ body: null }))).toBe(100);
  });

  it('extracts uploaded image DTO from Response events', () => {
    const image = {
      id: 1,
      medicationId: 42,
      imageUrl: '/uploads/a.jpg',
      isPrimary: false,
      sortOrder: 0,
      uploadedAt: '2026-07-30T00:00:00Z'
    };

    expect(extractMedicationImageUploadResponse(new HttpResponse({ body: image }))).toEqual(image);
  });
});
