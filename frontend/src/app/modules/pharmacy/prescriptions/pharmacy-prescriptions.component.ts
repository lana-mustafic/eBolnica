import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Subject, catchError, debounceTime, of, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { PrescriptionDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import {
  getPrescriptionStatusClass,
  getPrescriptionStatusLabel,
} from './prescription-status.util';
import { DialogButton, DialogType } from '../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../shared/services/dialog-helper.service';

interface PrescriptionActivityItem {
  type: 'new' | 'success' | 'warning';
  message: string;
  timeLabel: string;
}

@Component({
  selector: 'app-pharmacy-prescriptions',
  standalone: false,
  templateUrl: './pharmacy-prescriptions.component.html',
  styleUrl: './pharmacy-prescriptions.component.scss',
})
export class PharmacyPrescriptionsComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private dialog = inject(DialogHelperService);
  private destroyRef = inject(DestroyRef);

  prescriptions: PrescriptionDto[] = [];
  isLoading = false;
  loadError = false;
  totalCount = 0;
  totalPages = 0;
  currentPage = 1;
  pageSize = 10;

  totalPrescriptions = 0;
  pendingPrescriptions = 0;
  dispensedPrescriptions = 0;
  totalRevenue = 0;

  search = '';
  selectedStatus = 'All';
  doctorFilter = '';
  patientFilter = '';
  selectedDateRange = 'all';

  sortBy = 'prescribedDate';
  sortOrder: 'asc' | 'desc' = 'desc';

  selectedPrescription: PrescriptionDto | null = null;
  dispensingId: number | null = null;

  statusFilters = [
    { value: 'All', label: 'Svi' },
    { value: 'Pending', label: 'Na čekanju' },
    { value: 'Dispensed', label: 'Izdani' },
    { value: 'Cancelled', label: 'Otkazani' },
  ];

  dateRangeFilters = [
    { value: 'all', label: 'Svi datumi' },
    { value: 'today', label: 'Danas' },
    { value: 'week', label: 'Zadnjih 7 dana' },
    { value: 'month', label: 'Zadnjih 30 dana' },
  ];

  displayedColumns = ['number', 'patient', 'doctor', 'status', 'amount', 'actions'];

  private filterChanged$ = new Subject<void>();
  private loadTrigger$ = new Subject<void>();

  ngOnInit(): void {
    this.loadTrigger$
      .pipe(
        switchMap(() => {
          this.isLoading = true;
          this.loadError = false;
          return this.pharmacyApi.listPrescriptions(this.buildRequest()).pipe(
            catchError(() => {
              this.loadError = true;
              this.prescriptions = [];
              this.toaster.error('Greška pri učitavanju recepata.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((res) => {
        this.isLoading = false;
        if (!res) return;

        this.prescriptions = res.items;
        this.totalPrescriptions = res.summary.totalPrescriptions;
        this.pendingPrescriptions = res.summary.pendingPrescriptions;
        this.dispensedPrescriptions = res.summary.dispensedPrescriptions;
        this.totalRevenue = res.summary.totalRevenue;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.currentPage = res.currentPage;
      });

    this.filterChanged$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadTrigger$.next();
      });

    this.loadTrigger$.next();
  }

  get recentActivities(): PrescriptionActivityItem[] {
    const activities: PrescriptionActivityItem[] = [];
    const now = Date.now();
    const dayMs = 24 * 60 * 60 * 1000;

    for (const prescription of [...this.prescriptions].slice(0, 8)) {
      const createdAt = new Date(prescription.createdAt).getTime();
      const isRecent = now - createdAt < 2 * dayMs;

      if (prescription.status === 'Pending' && isRecent) {
        activities.push({
          type: 'new',
          message: `Novi recept ${prescription.prescriptionNumber} — ${prescription.patient.firstName} ${prescription.patient.lastName}`,
          timeLabel: this.formatRelativeTime(prescription.createdAt),
        });
      }

      if (prescription.status === 'Dispensed' && prescription.dispensedDate) {
        activities.push({
          type: 'success',
          message: `Izdan recept ${prescription.prescriptionNumber}`,
          timeLabel: this.formatRelativeTime(prescription.dispensedDate),
        });
      }

      if (prescription.status === 'Pending') {
        const age = now - new Date(prescription.prescribedDate).getTime();
        if (age > dayMs) {
          activities.push({
            type: 'warning',
            message: `Recept ${prescription.prescriptionNumber} na čekanju > 24h`,
            timeLabel: this.formatRelativeTime(prescription.prescribedDate),
          });
        }
      }
    }

    if (activities.length === 0) {
      activities.push({
        type: 'success',
        message: 'Nema novih aktivnosti na trenutnoj stranici.',
        timeLabel: 'sada',
      });
    }

    return activities.slice(0, 6);
  }

  hasActiveFilters(): boolean {
    return !!(
      this.search.trim() ||
      this.doctorFilter.trim() ||
      this.patientFilter.trim() ||
      (this.selectedStatus && this.selectedStatus !== 'All') ||
      this.selectedDateRange !== 'all'
    );
  }

  statusLabel(status: string): string {
    return getPrescriptionStatusLabel(status);
  }

  statusClass(status: string): string {
    return getPrescriptionStatusClass(status);
  }

  private buildRequest() {
    const dateRange = this.resolvePrescribedDateRange();

    return {
      status: this.selectedStatus === 'All' ? undefined : this.selectedStatus,
      search: this.search.trim() || undefined,
      patientSearch: this.patientFilter.trim() || undefined,
      doctorSearch: this.doctorFilter.trim() || undefined,
      prescribedFrom: dateRange.prescribedFrom,
      prescribedTo: dateRange.prescribedTo,
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
    };
  }

  private resolvePrescribedDateRange(): { prescribedFrom?: string; prescribedTo?: string } {
    if (this.selectedDateRange === 'all') {
      return {};
    }

    const now = new Date();
    const start = new Date(now);

    if (this.selectedDateRange === 'today') {
      start.setHours(0, 0, 0, 0);
    } else if (this.selectedDateRange === 'week') {
      start.setDate(start.getDate() - 7);
      start.setHours(0, 0, 0, 0);
    } else if (this.selectedDateRange === 'month') {
      start.setDate(start.getDate() - 30);
      start.setHours(0, 0, 0, 0);
    }

    return {
      prescribedFrom: start.toISOString(),
      prescribedTo: now.toISOString(),
    };
  }

  onSearchInput(): void {
    this.filterChanged$.next();
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadTrigger$.next();
  }

  reload(): void {
    this.loadTrigger$.next();
  }

  clearFilters(): void {
    this.search = '';
    this.doctorFilter = '';
    this.patientFilter = '';
    this.selectedStatus = 'All';
    this.selectedDateRange = 'all';
    this.currentPage = 1;
    this.loadTrigger$.next();
  }

  retryLoad(): void {
    this.loadTrigger$.next();
  }

  onSort(column: string): void {
    if (this.sortBy === column) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortOrder = 'asc';
    }
    this.currentPage = 1;
    this.loadTrigger$.next();
  }

  sortIndicator(column: string): string {
    if (this.sortBy !== column) return '';
    return this.sortOrder === 'asc' ? ' ▲' : ' ▼';
  }

  sortAriaSort(column: string): 'ascending' | 'descending' | 'none' {
    if (this.sortBy !== column) return 'none';
    return this.sortOrder === 'asc' ? 'ascending' : 'descending';
  }

  onSortKeydown(event: KeyboardEvent, column: string): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onSort(column);
    }
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadTrigger$.next();
  }

  openDetail(id: number): void {
    this.router.navigate(['/pharmacy/prescriptions', id]);
  }

  canDispense(prescription: PrescriptionDto): boolean {
    return prescription.status === 'Pending' && this.dispensingId !== prescription.id;
  }

  dispensePrescription(prescription: PrescriptionDto): void {
    if (!this.canDispense(prescription)) {
      return;
    }

    this.dialog
      .showCustom({
        type: DialogType.QUESTION,
        title: 'Izdaj recept',
        message: `Potvrdite izdavanje recepta ${prescription.prescriptionNumber}.`,
        buttons: [
          { type: DialogButton.CANCEL, label: 'Odustani' },
          { type: DialogButton.OK, label: 'Izdaj', color: 'primary' },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.OK) {
          return;
        }

        this.dispensingId = prescription.id;
        this.pharmacyApi
          .dispensePrescription(prescription.id, { dispensedDate: new Date().toISOString() })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.dispensingId = null;
              this.toaster.success('Recept uspješno izdan.');
              this.loadTrigger$.next();
            },
            error: (err) => {
              this.dispensingId = null;
              this.toaster.error(getApiErrorMessage(err, 'Greška pri izdavanju recepta.'));
            },
          });
      });
  }

  createNew(): void {
    this.router.navigate(['/pharmacy/prescriptions/new']);
  }

  cancelPrescription(prescription: PrescriptionDto): void {
    if (prescription.status !== 'Pending') {
      this.toaster.error('Samo recepti na čekanju mogu biti otkazani.');
      return;
    }

    this.dialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Otkaži recept',
        message: `Jeste li sigurni da želite otkazati recept ${prescription.prescriptionNumber}?`,
        buttons: [
          { type: DialogButton.CANCEL, label: 'Odustani' },
          { type: DialogButton.DELETE, label: 'Otkaži', color: 'warn' },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.DELETE) {
          return;
        }

        this.pharmacyApi
          .cancelPrescription(prescription.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.toaster.success('Recept je otkazan.');
              this.loadTrigger$.next();
            },
            error: (err) => {
              this.toaster.error(getApiErrorMessage(err, 'Greška pri otkazivanju recepta.'));
            },
          });
      });
  }

  exportPdf(): void {
    this.pharmacyApi
      .exportPrescriptionsPdf(this.buildRequest())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.pharmacyApi.downloadBlobResponse(res, 'prescriptions.pdf');
          this.toaster.success('PDF recepata preuzet.');
        },
        error: () => this.toaster.error('Greška pri exportu PDF.'),
      });
  }

  printPrescription(prescription: PrescriptionDto): void {
    this.router.navigate(['/pharmacy/prescriptions', prescription.id]);
  }

  private formatRelativeTime(value: string): string {
    const diffMs = Date.now() - new Date(value).getTime();
    const minutes = Math.floor(diffMs / 60000);

    if (minutes < 1) return 'upravo sada';
    if (minutes < 60) return `prije ${minutes}min`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `prije ${hours}h`;

    const days = Math.floor(hours / 24);
    return `prije ${days}d`;
  }
}
