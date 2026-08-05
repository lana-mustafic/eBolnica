import { Component, inject, OnInit } from '@angular/core';
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

  medication: MedicationDto | null = null;
  isLoading = true;
  loadError = false;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.loadError = true;
      this.isLoading = false;
      return;
    }

    this.pharmacyApi.getMedicationById(id).subscribe({
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
