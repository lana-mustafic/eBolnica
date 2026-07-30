import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { MedicationDto } from '../../../models/medication.dto';
import { MedicationCreateDto } from '../../../models/medication-create.dto';
import {
  medicationNameAsyncValidator
} from '../../../shared/validators/medication-name-async.validator';
import {
  getMedicationFieldErrorMessage,
  isMedicationFieldInvalidForDisplay,
  isMedicationNameCheckUnavailable
} from '../../../shared/utils/medication-field-error.util';
import { dateInputToIsoString } from '../../../shared/utils/date-only.util';

@Component({
  selector: 'app-medication-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './medication-form.component.html',
  styleUrl: './medication-form.component.css'
})
export class MedicationFormComponent implements OnInit {
  private formBuilder = inject(FormBuilder);
  private pharmacyService = inject(PharmacyService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  medicationForm: FormGroup;
  isEditMode: boolean = false;
  medicationId: number | null = null;
  isLoading: boolean = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

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
    this.medicationForm = this.formBuilder.group({
      name: [
        '',
        [Validators.required, Validators.minLength(3)],
        [
          medicationNameAsyncValidator(
            (name, excludeId) => this.pharmacyService.checkMedicationNameAvailability(name, excludeId),
            { excludeId: () => this.medicationId ?? undefined }
          )
        ]
      ],
      genericName: [''],
      description: [''],
      manufacturer: [''],
      price: [0, [Validators.required, Validators.min(0.01)]],
      stockQuantity: [0, [Validators.required, Validators.min(0)]],
      minimumStockLevel: [10, [Validators.required, Validators.min(0)]],
      expiryDate: ['', [Validators.required, (control: AbstractControl) => this.expiryDateValidator(control)]],
      batchNumber: [''],
      isActive: [true],
      requiresPrescription: [true],
      category: ['', Validators.required],
      dosageForm: [''],
      strength: ['']
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    
    if (id) {
      this.isEditMode = true;
      this.medicationId = +id;
      this.loadMedicationForEdit(+id);
    }
  }

  expiryDateValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value || this.isEditMode) {
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

  loadMedicationForEdit(id: number): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.pharmacyService.getMedicationById(id).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (medication) => {
        // Format expiry date for input
        const expiryDate = medication.expiryDate 
          ? new Date(medication.expiryDate).toISOString().split('T')[0]
          : '';

        this.medicationForm.patchValue({
          name: medication.name,
          genericName: medication.genericName || '',
          description: medication.description || '',
          manufacturer: medication.manufacturer || '',
          price: medication.price,
          stockQuantity: medication.stockQuantity,
          minimumStockLevel: medication.minimumStockLevel,
          expiryDate: expiryDate,
          batchNumber: medication.batchNumber || '',
          isActive: medication.isActive,
          requiresPrescription: medication.requiresPrescription,
          category: medication.category || '',
          dosageForm: medication.dosageForm || '',
          strength: medication.strength || ''
        });

        // Re-run async name check with excludeId so unchanged name is not flagged as duplicate
        this.medicationForm.get('name')?.updateValueAndValidity();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load medication. Please try again.';
        console.error('Error loading medication:', error);
      }
    });
  }

  onSubmit(): void {
    if (this.medicationForm.valid) {
      this.isLoading = true;
      this.errorMessage = null;
      this.successMessage = null;

      const formValue = this.medicationForm.value;
      
      // Format expiry date
      const expiryDate = formValue.expiryDate
        ? dateInputToIsoString(formValue.expiryDate)
        : '';

      const medicationDto: MedicationCreateDto = {
        name: formValue.name,
        genericName: formValue.genericName || undefined,
        description: formValue.description || undefined,
        manufacturer: formValue.manufacturer || undefined,
        price: formValue.price,
        stockQuantity: formValue.stockQuantity,
        minimumStockLevel: formValue.minimumStockLevel,
        expiryDate: expiryDate,
        batchNumber: formValue.batchNumber || undefined,
        isActive: formValue.isActive,
        requiresPrescription: formValue.requiresPrescription,
        category: formValue.category,
        dosageForm: formValue.dosageForm || undefined,
        strength: formValue.strength || undefined
      };

      if (this.isEditMode && this.medicationId) {
        // Update existing medication
        this.pharmacyService.updateMedication(this.medicationId, medicationDto).pipe(
          finalize(() => this.isLoading = false)
        ).subscribe({
          next: () => {
            this.successMessage = 'Medication updated successfully.';
            setTimeout(() => {
              this.router.navigate(['/pharmacy/medications']);
            }, 1500);
          },
          error: (error) => {
            if (error.error?.message) {
              this.errorMessage = error.error.message;
            } else if (error.error?.errors) {
              const errors = Object.values(error.error.errors).flat();
              this.errorMessage = errors.join(', ');
            } else {
              this.errorMessage = 'Failed to update medication. Please try again.';
            }
            console.error('Error updating medication:', error);
          }
        });
      } else {
        // Create new medication
        this.pharmacyService.createMedication(medicationDto).pipe(
          finalize(() => this.isLoading = false)
        ).subscribe({
          next: () => {
            this.successMessage = 'Medication created successfully.';
            setTimeout(() => {
              this.router.navigate(['/pharmacy/medications']);
            }, 1500);
          },
          error: (error) => {
            if (error.error?.message) {
              this.errorMessage = error.error.message;
            } else if (error.error?.errors) {
              const errors = Object.values(error.error.errors).flat();
              this.errorMessage = errors.join(', ');
            } else {
              this.errorMessage = 'Failed to create medication. Please try again.';
            }
            console.error('Error creating medication:', error);
          }
        });
      }
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.medicationForm.controls).forEach(key => {
        this.medicationForm.get(key)?.markAsTouched();
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/pharmacy/medications']);
  }

  getFieldError(fieldName: string): string {
    return getMedicationFieldErrorMessage(
      this.medicationForm.get(fieldName),
      this.getFieldLabel(fieldName)
    ) ?? '';
  }

  getNameFieldError(): string {
    return getMedicationFieldErrorMessage(
      this.medicationForm.get('name'),
      this.getFieldLabel('name')
    ) ?? '';
  }

  retryNameValidation(): void {
    const nameControl = this.medicationForm.get('name');
    nameControl?.markAsTouched();
    nameControl?.updateValueAndValidity();
  }

  isNameCheckUnavailable(): boolean {
    return isMedicationNameCheckUnavailable(this.medicationForm.get('name'));
  }

  isNameControlPending(): boolean {
    return this.medicationForm.get('name')?.pending ?? false;
  }

  isNameFieldInvalid(): boolean {
    return isMedicationFieldInvalidForDisplay(this.medicationForm.get('name'));
  }

  getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      name: 'Name',
      price: 'Price',
      stockQuantity: 'Stock Quantity',
      minimumStockLevel: 'Minimum Stock Level',
      expiryDate: 'Expiry Date',
      category: 'Category'
    };
    return labels[fieldName] || fieldName;
  }
}
