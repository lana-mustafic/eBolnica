import { HttpEvent, HttpEventType } from '@angular/common/http';
import { Observable, from, of, throwError } from 'rxjs';
import { catchError, concatMap, filter, map, reduce, tap } from 'rxjs/operators';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import {
  extractMedicationImageUploadResponse,
  getHttpUploadProgressPercent
} from './medication-image-upload-progress.util';
import {
  calculateSequentialBatchProgress,
  MedicationImageUploadEntry
} from './medication-image-upload-status.util';
import { createPendingMedicationImageId } from './medication-image-pending-queue.util';

export type { MedicationImageUploadEntry };

export function createMedicationImageUploadEntry(
  file: File,
  uploadKey?: string
): MedicationImageUploadEntry {
  return {
    file,
    uploadKey: uploadKey ?? createPendingMedicationImageId()
  };
}

export interface MedicationImageUploadError {
  uploadKey: string;
  fileName: string;
  message: string;
}

export interface MedicationImageUploadBatchResult {
  uploaded: MedicationImageDto[];
  errors: MedicationImageUploadError[];
}

export type MedicationImageUploadFn = (
  medicationId: number,
  file: File
) => Observable<HttpEvent<MedicationImageDto>>;

export interface MedicationImageUploadProgressHandlers {
  onFileStart?: (uploadKey: string, fileName: string, index: number, total: number) => void;
  onFileProgress?: (uploadKey: string, fileName: string, progressPercent: number, index: number, total: number) => void;
  onBatchProgress?: (overallPercent: number, index: number, total: number) => void;
  onFileComplete?: (uploadKey: string, fileName: string, image: MedicationImageDto) => void;
  onFileError?: (uploadKey: string, fileName: string, message: string) => void;
}

export function getMedicationImageUploadErrorMessage(
  error: { status?: number; error?: { message?: string } | string },
  fileName: string
): string {
  if (error?.status === 403) {
    const message = typeof error.error === 'string' ? error.error : error.error?.message;
    return message || `"${fileName}" failed security scan and was rejected.`;
  }

  if (error?.status === 400) {
    const message = typeof error.error === 'string' ? error.error : error.error?.message;
    if (message) {
      return message;
    }
  }

  if (typeof error.error === 'object' && error.error?.message) {
    return error.error.message;
  }

  if (typeof error.error === 'string' && error.error.trim()) {
    return error.error;
  }

  return `Failed to upload "${fileName}". Please try again.`;
}

export function uploadMedicationImagesSequentially(
  medicationId: number,
  entries: MedicationImageUploadEntry[],
  uploadFn: MedicationImageUploadFn,
  progress?: MedicationImageUploadProgressHandlers
): Observable<MedicationImageUploadBatchResult> {
  if (entries.length === 0) {
    return of({ uploaded: [], errors: [] });
  }

  return from(entries).pipe(
    concatMap((entry, index) => {
      const { file, uploadKey } = entry;
      progress?.onFileStart?.(uploadKey, file.name, index, entries.length);
      progress?.onBatchProgress?.(
        calculateSequentialBatchProgress(index, entries.length, 0),
        index,
        entries.length
      );

      return uploadFn(medicationId, file).pipe(
        tap(event => {
          const progressPercent = getHttpUploadProgressPercent(event);
          if (progressPercent != null) {
            progress?.onFileProgress?.(uploadKey, file.name, progressPercent, index, entries.length);
            progress?.onBatchProgress?.(
              calculateSequentialBatchProgress(index, entries.length, progressPercent),
              index,
              entries.length
            );
          }
        }),
        filter((event): event is HttpEvent<MedicationImageDto> => event.type === HttpEventType.Response),
        map(event => {
          const image = extractMedicationImageUploadResponse(event);
          if (!image) {
            throw { status: 500, error: { message: 'Upload completed without a response body.' } };
          }
          return image;
        }),
        tap(image => progress?.onFileComplete?.(uploadKey, file.name, image)),
        tap(() => {
          progress?.onBatchProgress?.(
            calculateSequentialBatchProgress(index + 1, entries.length, 0),
            index,
            entries.length
          );
        }),
        map(image => ({ uploadKey, fileName: file.name, image })),
        catchError(error => {
          const errorMessage = getMedicationImageUploadErrorMessage(error, file.name);
          progress?.onFileError?.(uploadKey, file.name, errorMessage);
          return of({
            uploadKey,
            fileName: file.name,
            errorMessage
          });
        })
      );
    }),
    reduce(
      (result, item) => {
        if ('image' in item && item.image) {
          result.uploaded.push(item.image);
        } else if ('errorMessage' in item && item.errorMessage) {
          result.errors.push({
            uploadKey: item.uploadKey,
            fileName: item.fileName,
            message: item.errorMessage
          });
        }
        return result;
      },
      { uploaded: [] as MedicationImageDto[], errors: [] as MedicationImageUploadError[] }
    )
  );
}
