import { HttpErrorResponse } from '@angular/common/http';
import { getApiErrorCode, getApiErrorMessage } from '../../../../core/utils/api-error.util';

export const PHARMACY_ERROR_CODES = [
  'auth.not_authenticated',
  'auth.not_approved',
  'validation.failed',
  'validation.error',
  'not_found',
  'conflict',
  'export.limit_exceeded',
  'import.limit_exceeded',
  'upload.no_file',
  'upload.file_too_large',
  'prescription.insufficient_stock',
  'prescription.medication_otc',
  'prescription.already_processed',
  'prescription.medication_inactive',
  'prescription.medication_expired',
  'prescription.medication_missing',
  'prescription.no_items',
  'prescription.report_access',
  'prescription.report_patient_mismatch',
  'prescription.patient_access',
  'medication.pending_prescriptions',
  'medication.prescription_history',
  'image.reorder_invalid',
] as const;

export type PharmacyErrorCode = (typeof PHARMACY_ERROR_CODES)[number];

export function asHttpErrorResponse(error: unknown): HttpErrorResponse | null {
  return error instanceof HttpErrorResponse ? error : null;
}

export function resolvePharmacyApiErrorMessage(error: unknown, fallback: string): string {
  const httpError = asHttpErrorResponse(error);
  if (!httpError) {
    return fallback;
  }

  return getApiErrorMessage(httpError, fallback);
}

export function resolvePharmacyApiErrorCode(error: unknown): string | undefined {
  const httpError = asHttpErrorResponse(error);
  if (!httpError) {
    return undefined;
  }

  return getApiErrorCode(httpError);
}

export function isPharmacyErrorCode(error: unknown, code: PharmacyErrorCode): boolean {
  return resolvePharmacyApiErrorCode(error) === code;
}
