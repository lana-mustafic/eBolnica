import { HttpErrorResponse } from '@angular/common/http';

export interface ApiErrorBody {
  code?: string;
  message?: string;
  traceId?: string;
  details?: string;
}

const PHARMACY_ERROR_MESSAGES: Record<string, string> = {
  'prescription.insufficient_stock': 'Nedovoljna zaliha za izdavanje recepta.',
  'prescription.medication_otc': 'Odabrani lijek ne zahtijeva recept.',
  'prescription.already_processed': 'Recept je već obrađen.',
  'prescription.medication_inactive': 'Jedan od lijekova na receptu nije aktivan.',
  'prescription.medication_expired': 'Jedan od lijekova na receptu je istekao.',
  'medication.pending_prescriptions': 'Lijek ima recepte na čekanju i ne može biti deaktiviran.',
  'upload.no_file': 'Datoteka nije odabrana.',
  'upload.file_too_large': 'Datoteka prelazi limit od 5 MB.',
};

export function getApiErrorBody(error: HttpErrorResponse): ApiErrorBody | null {
  const body = error.error;
  if (!body || typeof body !== 'object') {
    return null;
  }

  return body as ApiErrorBody;
}

export function getApiErrorCode(error: HttpErrorResponse): string | undefined {
  return getApiErrorBody(error)?.code;
}

export function getApiErrorMessage(error: HttpErrorResponse, fallback = 'Došlo je do greške.'): string {
  const body = getApiErrorBody(error);
  const code = body?.code;

  if (code && PHARMACY_ERROR_MESSAGES[code]) {
    return PHARMACY_ERROR_MESSAGES[code];
  }

  if (body?.message) {
    return body.message;
  }

  if (error.error?.title) {
    return error.error.title;
  }

  if (error.error?.errors && typeof error.error.errors === 'object') {
    const errors = Object.values(error.error.errors).flat();
    if (errors.length > 0) {
      return errors.join(', ');
    }
  }

  switch (error.status) {
    case 0:
      return 'Nije moguće povezati se sa serverom.';
    case 400:
      return 'Neispravan zahtjev.';
    case 401:
      return 'Niste prijavljeni.';
    case 403:
      return 'Nemate dozvolu za ovu radnju.';
    case 404:
      return 'Traženi resurs nije pronađen.';
    case 409:
      return 'Operacija nije moguća zbog konflikta stanja.';
    case 500:
      return 'Greška na serveru.';
    default:
      return fallback;
  }
}
