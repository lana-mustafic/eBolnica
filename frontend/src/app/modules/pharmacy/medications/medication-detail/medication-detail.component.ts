import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY, catchError, map, of, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto, MedicationStockHistoryDto } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthFacadeService } from '../../../../core/services/auth/auth-facade.service';
import { DialogButton, DialogType } from '../../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../../shared/services/dialog-helper.service';
import { getDosageFormLabel } from '../../constants/medication-dosage-forms.constant';
import { getMedicationCategoryLabel } from '../../constants/medication-categories.constant';
import { MedicationImageUrlService } from '../../services/medication-image-url.service';
import { KpiCardTone } from '../../shared/pharmacy-kpi-card/pharmacy-kpi-card.component';

interface StockHistoryRow {
  id: number;
  date: string;
  change: string;
  stock: number;
  note?: string;
}

@Component({
  selector: 'app-medication-detail',
  standalone: false,
  templateUrl: './medication-detail.component.html',
  styleUrl: './medication-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MedicationDetailComponent implements OnInit, OnDestroy {
  private pharmacyApi = inject(PharmacyApiService);
  private imageUrlService = inject(MedicationImageUrlService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private dialog = inject(DialogHelperService);
  private destroyRef = inject(DestroyRef);
  auth = inject(AuthFacadeService);

  medication = signal<MedicationDto | null>(null);
  isLoading = signal(true);
  loadError = signal(false);
  imageUrl = signal<string | null>(null);
  stockHistory = signal<StockHistoryRow[]>([]);
  isLoadingHistory = signal(false);

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = Number(params.get('id'));
          if (!Number.isFinite(id) || id <= 0) {
            this.loadError.set(true);
            this.isLoading.set(false);
            this.medication.set(null);
            this.stockHistory.set([]);
            return EMPTY;
          }

          this.isLoading.set(true);
          this.isLoadingHistory.set(true);
          this.loadError.set(false);
          this.clearImageUrl(this.medication());
          this.medication.set(null);
          this.stockHistory.set([]);

          return this.pharmacyApi.getMedicationById(id).pipe(
            switchMap((medication) =>
              this.pharmacyApi.getMedicationStockHistory(id).pipe(
                map((history) => ({ medication, history })),
                catchError(() => of({ medication, history: [] as MedicationStockHistoryDto[] }))
              )
            ),
            catchError(() => {
              this.loadError.set(true);
              this.isLoading.set(false);
              this.isLoadingHistory.set(false);
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(({ medication, history }) => {
        this.medication.set(medication);
        this.stockHistory.set(history.map((row) => this.mapStockHistoryRow(row)));
        this.isLoading.set(false);
        this.isLoadingHistory.set(false);
        this.loadImageUrl(medication);
      });
  }

  ngOnDestroy(): void {
    this.clearImageUrl(this.medication());
  }

  reload(): void {
    const id = this.medication()?.id ?? Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id <= 0) {
      return;
    }

    this.isLoading.set(true);
    this.isLoadingHistory.set(true);
    this.loadError.set(false);
    const previous = this.medication();
    this.clearImageUrl(previous);

    this.pharmacyApi
      .getMedicationById(id)
      .pipe(
        switchMap((medication) =>
          this.pharmacyApi.getMedicationStockHistory(id).pipe(
            map((history) => ({ medication, history })),
            catchError(() => of({ medication, history: [] as MedicationStockHistoryDto[] }))
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ medication, history }) => {
          this.medication.set(medication);
          this.stockHistory.set(history.map((row) => this.mapStockHistoryRow(row)));
          this.isLoading.set(false);
          this.isLoadingHistory.set(false);
          this.loadImageUrl(medication);
        },
        error: () => {
          this.loadError.set(true);
          this.isLoading.set(false);
          this.isLoadingHistory.set(false);
        },
      });
  }

  edit(): void {
    const med = this.medication();
    if (med) {
      this.router.navigate(['/pharmacy/medications', med.id, 'edit']);
    }
  }

  addStock(): void {
    this.edit();
  }

  deleteMedication(): void {
    const med = this.medication();
    if (!med) {
      return;
    }

    this.dialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Deaktiviraj lijek',
        message: `Jeste li sigurni da želite deaktivirati lijek "${med.name}"?`,
        buttons: [
          { type: DialogButton.CANCEL, label: 'Odustani' },
          { type: DialogButton.DELETE, label: 'Deaktiviraj', color: 'warn' },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.DELETE) {
          return;
        }

        const current = this.medication();
        if (!current) {
          return;
        }

        this.pharmacyApi
          .deleteMedication(current.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.toaster.success('Lijek deaktiviran.');
              this.router.navigate(['/pharmacy/medications']);
            },
            error: (err) => {
              this.toaster.error(getApiErrorMessage(err, 'Greška pri deaktivaciji lijeka.'));
            },
          });
      });
  }

  print(): void {
    window.print();
  }

  onImageError(): void {
    this.imageUrl.set(null);
  }

  get dosageFormLabel(): string {
    return getDosageFormLabel(this.medication()?.dosageForm) || '—';
  }

  get categoryLabel(): string {
    return getMedicationCategoryLabel(this.medication()?.category);
  }

  get medicationCode(): string {
    const med = this.medication();
    if (!med) {
      return '—';
    }
    return `MED-${String(med.id).padStart(3, '0')}`;
  }

  get expiryKpiValue(): string {
    const med = this.medication();
    if (!med?.expiryDate) {
      return '—';
    }

    return new Date(med.expiryDate).toLocaleDateString('bs-BA');
  }

  get expiryKpiTone(): KpiCardTone {
    const hintClass = this.expiryHintClass;
    if (hintClass === 'danger') {
      return 'red';
    }
    if (hintClass === 'warn') {
      return 'orange';
    }
    return 'green';
  }

  get expiryHint(): string {
    const med = this.medication();
    if (!med?.expiryDate) {
      return 'Rok nije definisan';
    }

    const expiry = new Date(med.expiryDate);
    const now = new Date();
    now.setHours(0, 0, 0, 0);

    if (expiry < now) {
      return 'Rok trajanja je istekao';
    }

    const daysLeft = Math.ceil((expiry.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    if (daysLeft <= 30) {
      return `Ističe za ${daysLeft} dana`;
    }

    const yearsLeft = Math.floor(daysLeft / 365);
    if (yearsLeft >= 1) {
      const label = yearsLeft === 1 ? 'godinu' : yearsLeft < 5 ? 'godine' : 'godina';
      return `Važi još ${yearsLeft} ${label}`;
    }

    const monthsLeft = Math.max(1, Math.floor(daysLeft / 30));
    const monthLabel = monthsLeft === 1 ? 'mjesec' : monthsLeft < 5 ? 'mjeseca' : 'mjeseci';
    return `Važi još ${monthsLeft} ${monthLabel}`;
  }

  get expiryHintClass(): string {
    const med = this.medication();
    if (!med?.expiryDate) {
      return '';
    }

    const expiry = new Date(med.expiryDate);
    const now = new Date();
    now.setHours(0, 0, 0, 0);

    if (expiry < now) {
      return 'danger';
    }

    const horizon = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);
    if (expiry <= horizon) {
      return 'warn';
    }

    return 'ok';
  }

  back(): void {
    this.router.navigate(['/pharmacy/medications']);
  }

  formatStockChange(changeQuantity: number): string {
    if (changeQuantity > 0) {
      return `+${changeQuantity}`;
    }

    return String(changeQuantity);
  }

  private mapStockHistoryRow(row: MedicationStockHistoryDto): StockHistoryRow {
    const note = this.stockHistoryNote(row);
    return {
      id: row.id,
      date: new Date(row.occurredAt).toLocaleString('bs-BA', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }),
      change: this.formatStockChange(row.changeQuantity),
      stock: row.stockAfter,
      note,
    };
  }

  private stockHistoryNote(row: MedicationStockHistoryDto): string | undefined {
    switch (row.reason) {
      case 'InitialStock':
        return 'Početna zaliha';
      case 'ManualAdjustment':
        return 'Ručna izmjena';
      case 'PrescriptionDispensed':
        return row.referenceLabel ? `Recept ${row.referenceLabel}` : 'Izdavanje recepta';
      case 'Import':
        return 'CSV import';
      default:
        return row.referenceLabel ?? undefined;
    }
  }

  private loadImageUrl(medication: MedicationDto): void {
    if (medication.primaryImageId) {
      this.imageUrlService
        .getAuthenticatedUrl(medication.id, medication.primaryImageId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (url) => {
            this.imageUrl.set(url);
          },
          error: () => {
            if (medication.primaryImageUrl) {
              this.imageUrl.set(this.imageUrlService.getLegacyUrl(medication.primaryImageUrl));
            }
          },
        });
      return;
    }

    if (medication.primaryImageUrl) {
      this.imageUrl.set(this.imageUrlService.getLegacyUrl(medication.primaryImageUrl));
    }
  }

  private clearImageUrl(medication?: MedicationDto | null): void {
    const med = medication ?? this.medication();
    if (med?.primaryImageId) {
      this.imageUrlService.revoke(med.id, med.primaryImageId);
    }
    this.imageUrl.set(null);
  }
}
