import { HttpEvent, HttpEventType } from '@angular/common/http';
import { Observable, from, of, throwError } from 'rxjs';
import { catchError, concatMap, filter, map, reduce, tap } from 'rxjs/operators';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import {
  extractMedicationImageUploadResponse,
  getHttpUploadProgressPercent
} from './medication-image-upload-progress.util';

export interface MedicationImageUploadError {
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
  onFileStart?: (fileName: string, index: number, total: number) => void;
  onFileProgress?: (fileName: string, progressPercent: number, index: number, total: number) => void;
  onFileComplete?: (fileName: string, image: MedicationImageDto) => void;
  onFileError?: (fileName: string, message: string) => void;
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
  files: File[],
  uploadFn: MedicationImageUploadFn,
  progress?: MedicationImageUploadProgressHandlers
): Observable<MedicationImageUploadBatchResult> {
  if (files.length === 0) {
    return of({ uploaded: [], errors: [] });
  }

  return from(files).pipe(
    concatMap((file, index) => {
      progress?.onFileStart?.(file.name, index, files.length);

      return uploadFn(medicationId, file).pipe(
        tap(event => {
          const progressPercent = getHttpUploadProgressPercent(event);
          if (progressPercent != null) {
            progress?.onFileProgress?.(file.name, progressPercent, index, files.length);
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
        tap(image => progress?.onFileComplete?.(file.name, image)),
        map(image => ({ fileName: file.name, image })),
        catchError(error => {
          const errorMessage = getMedicationImageUploadErrorMessage(error, file.name);
          progress?.onFileError?.(file.name, errorMessage);
          return of({
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
          result.errors.push({ fileName: item.fileName, message: item.errorMessage });
        }
        return result;
      },
      { uploaded: [] as MedicationImageDto[], errors: [] as MedicationImageUploadError[] }
    )
  );
}
