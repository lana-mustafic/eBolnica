import { fakeAsync, tick } from '@angular/core/testing';
import { FormControl, ValidationErrors } from '@angular/forms';
import { of, throwError } from 'rxjs';
import {
  medicationNameAsyncValidator,
  MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR,
  MEDICATION_NAME_EXISTS_ERROR,
  MedicationNameAvailabilityFn,
} from './medication-name-async.validator';

describe('medicationNameAsyncValidator', () => {
  const debounceMs = 0;

  function runValidator(
    value: string,
    checkAvailability: MedicationNameAvailabilityFn,
    options?: Parameters<typeof medicationNameAsyncValidator>[1]
  ): ValidationErrors | null {
    const validator = medicationNameAsyncValidator(checkAvailability, {
      debounceMs,
      ...options,
    });
    const control = new FormControl(value);
    let errors: ValidationErrors | null = null;

    validator(control).subscribe((result) => {
      errors = result;
    });

    tick(debounceMs);
    return errors;
  }

  it('returns null when the name is available', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(of({ isAvailable: true }));
    const errors = runValidator('Paracetamol', check);

    expect(check).toHaveBeenCalledWith('Paracetamol', undefined);
    expect(errors).toBeNull();
  }));

  it('returns duplicate error when the name is taken', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(of({ isAvailable: false }));
    const errors = runValidator('Paracetamol', check);

    expect(errors).toEqual({ [MEDICATION_NAME_EXISTS_ERROR]: true });
  }));

  it('returns unavailable error when checkFailed is true', fakeAsync(() => {
    const check = jasmine
      .createSpy('check')
      .and.returnValue(of({ isAvailable: false, checkFailed: true }));
    const errors = runValidator('Paracetamol', check);

    expect(errors).toEqual({ [MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]: true });
  }));

  it('returns unavailable error when the check observable errors', fakeAsync(() => {
    const check = jasmine
      .createSpy('check')
      .and.returnValue(throwError(() => new Error('network error')));
    const errors = runValidator('Paracetamol', check);

    expect(errors).toEqual({ [MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]: true });
  }));

  it('skips validation for empty or too-short names', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(of({ isAvailable: true }));

    runValidator('', check);
    runValidator('ab', check, { minLength: 3 });

    expect(check).not.toHaveBeenCalled();
  }));

  it('trims whitespace before checking', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(of({ isAvailable: true }));
    runValidator('  Paracetamol  ', check);

    expect(check).toHaveBeenCalledWith('Paracetamol', undefined);
  }));

  it('passes excludeId to the availability check', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(of({ isAvailable: true }));
    runValidator('Paracetamol', check, { excludeId: 5 });

    expect(check).toHaveBeenCalledWith('Paracetamol', 5);
  }));

  it('resolves excludeId from a factory function', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(of({ isAvailable: true }));
    runValidator('Paracetamol', check, { excludeId: () => 5 });

    expect(check).toHaveBeenCalledWith('Paracetamol', 5);
  }));

  it('debounces availability checks', fakeAsync(() => {
    const check = jasmine.createSpy('check').and.returnValue(of({ isAvailable: true }));
    const validator = medicationNameAsyncValidator(check, { debounceMs: 400, minLength: 1 });
    const control = new FormControl('Paracetamol');

    validator(control).subscribe();
    tick(399);
    expect(check).not.toHaveBeenCalled();

    tick(1);
    expect(check).toHaveBeenCalledOnceWith('Paracetamol', undefined);
  }));
});
