import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto } from '../../../../api-services/pharmacy/pharmacy-api.models';

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
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = Number(params.get('id'));
      if (!id) {
        this.loadError = true;
        this.isLoading = false;
        this.medication = null;
        return;
      }

      this.loadMedication(id);
    });
  }

  private loadMedication(id: number): void {
    this.isLoading = true;
    this.loadError = false;
    this.medication = null;

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

  back(): void {
    this.router.navigate(['/pharmacy/medications']);
  }
}
