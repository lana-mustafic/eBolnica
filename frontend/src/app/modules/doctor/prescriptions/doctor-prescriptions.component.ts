import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { DoctorApiService } from '../../../api-services/doctor/doctor-api.service';
import { PrescriptionDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import {
  getPrescriptionStatusClass,
  getPrescriptionStatusLabel,
} from '../../pharmacy/prescriptions/prescription-status.util';

@Component({
  selector: 'app-doctor-prescriptions',
  standalone: false,
  templateUrl: './doctor-prescriptions.component.html',
  styleUrl: './doctor-prescriptions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorPrescriptionsComponent implements OnInit {
  private doctorApi = inject(DoctorApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);

  isLoading = signal(true);
  prescriptions = signal<PrescriptionDto[]>([]);
  totalCount = signal(0);
  pendingCount = signal(0);
  selectedStatus = signal('All');
  search = '';

  readonly statusLabel = getPrescriptionStatusLabel;
  readonly statusClass = getPrescriptionStatusClass;

  readonly statusFilters = [
    { value: 'All', label: 'Svi' },
    { value: 'Pending', label: 'Na čekanju' },
    { value: 'Dispensed', label: 'Izdani' },
    { value: 'Cancelled', label: 'Otkazani' },
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.doctorApi
      .listPrescriptions({
        status: this.selectedStatus() === 'All' ? undefined : this.selectedStatus(),
        search: this.search.trim() || undefined,
        pageNumber: 1,
        pageSize: 20,
        sortBy: 'prescribedDate',
        sortOrder: 'desc',
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.prescriptions.set(res.items);
          this.totalCount.set(res.totalCount);
          this.pendingCount.set(res.summary.pendingPrescriptions);
          this.isLoading.set(false);
        },
        error: (err) => {
          this.isLoading.set(false);
          this.toaster.error(getApiErrorMessage(err, 'Greška pri učitavanju recepata.'));
        },
      });
  }

  applyFilters(): void {
    this.load();
  }

  openDetail(id: number): void {
    void this.router.navigate(['/doctor/prescriptions', id]);
  }
}
