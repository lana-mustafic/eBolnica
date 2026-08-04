import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { BaseComponent } from '../../../core/components/base-classes/base-component';
import { AuthApiService } from '../../../api-services/auth/auth-api.service';
import { RegisterPatientCommand } from '../../../api-services/auth/auth-api.model';
import { passwordMatchValidator } from '../shared/auth-form.validators';

@Component({
  selector: 'app-patient-registration',
  standalone: false,
  templateUrl: './patient-registration.component.html',
  styleUrl: './patient-registration.component.scss',
})
export class PatientRegistrationComponent extends BaseComponent {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private router = inject(Router);

  hidePassword = true;
  successMessage: string | null = null;

  form = this.fb.group(
    {
      firstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
      dateOfBirth: [''],
      gender: [''],
    },
    { validators: passwordMatchValidator }
  );

  onSubmit(): void {
    if (this.form.invalid || this.isLoading) return;

    this.startLoading();
    this.successMessage = null;

    const raw = this.form.getRawValue();
    const payload: RegisterPatientCommand = {
      firstName: raw.firstName ?? '',
      lastName: raw.lastName ?? '',
      email: raw.email ?? '',
      password: raw.password ?? '',
      confirmPassword: raw.confirmPassword ?? '',
      dateOfBirth: raw.dateOfBirth || null,
      gender: raw.gender || null,
    };

    this.authApi.registerPatient(payload).subscribe({
      next: () => {
        this.stopLoading();
        this.successMessage =
          'Registracija uspješna. Vaš nalog čeka odobrenje administratora.';
        this.form.reset();
      },
      error: (err) => {
        const msg =
          err?.error?.message ??
          (err.status === 409 ? 'Email je već registriran.' : 'Registracija nije uspjela.');
        this.stopLoading(msg);
      },
    });
  }
}
