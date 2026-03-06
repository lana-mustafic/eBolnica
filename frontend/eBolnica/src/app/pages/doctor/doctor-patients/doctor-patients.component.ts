import { Component, inject } from '@angular/core';
import { DoctorService } from '../../../shared/services/doctor/doctor.service';
import { DoctorAssignedPatientDto } from '../../../models/doctor-patients.dto';
import { AuthService } from '../../../shared/services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { UpdatePatientDto } from '../../../models/update-patient.dto';
import { Router } from '@angular/router';
import { PatientFilterParams } from '../../../models/patient-filters.dto';
import { Subject } from 'rxjs';
import { debounceTime, map, filter } from 'rxjs';
import { PagedResponse } from '../../../models/paged-response.dto';

@Component({
  selector: 'app-doctor-patients',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  standalone: true,
  templateUrl: './doctor-patients.component.html',
  styleUrl: './doctor-patients.component.css'
})
export class DoctorPatientsComponent {

  doctorService = inject(DoctorService);
  authService = inject(AuthService);
  fb = inject(FormBuilder);
  router = inject(Router);
  
  assignedPatients: DoctorAssignedPatientDto[] = [];
  showAddForm = false;
  showEditForm = false;
  editingPatient: DoctorAssignedPatientDto | null = null;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  isLoading = false;

  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  filters: PatientFilterParams = {
    firstName: '',
    lastName: '',
    gender: '',
    bloodType: '',
    birthYear: undefined,
    page: 1,
    pageSize: 10
  };

  private filterChanged$ = new Subject<void>();

  patientForm: FormGroup;

  constructor() {
    this.patientForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      dateOfBirth: [''],
      gender: [''],
      phoneNumber: ['', [Validators.pattern(/^[+]?[(]?[0-9]{1,4}[)]?[-\s.]?[(]?[0-9]{1,4}[)]?[-\s.]?[0-9]{1,9}$/)]],
      address: ['', [Validators.maxLength(200)]],
      bloodType: [''],
      recordNumber: ['', [Validators.maxLength(50)]]
    });

    this.filterChanged$.pipe(
      debounceTime(300),
      filter(() => true),
      map(() => ({ page: 1 }))
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadPatients();
    });
  }


  ngOnInit(){
    this.loadPatients();
  }

  loadPatients() {
    this.isLoading = true;
    const params: PatientFilterParams = {
      ...this.filters,
      page: this.currentPage,
      pageSize: this.pageSize
    };

    this.doctorService.getAssignedPatients(params).subscribe({
        next: (response: PagedResponse<DoctorAssignedPatientDto>) => {
          this.assignedPatients = response.items;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Error loading patients', err);
          this.errorMessage = 'Error loading patients';
          this.isLoading = false;
        }
      });
    }

    onFilterChange() {
    this.filterChanged$.next();
    }

     goToPage(page: number) {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadPatients();
    }

    filteredPatients() {
      return this.assignedPatients.filter(p => {
        let year: string | null = null;
        if (p.dateOfBirth) {
          const d = new Date(p.dateOfBirth);
          if (!isNaN(d.getTime())) year = d.getFullYear().toString();
      }

      const yearMatch = !this.filters.birthYear || (year && year === this.filters.birthYear.toString());

      return (!this.filters.firstName || p.firstName.toLowerCase().includes(this.filters.firstName.toLowerCase()))
        && (!this.filters.lastName || p.lastName.toLowerCase().includes(this.filters.lastName.toLowerCase()))
        && (!this.filters.gender || p.gender === this.filters.gender)
        && (!this.filters.bloodType || p.bloodType === this.filters.bloodType)
        && yearMatch;
    });
  }

  closeForms() {
    this.showAddForm = false;
    this.showEditForm = false;
    this.editingPatient = null;
    this.patientForm.reset();
    this.errorMessage = null;
    this.successMessage = null;
  }

  openMedicalRecord(patientId: number){
    this.router.navigate(['/medical-record', patientId]);
  }

  onSubmit() {
    if (this.patientForm.valid) {
      this.isLoading = true;
      this.errorMessage = null;
      this.successMessage = null;

      const formValue = this.patientForm.value;

      if (this.showEditForm && this.editingPatient) {
        const updateDto: UpdatePatientDto = {
          firstName: formValue.firstName,
          lastName: formValue.lastName,
          dateOfBirth: formValue.dateOfBirth || undefined,
          gender: formValue.gender || undefined,
          phoneNumber: formValue.phoneNumber || undefined,
          address: formValue.address || undefined,
          bloodType: formValue.bloodType || undefined,
          recordNumber: formValue.recordNumber || undefined
        };

        this.doctorService.updatePatient(this.editingPatient.id, updateDto).subscribe({
          next: () => {
            this.isLoading = false;
            this.successMessage = 'Patient updated successfully';
            this.closeForms();
            this.loadPatients();
          },
          error: (err) => {
            this.isLoading = false;
            if (err.error?.message) {
              this.errorMessage = err.error.message;
            } else if (err.error?.errors) {
              const errors = Object.values(err.error.errors).flat();
              this.errorMessage = errors.join(', ');
            } else {
              this.errorMessage = 'Error updating patient';
            }
          }
        });
      }
    } else {
      Object.keys(this.patientForm.controls).forEach(key => {
        this.patientForm.get(key)?.markAsTouched();
      });
    }
  }

}

