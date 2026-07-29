import { AbstractControl, ValidationErrors } from '@angular/forms';
import {
  MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR,
  MEDICATION_NAME_EXISTS_ERROR,
  MEDICATION_NAME_VALIDATOR_MESSAGES
} from '../validators/medication-name-async.validator';

export interface MedicationFieldErrorOptions {
  /** When true, sync validators require the control to be touched (default: true). */
  requireTouched?: boolean;
}

/**
 * Maps medication form control validation errors to user-facing messages.
 */
export function getMedicationFieldErrorMessage(
  control: AbstractControl | null | undefined,
  fieldLabel: string,
  options: MedicationFieldErrorOptions = {}
): string | null {
  const requireTouched = options.requireTouched ?? true;

  if (!control?.errors) {
    return null;
  }

  const asyncMessage = getAsyncNameErrorMessage(control.errors);
  if (asyncMessage) {
    return asyncMessage;
  }

  if (requireTouched && !control.touched) {
    return null;
  }

  return mapSyncValidationError(control.errors, fieldLabel);
}

/**
 * Whether a field should show invalid styling (includes async name errors without touch).
 */
export function isMedicationFieldInvalidForDisplay(
  control: AbstractControl | null | undefined
): boolean {
  if (!control) {
    return false;
  }

  if (hasAsyncNameError(control.errors)) {
    return true;
  }

  return control.invalid && control.touched;
}

export function isMedicationNameCheckUnavailable(
  control: AbstractControl | null | undefined
): boolean {
  return control?.hasError(MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR) ?? false;
}

function getAsyncNameErrorMessage(errors: ValidationErrors): string | null {
  if (errors[MEDICATION_NAME_EXISTS_ERROR]) {
    return MEDICATION_NAME_VALIDATOR_MESSAGES[MEDICATION_NAME_EXISTS_ERROR];
  }

  if (errors[MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]) {
    return MEDICATION_NAME_VALIDATOR_MESSAGES[MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR];
  }

  return null;
}

function hasAsyncNameError(errors: ValidationErrors | null): boolean {
  if (!errors) {
    return false;
  }

  return !!(
    errors[MEDICATION_NAME_EXISTS_ERROR] ||
    errors[MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]
  );
}

function mapSyncValidationError(errors: ValidationErrors, fieldLabel: string): string | null {
  if (errors['required']) {
    return `${fieldLabel} is required`;
  }

  if (errors['minlength']) {
    return `${fieldLabel} must be at least ${errors['minlength'].requiredLength} characters`;
  }

  if (errors['maxlength']) {
    return `${fieldLabel} must not exceed ${errors['maxlength'].requiredLength} characters`;
  }

  if (errors['min']) {
    return `${fieldLabel} must be at least ${errors['min'].min}`;
  }

  if (errors['pastDate']) {
    return 'Expiry date must be in the future';
  }

  return null;
}
