import { Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { map } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationUpsertCommand } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { medicationNameAsyncValidator } from '../../../shared/validators/medication-name-async.validator';

@Component({
  selector: 'app-medication-wizard',
  standalone: false,
  templateUrl: './medication-wizard.component.html',
  styleUrl: './medication-wizard.component.scss',
})
export class MedicationWizardComponent {
  private fb = inject(FormBuilder);
  private pharmacyApi = inject(PharmacyApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);

  step = 1;
  isSaving = false;

  categories = ['Analgesics', 'Antibiotics', 'Cardiovascular', 'Diabetes', 'Other'];
  dosageForms = ['Tablet', 'Capsule', 'Liquid', 'Injection'];

  form = this.fb.group({
    name: [
      '',
      [Validators.required, Validators.minLength(3)],
      [
        medicationNameAsyncValidator((name, excludeId) =>
          this.pharmacyApi.checkName(name, excludeId).pipe(map((res) => ({ isAvailable: res.isAvailable })))
        ),
      ],
    ],
    genericName: [''],
    category: ['', Validators.required],
    manufacturer: [''],
    description: [''],
    price: [0, [Validators.required, Validators.min(0.01)]],
    stockQuantity: [0, [Validators.required, Validators.min(0)]],
    minimumStockLevel: [10, [Validators.required, Validators.min(0)]],
    expiryDate: ['', Validators.required],
    batchNumber: [''],
    dosageForm: [''],
    strength: [''],
    requiresPrescription: [true],
    isActive: [true],
  });

  next(): void {
    if (this.step === 1 && !this.isStepValid(['name', 'category', 'genericName', 'manufacturer', 'description'])) return;
    if (this.step === 2 && !this.isStepValid(['price', 'stockQuantity', 'minimumStockLevel', 'expiryDate', 'batchNumber', 'dosageForm', 'strength'])) return;
    this.step++;
  }

  back(): void {
    if (this.step > 1) this.step--;
  }

  get nameControl() {
    return this.form.get('name');
  }

  submit(): void {
    if (this.form.invalid || this.form.pending) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const body: MedicationUpsertCommand = {
      name: raw.name!,
      genericName: raw.genericName || null,
      description: raw.description || null,
      manufacturer: raw.manufacturer || null,
      price: raw.price!,
      stockQuantity: raw.stockQuantity!,
      minimumStockLevel: raw.minimumStockLevel!,
      expiryDate: new Date(raw.expiryDate!).toISOString(),
      batchNumber: raw.batchNumber || null,
      isActive: raw.isActive!,
      requiresPrescription: raw.requiresPrescription!,
      category: raw.category!,
      dosageForm: raw.dosageForm || null,
      strength: raw.strength || null,
    };

    this.isSaving = true;
    this.pharmacyApi.createMedication(body).subscribe({
      next: (created) => {
        this.toaster.success('Lijek kreiran preko wizarda.');
        this.router.navigate(['/pharmacy/medications', created.id, 'edit']);
      },
      error: () => {
        this.isSaving = false;
        this.toaster.error('Greška pri čuvanju.');
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/pharmacy/medications']);
  }

  private isStepValid(fields: string[]): boolean {
    let ok = true;
    for (const f of fields) {
      const c = this.form.get(f);
      if (c?.pending) {
        ok = false;
      }
      if (c?.invalid) {
        c.markAsTouched();
        ok = false;
      }
    }
    return ok;
  }
}
