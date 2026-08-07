import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import {
  PrescriptionDto,
  PrescriptionItemDto,
} from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { getPrescriptionStatusLabel } from '../prescription-status.util';
import { DialogButton, DialogType } from '../../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';

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
  private dialog = inject(DialogHelperService);
  private destroyRef = inject(DestroyRef);

  prescription: PrescriptionDto | null = null;
  isLoading = false;
  loadError = false;
  isDispensing = false;
  isCancelling = false;
  prescriptionId: number | null = null;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = Number(params.get('id'));
          if (!Number.isFinite(id) || id <= 0) {
            this.loadError = true;
            this.isLoading = false;
            this.prescription = null;
            return of(null);
          }

          this.prescriptionId = id;
          this.isLoading = true;
          this.loadError = false;
          this.prescription = null;

          return this.pharmacyApi.getPrescriptionById(id).pipe(
            catchError(() => {
              this.loadError = true;
              this.toaster.error('Greška pri učitavanju recepta.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((prescription) => {
        this.isLoading = false;
        if (!prescription) {
          return;
        }

        this.prescription = prescription;
      });
  }

  reload(): void {
    const id = this.prescriptionId ?? Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id <= 0) {
      return;
    }

    this.prescriptionId = id;
    this.isLoading = true;
    this.loadError = false;
    this.prescription = null;

    this.pharmacyApi
      .getPrescriptionById(id)
      .pipe(
        catchError(() => {
          this.loadError = true;
          this.toaster.error('Greška pri učitavanju recepta.');
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((prescription) => {
        this.isLoading = false;
        if (!prescription) {
          return;
        }

        this.prescription = prescription;
      });
  }

  canDispense(): boolean {
    return this.prescription?.status === 'Pending' && !this.isDispensing;
  }

  canCancel(): boolean {
    return this.prescription?.status === 'Pending' && !this.isCancelling;
  }

  statusLabel(status: string): string {
    return getPrescriptionStatusLabel(status);
  }

  cancel(): void {
    if (!this.prescriptionId || !this.canCancel()) {
      return;
    }

    this.dialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Otkaži recept',
        message: `Jeste li sigurni da želite otkazati recept ${this.prescription?.prescriptionNumber}?`,
        buttons: [
          { type: DialogButton.CANCEL, label: 'Odustani' },
          { type: DialogButton.DELETE, label: 'Otkaži', color: 'warn' },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.DELETE || !this.prescriptionId) {
          return;
        }

        this.isCancelling = true;
        this.pharmacyApi
          .cancelPrescription(this.prescriptionId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (updated) => {
              this.prescription = updated;
              this.isCancelling = false;
              this.toaster.success('Recept je otkazan.');
            },
            error: (err) => {
              this.isCancelling = false;
              this.toaster.error(getApiErrorMessage(err, 'Greška pri otkazivanju recepta.'));
            },
          });
      });
  }

  stockStatus(item: PrescriptionItemDto): { label: string; css: string } {
    if (item.stockQuantity == null) return { label: 'Nije pronađen', css: 'stock-missing' };
    if (item.stockQuantity === 0) return { label: 'Nema na stanju', css: 'stock-out' };
    if (item.stockQuantity < item.quantity) return { label: 'Nedovoljno', css: 'stock-low' };
    if (item.minimumStockLevel != null && item.stockQuantity < item.minimumStockLevel) {
      return { label: 'Ispod minimuma', css: 'stock-warn' };
    }
    return { label: 'Na stanju', css: 'stock-ok' };
  }

  dispense(): void {
    if (!this.prescriptionId || !this.canDispense()) return;

    const validation = this.validateStock();
    if (!validation.ok) {
      this.toaster.error(validation.message);
      return;
    }

    this.dialog
      .showCustom({
        type: DialogType.QUESTION,
        title: 'Izdaj recept',
        message: 'Potvrdite izdavanje recepta i smanjenje zaliha.',
        buttons: [
          { type: DialogButton.CANCEL, label: 'Odustani' },
          { type: DialogButton.OK, label: 'Izdaj', color: 'primary' },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.OK || !this.prescriptionId) {
          return;
        }

        this.isDispensing = true;
        this.pharmacyApi
          .dispensePrescription(this.prescriptionId, { dispensedDate: new Date().toISOString() })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (updated) => {
              this.prescription = updated;
              this.isDispensing = false;
              this.toaster.success('Recept uspješno izdan.');
              this.reload();
            },
            error: (err) => {
              this.isDispensing = false;
              this.toaster.error(getApiErrorMessage(err, 'Greška pri izdavanju recepta.'));
            },
          });
      });
  }

  private validateStock(): { ok: boolean; message: string } {
    if (!this.prescription) return { ok: false, message: 'Recept nije učitan.' };

    for (const item of this.prescription.prescriptionItems) {
      if (item.stockQuantity == null || item.stockQuantity < item.quantity) {
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
