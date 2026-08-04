import { HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { MedicationImageDto } from '../../../../api-services/pharmacy/pharmacy-api.models';

export function getHttpUploadProgressPercent(event: HttpEvent<unknown>): number | null {
  if (event.type === HttpEventType.UploadProgress && event.total) {
    return Math.round((event.loaded / event.total) * 100);
  }

  if (event.type === HttpEventType.Response) {
    return 100;
  }

  return null;
}

export function extractMedicationImageUploadResponse(
  event: HttpEvent<MedicationImageDto>
): MedicationImageDto | null {
  if (event.type !== HttpEventType.Response) {
    return null;
  }

  return (event as HttpResponse<MedicationImageDto>).body ?? null;
}
