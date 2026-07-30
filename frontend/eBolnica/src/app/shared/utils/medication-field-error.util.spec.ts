import { FormControl } from '@angular/forms';
import {
  getMedicationFieldErrorMessage,
  isMedicationFieldInvalidForDisplay,
  isMedicationNameCheckUnavailable
} from './medication-field-error.util';
import {
  MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR,
  MEDICATION_NAME_EXISTS_ERROR
} from '../validators/medication-name-async.validator';

describe('medication-field-error.util', () => {
  it('maps duplicate name error without requiring touch', () => {
    const control = new FormControl('Paracetamol');
    control.setErrors({ [MEDICATION_NAME_EXISTS_ERROR]: true });

    expect(getMedicationFieldErrorMessage(control, 'Name')).toBe('Medication name already exists');
    expect(isMedicationFieldInvalidForDisplay(control)).toBeTrue();
  });

  it('maps unavailable name check error', () => {
    const control = new FormControl('Paracetamol');
    control.setErrors({ [MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]: true });

    expect(getMedicationFieldErrorMessage(control, 'Name')).toBe(
      'Unable to verify name availability. Please try again.'
    );
    expect(isMedicationNameCheckUnavailable(control)).toBeTrue();
  });

  it('maps sync required error only when touched', () => {
    const control = new FormControl('');
    control.setErrors({ required: true });

    expect(getMedicationFieldErrorMessage(control, 'Category')).toBeNull();

    control.markAsTouched();
    expect(getMedicationFieldErrorMessage(control, 'Category')).toBe('Category is required');
  });

  it('maps minlength and pastDate errors', () => {
    const nameControl = new FormControl('ab');
    nameControl.setErrors({ minlength: { requiredLength: 3, actualLength: 2 } });
    nameControl.markAsTouched();
    expect(getMedicationFieldErrorMessage(nameControl, 'Name')).toBe(
      'Name must be at least 3 characters'
    );

    const expiryControl = new FormControl('2020-01-01');
    expiryControl.setErrors({ pastDate: true });
    expiryControl.markAsTouched();
    expect(getMedicationFieldErrorMessage(expiryControl, 'Expiry Date')).toBe(
      'Expiry date must be in the future'
    );
  });
});
