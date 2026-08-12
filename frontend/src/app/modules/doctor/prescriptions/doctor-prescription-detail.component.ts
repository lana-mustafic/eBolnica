import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { DoctorApiService } from '../../../api-services/doctor/doctor-api.service';
import { PrescriptionDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import {
  getPrescriptionStatusClass,
  getPrescriptionStatusLabel,
} from '../../pharmacy/prescriptions/prescription-status.util';

@Component({
  selector: 'app-doctor-prescription-detail',
  standalone: false,
  templateUrl: './doctor-prescription-detail.component.html',
  styleUrl: './doctor-prescription-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorPrescriptionDetailComponent implements OnInit {
  private doctorApi = inject(DoctorApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);

  isLoading = signal(true);
  prescription = signal<PrescriptionDto | null>(null);

  readonly statusLabel = getPrescriptionStatusLabel;
  readonly statusClass = getPrescriptionStatusClass;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.doctorApi
      .getPrescription(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prescription) => {
          this.prescription.set(prescription);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.isLoading.set(false);
          this.toaster.error(getApiErrorMessage(err, 'Recept nije moguće učitati.'));
        },
      });
  }

  back(): void {
    void this.router.navigate(['/doctor/prescriptions']);
  }
}
