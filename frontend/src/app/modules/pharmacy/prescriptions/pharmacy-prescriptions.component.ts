import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Subject, catchError, debounceTime, of, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { PrescriptionDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { ToasterService } from '../../../core/services/toaster.service';
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

  auth = inject(AuthFacadeService);

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
    { value: 'Cancelled', label: 'Odbijeni' },
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
    this.pharmacyApi
      .getDashboardStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const summary = res.metadata.summary;
          this.totalPrescriptions = summary.totalPrescriptions;
          this.pendingPrescriptions = summary.pendingPrescriptions;
          this.dispensedPrescriptions = Math.max(
            0,
            summary.totalPrescriptions - summary.pendingPrescriptions
          );
          this.totalRevenue = summary.totalRevenue;
        },
        error: () => {
          this.totalPrescriptions = 0;
          this.pendingPrescriptions = 0;
          this.dispensedPrescriptions = 0;
          this.totalRevenue = 0;
        },
      });

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

  get visiblePrescriptions(): PrescriptionDto[] {
    if (this.selectedDateRange === 'all') {
      return this.prescriptions;
    }

    const now = new Date();
    const start = new Date(now);

    if (this.selectedDateRange === 'today') {
      start.setHours(0, 0, 0, 0);
    } else if (this.selectedDateRange === 'week') {
      start.setDate(start.getDate() - 7);
    } else if (this.selectedDateRange === 'month') {
      start.setDate(start.getDate() - 30);
    }

    return this.prescriptions.filter((prescription) => {
      const date = new Date(prescription.prescribedDate);
      return date >= start && date <= now;
    });
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
    switch (status) {
      case 'Pending':
        return 'Na čekanju';
      case 'Dispensed':
        return 'Izdan';
      case 'Cancelled':
        return 'Odbijen';
      default:
        return status;
    }
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'status-pending';
      case 'Dispensed':
        return 'status-dispensed';
      case 'Cancelled':
        return 'status-cancelled';
      default:
        return 'status-preparing';
    }
  }

  private buildRequest() {
    const searchTerm = this.resolveSearchTerm();

    return {
      status: this.selectedStatus === 'All' ? undefined : this.selectedStatus,
      search: searchTerm,
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
    };
  }

  private resolveSearchTerm(): string | undefined {
    if (this.search.trim()) {
      return this.search.trim();
    }

    if (this.patientFilter.trim()) {
      return this.patientFilter.trim();
    }

    if (this.doctorFilter.trim()) {
      return this.doctorFilter.trim();
    }

    return undefined;
  }

  onFilterChange(): void {
    this.filterChanged$.next();
  }

  onStatusChange(status: string): void {
    this.selectedStatus = status;
    this.currentPage = 1;
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
              const msg = err?.error?.message ?? err?.error?.title ?? 'Greška pri izdavanju recepta.';
              this.toaster.error(msg);
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
