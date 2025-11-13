import { Component, inject, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DoctorService } from '../../../shared/services/doctor/doctor.service';
import { PatientSearchDto } from '../../../models/patient-search.dto';
import { AssignPatientDto } from '../../../models/assign-patient.dto';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-patient-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './patient-wizard.component.html',
  styleUrl: './patient-wizard.component.css'
})
export class PatientWizardComponent {
  @Output() patientCreated = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  doctorService = inject(DoctorService);
  fb = inject(FormBuilder);

  currentStep = 1;
  totalSteps = 3;
  errorMessage: string | null = null;
  isLoading = false;
  isSearching = false;

  selectedPatient: PatientSearchDto | null = null;
  searchResults: PatientSearchDto[] = [];
  searchTerm = '';
  private searchSubject = new Subject<string>();

  wizardForm: FormGroup;

  constructor() {
    this.wizardForm = this.fb.group({
      // Step 1: Patient Selection
      patientId: [null, Validators.required],
      searchTerm: [''],
      
      // Step 2: Personal Details
      dateOfBirth: [''],
      gender: [''],
      phoneNumber: ['', [Validators.pattern(/^[+]?[(]?[0-9]{1,4}[)]?[-\s.]?[(]?[0-9]{1,4}[)]?[-\s.]?[0-9]{1,9}$/)]],
      
      // Step 3: Medical Information
      address: ['', [Validators.maxLength(200)]],
      bloodType: [''],
      medicalRecordId: ['', [Validators.maxLength(50)]]
    });

    // Setup search with debounce
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(searchTerm => {
        this.isSearching = true;
        const term = searchTerm && searchTerm.trim().length >= 2 ? searchTerm.trim() : undefined;
        return this.doctorService.searchPatients(term);
      })
    ).subscribe({
      next: (results) => {
        this.searchResults = results;
        this.isSearching = false;
      },
      error: (err) => {
        console.error('Search error:', err);
        this.isSearching = false;
        this.searchResults = [];
      }
    });
  }

  get step1Form() {
    return {
      patientId: this.wizardForm.get('patientId'),
      searchTerm: this.wizardForm.get('searchTerm')
    };
  }

  get step2Form() {
    return {
      dateOfBirth: this.wizardForm.get('dateOfBirth'),
      gender: this.wizardForm.get('gender'),
      phoneNumber: this.wizardForm.get('phoneNumber')
    };
  }

  get step3Form() {
    return {
      address: this.wizardForm.get('address'),
      bloodType: this.wizardForm.get('bloodType'),
      medicalRecordId: this.wizardForm.get('medicalRecordId')
    };
  }

  onSearchChange() {
    const searchTerm = this.wizardForm.get('searchTerm')?.value || '';
    this.searchTerm = searchTerm;
    if (searchTerm.trim().length >= 2) {
      this.searchSubject.next(searchTerm);
    } else {
      this.searchResults = [];
      this.isSearching = false;
    }
  }

  selectPatient(patient: PatientSearchDto) {
    this.selectedPatient = patient;
    this.wizardForm.patchValue({ patientId: patient.id });
    this.searchResults = [];
    this.searchTerm = '';
    this.wizardForm.get('searchTerm')?.setValue('');
  }

  clearSelection() {
    this.selectedPatient = null;
    this.wizardForm.patchValue({ patientId: null });
  }

  isStepValid(step: number): boolean {
    switch(step) {
      case 1:
        return !!this.wizardForm.get('patientId')?.value;
      case 2:
        return true; // Step 2 fields are optional
      case 3:
        return true; // Step 3 fields are optional
      default:
        return false;
    }
  }

  nextStep() {
    if (this.isStepValid(this.currentStep)) {
      if (this.currentStep < this.totalSteps) {
        this.currentStep++;
        this.errorMessage = null;
      }
    } else {
      this.markStepFieldsAsTouched(this.currentStep);
    }
  }

  previousStep() {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.errorMessage = null;
    }
  }

  markStepFieldsAsTouched(step: number) {
    switch(step) {
      case 1:
        this.wizardForm.get('patientId')?.markAsTouched();
        break;
      case 2:
        Object.values(this.step2Form).forEach(control => control?.markAsTouched());
        break;
      case 3:
        Object.values(this.step3Form).forEach(control => control?.markAsTouched());
        break;
    }
  }

  onSubmit() {
    if (this.wizardForm.valid && this.selectedPatient) {
      this.isLoading = true;
      this.errorMessage = null;

      const formValue = this.wizardForm.value;
      const assignData: AssignPatientDto = {
        patientId: this.selectedPatient.id,
        dateOfBirth: formValue.dateOfBirth || undefined,
        gender: formValue.gender || undefined,
        phoneNumber: formValue.phoneNumber || undefined,
        address: formValue.address || undefined,
        bloodType: formValue.bloodType || undefined,
        medicalRecordId: formValue.medicalRecordId || undefined
      };

      this.doctorService.assignPatient(assignData).subscribe({
        next: () => {
          this.isLoading = false;
          this.patientCreated.emit();
        },
        error: (err) => {
          this.isLoading = false;
          if (err.error?.message) {
            this.errorMessage = err.error.message;
          } else if (err.error?.errors) {
            const errors = Object.values(err.error.errors).flat();
            this.errorMessage = errors.join(', ');
          } else {
            this.errorMessage = 'An error occurred while assigning the patient.';
          }
        }
      });
    } else {
      this.markStepFieldsAsTouched(this.currentStep);
    }
  }

  onCancel() {
    this.cancel.emit();
  }

  getProgressPercentage(): number {
    return (this.currentStep / this.totalSteps) * 100;
  }
}
