import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { ConfirmDialogService } from '../../../shared/services/confirm-dialog.service';
import { MedicationCreateDto } from '../../../models/medication-create.dto';
import {
  medicationNameAsyncValidator,
  MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR,
  MEDICATION_NAME_EXISTS_ERROR,
  MEDICATION_NAME_VALIDATOR_MESSAGES
} from '../../../shared/validators/medication-name-async.validator';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-medication-wizard',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule
  ],
  templateUrl: './medication-wizard.component.html',
  styleUrl: './medication-wizard.component.css'
})
export class MedicationWizardComponent {
  private formBuilder = inject(FormBuilder);
  private pharmacyService = inject(PharmacyService);
  private router = inject(Router);
  private confirmDialog = inject(ConfirmDialogService);

  wizardForm: FormGroup;
  currentStep: number = 1;
  totalSteps: number = 3;
  isLoading: boolean = false;
  errorMessage: string | null = null;

  // Common categories and dosage forms
  categories: string[] = [
    'Analgesics',
    'Antibiotics',
    'Antivirals',
    'Antifungals',
    'Antidepressants',
    'Antihistamines',
    'Cardiovascular',
    'Diabetes',
    'Gastrointestinal',
    'Neurological',
    'Respiratory',
    'Vitamins',
    'Painkillers',
    'Other'
  ];

  dosageForms: string[] = [
    'Tablet',
    'Capsule',
    'Liquid',
    'Injection',
    'Cream',
    'Ointment',
    'Drops',
    'Spray',
    'Inhaler',
    'Other'
  ];

  constructor() {
    this.wizardForm = this.formBuilder.group({
      // Step 1: Basic Information
      name: [
        '',
        [Validators.required, Validators.minLength(3), Validators.maxLength(100)],
        [
          medicationNameAsyncValidator(
            (name, excludeId) => this.pharmacyService.checkMedicationNameAvailability(name, excludeId)
          )
        ]
      ],
      category: ['', Validators.required],
      description: ['', Validators.maxLength(500)],

      // Step 2: Details & Stock
      price: [0, [Validators.required, Validators.min(0.01)]],
      stockQuantity: [0, [Validators.required, Validators.min(0)]],
      minimumStockLevel: [10, [Validators.required, Validators.min(0)]],
      dosageForm: [''],
      strength: ['', Validators.maxLength(50)],

      // Step 3: Additional Info
      expiryDate: ['', [Validators.required, this.futureDateValidator]],
      batchNumber: ['', Validators.maxLength(50)],
      requiresPrescription: [true],
      isActive: [true],
      genericName: ['', Validators.maxLength(100)],
      manufacturer: ['', Validators.maxLength(100)]
    });
  }

