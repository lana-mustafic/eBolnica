import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Subject, catchError, debounceTime, of, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { PharmacyActivityDto, PrescriptionDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import { formatRelativeTime } from '../../../core/utils/relative-time.util';
import {
  getPrescriptionStatusClass,
  getPrescriptionStatusLabel,
} from './prescription-status.util';
import { DialogButton, DialogType } from '../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../shared/services/dialog-helper.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { mapPrescriptionActivityType } from '../shared/pharmacy-activity.util';

interface PrescriptionActivityItem {
  id: number;
  type: 'new' | 'success' | 'warning';
  message: string;
  timeLabel: string;
}

@Component({
  selector: 'app-pharmacy-prescriptions',
  standalone: false,
  templateUrl: './pharmacy-prescriptions.component.html',
  styleUrl: './pharmacy-prescriptions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyPrescriptionsComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private dialog = inject(DialogHelperService);
  private destroyRef = inject(DestroyRef);

  auth = inject(AuthFacadeService);

  prescriptions = signal<PrescriptionDto[]>([]);
  isLoading = signal(false);
  loadError = signal(false);
  totalCount = signal(0);
  totalPages = signal(0);
  currentPage = signal(1);
  pageSize = 10;

  totalPrescriptions = signal(0);
  pendingPrescriptions = signal(0);
  dispensedPrescriptions = signal(0);
  totalRevenue = signal(0);

  dispensingId = signal<number | null>(null);

  search = '';
  selectedStatus = 'All';
  doctorFilter = '';
  patientFilter = '';
  selectedDateRange = 'all';

  sortBy = 'prescribedDate';
  sortOrder: 'asc' | 'desc' = 'desc';

  selectedPrescription: PrescriptionDto | null = null;

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
  private activitiesLoadTrigger$ = new Subject<void>();

  activities = signal<PharmacyActivityDto[]>([]);

  recentActivities = computed((): PrescriptionActivityItem[] =>
    this.activities().map((activity) => ({
      id: activity.id,
      type: mapPrescriptionActivityType(activity),
      message: activity.message,
      timeLabel: formatRelativeTime(activity.occurredAt),
    }))
  );

  ngOnInit(): void {
    this.activitiesLoadTrigger$
      .pipe(
        switchMap(() =>
          this.pharmacyApi.listRecentActivities({ limit: 6, category: 'prescription' }).pipe(
            catchError(() => of([] as PharmacyActivityDto[]))
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((activities) => this.activities.set(activities));

    this.loadTrigger$
      .pipe(
        switchMap(() => {
          this.isLoading.set(true);
          this.loadError.set(false);
          return this.pharmacyApi.listPrescriptions(this.buildRequest()).pipe(
            catchError(() => {
              this.loadError.set(true);
              this.prescriptions.set([]);
              this.toaster.error('Greška pri učitavanju recepata.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((res) => {
        this.isLoading.set(false);
        if (!res) return;

        this.prescriptions.set(res.items);
        this.totalPrescriptions.set(res.summary.totalPrescriptions);
        this.pendingPrescriptions.set(res.summary.pendingPrescriptions);
        this.dispensedPrescriptions.set(res.summary.dispensedPrescriptions);
        this.totalRevenue.set(res.summary.totalRevenue);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
        this.currentPage.set(res.currentPage);
      });

    this.filterChanged$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadTrigger$.next();
      });

    this.loadTrigger$.next();
    this.activitiesLoadTrigger$.next();
  }

  private reloadActivities(): void {
    this.activitiesLoadTrigger$.next();
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
      pageNumber: this.currentPage(),
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
    this.currentPage.set(1);
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
    this.currentPage.set(1);
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
    this.currentPage.set(1);
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
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadTrigger$.next();
  }

  openDetail(id: number): void {
    this.router.navigate(['/pharmacy/prescriptions', id]);
  }

  canDispense(prescription: PrescriptionDto): boolean {
    return prescription.status === 'Pending' && this.dispensingId() !== prescription.id;
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

        this.dispensingId.set(prescription.id);
        this.pharmacyApi
          .dispensePrescription(prescription.id, { dispensedDate: new Date().toISOString() })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.dispensingId.set(null);
              this.toaster.success('Recept uspješno izdan.');
              this.loadTrigger$.next();
              this.reloadActivities();
            },
            error: (err) => {
              this.dispensingId.set(null);
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
              this.reloadActivities();
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
}
