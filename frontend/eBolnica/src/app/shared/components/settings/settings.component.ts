import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { AdminService } from '../../services/admin.service';
import { DoctorService } from '../../services/doctor/doctor.service';
import { Location } from '@angular/common';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {

  private authService = inject(AuthService);
  private adminService = inject(AdminService);
  private doctorService = inject(DoctorService);
  private fb = inject(FormBuilder);
  private location = inject(Location);

  userRole: string | null = null;
  successMessage: string | null = null;
  errorMessage: string | null = null;
  isLoading = false;

  adminForm!: FormGroup;
  doctorForm!: FormGroup;

  ngOnInit(): void {
    this.userRole = this.authService.getUserType();
    this.initForms();

    if (this.userRole === 'Doctor') {
      this.loadDoctorData();
    } else if (this.userRole === 'Admin') {
      this.loadAdminData();
    }
  }

  initForms(): void {
    this.adminForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]]
    });

    this.doctorForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      phoneNumber: [''],
      specialization: ['', Validators.required],
      address: ['']
    });
  }

  loadDoctorData(): void {
    this.isLoading = true;
    this.doctorService.getDoctorData().subscribe({
      next: (data) => {
        this.doctorForm.patchValue({
          firstName: data.firstName,
          lastName: data.lastName,
          phoneNumber: data.phoneNumber,
          specialization: data.specialization,
          address: data.address
        });
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load profile data.';
        this.isLoading = false;
      }
    });
  }

  goBack(): void {
    this.location.back();
  }

  loadAdminData(): void {
    this.isLoading = true;
    const token = this.authService.getToken();
    if (!token) return;
    
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.adminForm.patchValue({
        firstName: payload.firstName,
        lastName: payload.lastName,
        email: payload.email
      });
    } catch {
      this.errorMessage = 'Failed to load profile data.';
    }
    this.isLoading = false;
  }

  onSubmitAdmin(): void {
    if (this.adminForm.invalid) {
      this.adminForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.successMessage = null;
    this.errorMessage = null;

    const userId = this.getUserIdFromToken();
    if (!userId) {
      this.errorMessage = 'User ID not found.';
      return;
    }

    this.adminService.updateUser(userId, this.adminForm.value).subscribe({
      next: () => {
        this.successMessage = 'Settings saved successfully!';
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Failed to save settings.';
        this.isLoading = false;
      }
    });
  }

  onSubmitDoctor(): void {
    if (this.doctorForm.invalid) {
      this.doctorForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.successMessage = null;
    this.errorMessage = null;

    this.doctorService.editDoctorData(this.doctorForm.value).subscribe({
      next: () => {
        this.successMessage = 'Settings saved successfully!';
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Failed to save settings.';
        this.isLoading = false;
      }
    });
  }

  private getUserIdFromToken(): string | null {
    const token = this.authService.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.sub || null;
    } catch {
      return null;
    }
  }

  get adminFirstName() { return this.adminForm.get('firstName'); }
  get adminLastName() { return this.adminForm.get('lastName'); }
  get adminEmail() { return this.adminForm.get('email'); }
  get doctorFirstName() { return this.doctorForm.get('firstName'); }
  get doctorLastName() { return this.doctorForm.get('lastName'); }
  get doctorSpecialization() { return this.doctorForm.get('specialization'); }
}