  // Validators
  futureDateValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) {
      return null;
    }
    
    const selectedDate = new Date(control.value);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    if (selectedDate <= today) {
      return { pastDate: true };
    }
    
    return null;
  }

  // Step validation
  isStepValid(step: number): boolean {
    switch (step) {
      case 1:
        return this.isNameControlReady() &&
               this.wizardForm.get('category')?.valid === true;
      case 2:
        return this.wizardForm.get('price')?.valid === true &&
               this.wizardForm.get('stockQuantity')?.valid === true &&
               this.wizardForm.get('minimumStockLevel')?.valid === true;
      case 3:
        return this.wizardForm.get('expiryDate')?.valid === true;
      default:
        return false;
    }
  }

  // Navigation methods
  nextStep(): void {
    if (this.isStepValid(this.currentStep) && this.currentStep < this.totalSteps) {
      this.currentStep++;
      this.errorMessage = null;
      // Scroll to top
      window.scrollTo({ top: 0, behavior: 'smooth' });
    } else {
      // Mark invalid fields as touched
      this.markStepFieldsAsTouched(this.currentStep);
    }
  }

  previousStep(): void {
    if (this.currentStep > 1) {
      this.currentStep--;
      this.errorMessage = null;
      // Scroll to top
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  goToStep(step: number): void {
    // Can only go to completed steps or next step
    if (step >= 1 && step <= this.totalSteps) {
      let canGoToStep = true;
      
      // Check if all previous steps are valid
      for (let i = 1; i < step; i++) {
        if (!this.isStepValid(i)) {
          canGoToStep = false;
          break;
        }
      }
      
      if (canGoToStep || step <= this.currentStep) {
        this.currentStep = step;
        this.errorMessage = null;
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    }
  }

  markStepFieldsAsTouched(step: number): void {
    switch (step) {
      case 1:
        this.wizardForm.get('name')?.markAsTouched();
        this.wizardForm.get('category')?.markAsTouched();
        break;
      case 2:
        this.wizardForm.get('price')?.markAsTouched();
        this.wizardForm.get('stockQuantity')?.markAsTouched();
        this.wizardForm.get('minimumStockLevel')?.markAsTouched();
        break;
      case 3:
        this.wizardForm.get('expiryDate')?.markAsTouched();
        break;
    }
  }

  // Form submission
  onSubmit(): void {
    if (this.wizardForm.valid) {
      this.isLoading = true;
      this.errorMessage = null;

      const formValue = this.wizardForm.value;
      const medicationData: MedicationCreateDto = {
        name: formValue.name,
        category: formValue.category,
        description: formValue.description || undefined,
        price: formValue.price,
        stockQuantity: formValue.stockQuantity,
        minimumStockLevel: formValue.minimumStockLevel,
        expiryDate: new Date(formValue.expiryDate).toISOString(),
        batchNumber: formValue.batchNumber || undefined,
        requiresPrescription: formValue.requiresPrescription ?? true,
        isActive: formValue.isActive ?? true,
        genericName: formValue.genericName || undefined,
        manufacturer: formValue.manufacturer || undefined,
        dosageForm: formValue.dosageForm || undefined,
        strength: formValue.strength || undefined
      };

      this.pharmacyService.createMedication(medicationData).pipe(
        finalize(() => this.isLoading = false)
      ).subscribe({
        next: () => {
          // Redirect to medications list
          this.router.navigate(['/pharmacy/medications']);
        },
        error: (error) => {
          if (error.error?.errors) {
            // Handle validation errors from backend
            const errors = error.error.errors;
            this.errorMessage = Object.values(errors).flat().join(', ');
          } else if (error.error?.message) {
            this.errorMessage = error.error.message;
          } else {
            this.errorMessage = 'Failed to create medication. Please try again.';
          }
          console.error('Error creating medication:', error);
          
          // Scroll to error message
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }
      });
    } else {
      // Mark all invalid fields as touched
      Object.keys(this.wizardForm.controls).forEach(key => {
        const control = this.wizardForm.get(key);
        if (control && control.invalid) {
          control.markAsTouched();
        }
      });
      
      // Go to first invalid step
      for (let i = 1; i <= this.totalSteps; i++) {
        if (!this.isStepValid(i)) {
          this.currentStep = i;
          this.markStepFieldsAsTouched(i);
          break;
        }
      }
    }
  }

  cancel(): void {
    this.confirmDialog.confirm({
      title: 'Cancel wizard',
      message: 'Are you sure you want to cancel? All entered data will be lost.',
      confirmText: 'Leave',
      cancelText: 'Stay',
      confirmColor: 'warn'
    }).subscribe(confirmed => {
      if (confirmed) {
        this.router.navigate(['/pharmacy/medications']);
      }
    });
  }

  // Helper methods
  getProgressPercentage(): number {
    return (this.currentStep / this.totalSteps) * 100;
  }

  getFieldError(fieldName: string): string | null {
    const field = this.wizardForm.get(fieldName);
    if (!field || !field.errors) {
      return null;
    }

    if (field.errors[MEDICATION_NAME_EXISTS_ERROR]) {
      return MEDICATION_NAME_VALIDATOR_MESSAGES[MEDICATION_NAME_EXISTS_ERROR];
    }

    if (field.errors[MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR]) {
      return MEDICATION_NAME_VALIDATOR_MESSAGES[MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR];
    }

    if (!field.touched) {
      return null;
    }

    if (field.errors['required']) {
      return `${this.getFieldLabel(fieldName)} is required`;
    }
    if (field.errors['minlength']) {
      return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['minlength'].requiredLength} characters`;
    }
    if (field.errors['maxlength']) {
      return `${this.getFieldLabel(fieldName)} must not exceed ${field.errors['maxlength'].requiredLength} characters`;
    }
    if (field.errors['min']) {
      return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['min'].min}`;
    }
    if (field.errors['pastDate']) {
      return 'Expiry date must be in the future';
    }

    return null;
  }

  retryNameValidation(): void {
    const nameControl = this.wizardForm.get('name');
    nameControl?.markAsTouched();
    nameControl?.updateValueAndValidity();
  }

  isNameCheckUnavailable(): boolean {
    const nameControl = this.wizardForm.get('name');
    return nameControl?.hasError(MEDICATION_NAME_CHECK_UNAVAILABLE_ERROR) ?? false;
  }

  isNameControlPending(): boolean {
    return this.wizardForm.get('name')?.pending ?? false;
  }

  private isNameControlReady(): boolean {
    const nameControl = this.wizardForm.get('name');
    return nameControl?.valid === true && !nameControl.pending;
  }

  getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      name: 'Name',
      category: 'Category',
      description: 'Description',
      price: 'Price',
      stockQuantity: 'Stock Quantity',
      minimumStockLevel: 'Minimum Stock Level',
      expiryDate: 'Expiry Date',
      batchNumber: 'Batch Number',
      dosageForm: 'Dosage Form',
      strength: 'Strength',
      genericName: 'Generic Name',
      manufacturer: 'Manufacturer'
    };
    return labels[fieldName] || fieldName;
  }

  isStepCompleted(step: number): boolean {
    return step < this.currentStep || (step === this.currentStep && this.isStepValid(step));
  }

  getStepTitle(step: number): string {
    const titles = ['', 'Basic Information', 'Details & Stock', 'Additional Info'];
    return titles[step] || '';
  }
}
