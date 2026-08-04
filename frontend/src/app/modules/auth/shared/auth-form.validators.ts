import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;

  if (password !== confirmPassword) {
    control.get('confirmPassword')?.setErrors({ passwordMismatch: true });
    return { passwordMismatch: true };
  }

  const confirmControl = control.get('confirmPassword');
  if (confirmControl?.hasError('passwordMismatch')) {
    const errors = { ...confirmControl.errors };
    delete errors['passwordMismatch'];
    confirmControl.setErrors(Object.keys(errors).length ? errors : null);
  }

  return null;
};
