import { AbstractControl, AsyncValidatorFn, ValidationErrors } from '@angular/forms';
import { Observable, of, timer } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

/** Validation error key when the name is already taken. */
export const MEDICATION_NAME_EXISTS_ERROR = 'medicationNameExists';

/** Validation error key when the availability check could not be completed. */
export const MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR = 'medicationNameCheckUnavailable';

export const MEDICATION_NAME_VALIDATOR_MESSAGES: Record<string, string> = {
  [MEDICATION_NAME_EXISTS_ERROR]: 'Medication name already exists',
  [MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]:
    'Unable to verify name availability. Please try again.'
};

export interface MedicationNameAvailabilityResult {
  isAvailable: boolean;
  /** True when the check failed due to network/API error (not a confirmed duplicate). */
  checkFailed?: boolean;
}

export type MedicationNameAvailabilityFn = (
  name: string,
  excludeId?: number
) => Observable<MedicationNameAvailabilityResult>;

export interface MedicationNameAsyncValidatorOptions {
  /** Medication ID to exclude during edit (current record). */
  excludeId?: number | (() => number | undefined | null);
  /** Debounce delay in ms before calling the API (default: 400). */
  debounceMs?: number;
  /** Minimum trimmed name length before running the check (default: 3). */
  minLength?: number;
}

/**
 * Async validator factory for unique medication names.
 * Delegates to an injected availability check (typically a pharmacy API call).
 */
export function medicationNameAsyncValidator(
  checkAvailability: MedicationNameAvailabilityFn,
  options: MedicationNameAsyncValidatorOptions = {}
): AsyncValidatorFn {
  const debounceMs = options.debounceMs ?? 400;
  const minLength = options.minLength ?? 3;

  return (control: AbstractControl): Observable<ValidationErrors | null> => {
    const name = normalizeNameValue(control.value);

    if (!name || name.length < minLength) {
      return of(null);
    }

    const excludeId = resolveExcludeId(options.excludeId);

    return timer(debounceMs).pipe(
      switchMap(() => checkAvailability(name, excludeId)),
      map(result => mapAvailabilityResult(result)),
      catchError(() =>
        of({ [MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]: true } as ValidationErrors)
      )
    );
  };
}

function normalizeNameValue(value: unknown): string {
  return typeof value === 'string' ? value.trim() : '';
}

function resolveExcludeId(
  excludeId?: number | (() => number | undefined | null)
): number | undefined {
  if (excludeId == null) {
    return undefined;
  }

  if (typeof excludeId === 'function') {
    const resolved = excludeId();
    return resolved ?? undefined;
  }

  return excludeId;
}

function mapAvailabilityResult(
  result: MedicationNameAvailabilityResult
): ValidationErrors | null {
  if (result.checkFailed) {
    return { [MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]: true };
  }

  if (!result.isAvailable) {
    return { [MEDICATION_NAME_EXISTS_ERROR]: true };
  }

  return null;
}
