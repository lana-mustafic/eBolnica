import { HttpErrorResponse } from '@angular/common/http';

export interface ApiErrorBody {
  code?: string;
  message?: string;
  traceId?: string;
  details?: string;
}

const PHARMACY_ERROR_MESSAGES: Record<string, string> = {
  'auth.not_authenticated': 'Niste prijavljeni.',
  'auth.not_approved': 'Vaš nalog nije odobren.',
  'validation.failed': 'Neispravan zahtjev.',
  'validation.error': 'Validacija nije uspjela.',
  'not_found': 'Traženi resurs nije pronađen.',
  'conflict': 'Operacija nije moguća zbog konflikta stanja.',
  'export.limit_exceeded': 'Previše stavki za PDF izvoz. Sužite filtere ili koristite CSV izvoz.',
  'import.limit_exceeded': 'CSV import prelazi dozvoljeni broj redova.',
  'upload.no_file': 'Datoteka nije odabrana.',
  'upload.file_too_large': 'Datoteka prelazi limit od 5 MB.',
  'prescription.insufficient_stock': 'Nedovoljna zaliha za izdavanje recepta.',
  'prescription.medication_otc': 'Odabrani lijek ne zahtijeva recept.',
  'prescription.already_processed': 'Recept je već obrađen.',
  'prescription.medication_inactive': 'Jedan od lijekova na receptu nije aktivan.',
  'prescription.medication_expired': 'Jedan od lijekova na receptu je istekao.',
  'prescription.medication_missing': 'Jedan ili više lijekova nije pronađen.',
  'prescription.no_items': 'Recept nema stavki za izdavanje.',
  'prescription.report_access': 'Medicinski izvještaj vam ne pripada.',
  'prescription.report_patient_mismatch': 'Medicinski izvještaj ne pripada odabranom pacijentu.',
  'prescription.patient_access': 'Pacijent vam nije dodijeljen.',
  'medication.pending_prescriptions': 'Lijek ima recepte na čekanju i ne može biti deaktiviran.',
  'medication.prescription_history': 'Lijek se ne može obrisati jer postoji u historiji recepata.',
  'image.reorder_invalid': 'Neispravan redoslijed slika.',
};

export function getApiErrorBody(error: HttpErrorResponse): ApiErrorBody | null {
  const body = error.error;
  if (!body || typeof body !== 'object') {
    return null;
  }

  const record = body as Record<string, unknown>;
  return {
    code: readString(record, 'code', 'Code'),
    message: readString(record, 'message', 'Message'),
    traceId: readString(record, 'traceId', 'TraceId'),
    details: readString(record, 'details', 'Details'),
  };
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
      return PHARMACY_ERROR_MESSAGES['conflict'];
    case 500:
      return 'Greška na serveru.';
    default:
      return fallback;
  }
}

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === 'string' && value.trim()) {
      return value;
    }
  }

  return undefined;
}
