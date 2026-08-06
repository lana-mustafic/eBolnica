import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import {
  MedicationDto,
  PrescriptionDto,
  PrescriptionItemDto,
} from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';

@Component({
  selector: 'app-prescription-detail',
  standalone: false,
  templateUrl: './prescription-detail.component.html',
  styleUrl: './prescription-detail.component.scss',
})
export class PrescriptionDetailComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);

  prescription: PrescriptionDto | null = null;
  medications: MedicationDto[] = [];
  isLoading = false;
  loadError = false;
  isDispensing = false;
  prescriptionId: number | null = null;

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = Number(params.get('id'));
      if (!id) {
        this.loadError = true;
        this.isLoading = false;
        this.prescription = null;
        return;
      }

      this.prescriptionId = id;
      this.loadData();
    });
  }

  loadData(): void {
    if (!this.prescriptionId) return;

    this.isLoading = true;
    this.loadError = false;
    this.pharmacyApi
      .getPrescriptionById(this.prescriptionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prescription) => {
          this.prescription = prescription;
          this.loadMedicationStock(prescription.prescriptionItems);
        },
        error: () => {
          this.loadError = true;
          this.prescription = null;
          this.toaster.error('Greška pri učitavanju recepta.');
          this.isLoading = false;
        },
      });
  }

  private loadMedicationStock(items: PrescriptionItemDto[]): void {
    const ids = [...new Set(items.map((i) => i.medicationId))];
    if (ids.length === 0) {
      this.isLoading = false;
      return;
    }

    forkJoin(
      ids.map((id) =>
        this.pharmacyApi.getMedicationById(id).pipe(catchError(() => of(null as MedicationDto | null)))
      )
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (meds) => {
          this.medications = meds.filter((med): med is MedicationDto => med != null);
          this.isLoading = false;
        },
        error: () => {
          this.toaster.error('Greška pri učitavanju zaliha lijekova.');
          this.isLoading = false;
        },
      });
  }

  canDispense(): boolean {
    return this.prescription?.status === 'Pending' && !this.isDispensing;
  }

  stockStatus(medicationId: number, required: number): { label: string; css: string } {
    const med = this.medications.find((m) => m.id === medicationId);
    if (!med) return { label: 'Nije pronađen', css: 'stock-missing' };
    if (med.stockQuantity === 0) return { label: 'Nema na stanju', css: 'stock-out' };
    if (med.stockQuantity < required) return { label: 'Nedovoljno', css: 'stock-low' };
    if (med.stockQuantity < med.minimumStockLevel) return { label: 'Ispod minimuma', css: 'stock-warn' };
    return { label: 'Na stanju', css: 'stock-ok' };
  }

  dispense(): void {
    if (!this.prescriptionId || !this.canDispense()) return;

    const validation = this.validateStock();
    if (!validation.ok) {
      this.toaster.error(validation.message);
      return;
    }

    if (!confirm('Potvrdite izdavanje recepta i smanjenje zaliha.')) return;

    this.isDispensing = true;
    this.pharmacyApi
      .dispensePrescription(this.prescriptionId, { dispensedDate: new Date().toISOString() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.prescription = updated;
          this.isDispensing = false;
          this.toaster.success('Recept uspješno izdan.');
          this.loadMedicationStock(updated.prescriptionItems);
        },
        error: (err) => {
          this.isDispensing = false;
          const msg = err?.error?.message ?? err?.error?.title ?? 'Greška pri izdavanju recepta.';
          this.toaster.error(msg);
        },
      });
  }

  private validateStock(): { ok: boolean; message: string } {
    if (!this.prescription) return { ok: false, message: 'Recept nije učitan.' };

    for (const item of this.prescription.prescriptionItems) {
      const med = this.medications.find((m) => m.id === item.medicationId);
      if (!med || med.stockQuantity < item.quantity) {
        return {
          ok: false,
          message: `Nedovoljna zaliha za ${item.medicationName}.`,
        };
      }
    }

    return { ok: true, message: '' };
  }

  back(): void {
    this.router.navigate(['/pharmacy/prescriptions']);
  }
}
