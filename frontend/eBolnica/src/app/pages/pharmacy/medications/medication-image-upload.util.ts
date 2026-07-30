import { Observable, from, of } from 'rxjs';
import { catchError, concatMap, map, reduce } from 'rxjs/operators';
import { MedicationImageDto } from '../../../models/medication-image.dto';

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
) => Observable<MedicationImageDto>;

export function getMedicationImageUploadErrorMessage(
  error: { status?: number; error?: { message?: string } | string },
  fileName: string
): string {
  if (error?.status === 403) {
    const message = typeof error.error === 'string' ? error.error : error.error?.message;
    return message || `"${fileName}" failed security scan and was rejected.`;
  }

  if (typeof error.error === 'object' && error.error?.message) {
    return error.error.message;
  }

  return `Failed to upload "${fileName}". Please try again.`;
}

export function uploadMedicationImagesSequentially(
  medicationId: number,
  files: File[],
  uploadFn: MedicationImageUploadFn
): Observable<MedicationImageUploadBatchResult> {
  if (files.length === 0) {
    return of({ uploaded: [], errors: [] });
  }

  return from(files).pipe(
    concatMap(file =>
      uploadFn(medicationId, file).pipe(
        map(image => ({ fileName: file.name, image })),
        catchError(error =>
          of({
            fileName: file.name,
            errorMessage: getMedicationImageUploadErrorMessage(error, file.name)
          })
        )
      )
    ),
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
