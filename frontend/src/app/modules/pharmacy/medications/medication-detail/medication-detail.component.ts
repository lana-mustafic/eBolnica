import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { EMPTY, catchError, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto } from '../../../../api-services/pharmacy/pharmacy-api.models';
import { getDosageFormLabel } from '../../constants/medication-dosage-forms.constant';
import { getMedicationCategoryLabel } from '../../constants/medication-categories.constant';

@Component({
  selector: 'app-medication-detail',
  standalone: false,
  templateUrl: './medication-detail.component.html',
  styleUrl: './medication-detail.component.scss',
})
export class MedicationDetailComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  medication: MedicationDto | null = null;
  isLoading = true;
  loadError = false;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = Number(params.get('id'));
          if (!Number.isFinite(id) || id <= 0) {
            this.loadError = true;
            this.isLoading = false;
            this.medication = null;
            return EMPTY;
          }

          this.isLoading = true;
          this.loadError = false;
          this.medication = null;

          return this.pharmacyApi.getMedicationById(id).pipe(
            catchError(() => {
              this.loadError = true;
              this.isLoading = false;
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((medication) => {
        this.medication = medication;
        this.isLoading = false;
      });
  }

  reload(): void {
    const id = this.medication?.id ?? Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id <= 0) {
      return;
    }

    this.isLoading = true;
    this.loadError = false;
    this.pharmacyApi
      .getMedicationById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (medication) => {
          this.medication = medication;
          this.isLoading = false;
        },
        error: () => {
          this.loadError = true;
          this.isLoading = false;
        },
      });
  }

  edit(): void {
    if (this.medication) {
      this.router.navigate(['/pharmacy/medications', this.medication.id, 'edit']);
    }
  }

  get dosageFormLabel(): string {
    return getDosageFormLabel(this.medication?.dosageForm);
  }

  get categoryLabel(): string {
    return getMedicationCategoryLabel(this.medication?.category);
  }

  back(): void {
    this.router.navigate(['/pharmacy/medications']);
  }
}
