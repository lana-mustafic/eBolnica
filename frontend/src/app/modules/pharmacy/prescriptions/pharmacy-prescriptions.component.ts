import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import {
  catchError,
  debounceTime,
  map,
  merge,
  Observable,
  of,
  shareReplay,
  startWith,
  Subject,
  switchMap,
  tap,
  finalize,
} from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { PharmacyActivityDto, PrescriptionDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import { formatPharmacyActivityMeta, mapPrescriptionActivityType } from '../shared/pharmacy-activity.util';
import {
  getPrescriptionStatusClass,
  getPrescriptionStatusLabel,
} from './prescription-status.util';
import { DialogButton, DialogType } from '../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../shared/services/dialog-helper.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { resolvePharmacyApiErrorMessage } from '../shared/utils/pharmacy-api-error.util';
import { validatePrescriptionStock } from '../shared/utils/prescription-dispense.util';
import { MatTableDataSource } from '@angular/material/table';

interface PrescriptionActivityItem {
  id: number;
  type: 'new' | 'success' | 'warning';
  message: string;
  timeLabel: string;
}

interface PrescriptionsListViewModel {
  loading: boolean;
  error: boolean;
  prescriptions: PrescriptionDto[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  totalPrescriptions: number;
  pendingPrescriptions: number;
  dispensedPrescriptions: number;
  totalRevenue: number;
  totalRevenueLabel: string;
  tableEmptyMessage: string;
  isOutOfRangePage: boolean;
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

  readonly tableDataSource = new MatTableDataSource<PrescriptionDto>([]);

  currentPage = signal(1);
  totalPages = signal(0);
  pageSize = 10;

  dispensingId = signal<number | null>(null);
  isExporting = signal(false);

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

  readonly activities = toSignal(
    merge(of(undefined), this.activitiesLoadTrigger$).pipe(
      switchMap(() =>
        this.pharmacyApi.listRecentActivities({ limit: 6, category: 'prescription' }).pipe(
          catchError(() => of([] as PharmacyActivityDto[]))
        )
      ),
      map((activities) =>
        activities.map((activity) => ({
          id: activity.id,
          type: mapPrescriptionActivityType(activity),
          message: activity.message,
          timeLabel: formatPharmacyActivityMeta(activity),
        }))
      )
    ),
    { initialValue: [] as PrescriptionActivityItem[] }
  );

  readonly listState$ = this.loadTrigger$.pipe(
    switchMap(() => this.loadPrescriptionsViewModel()),
    tap((vm) => {
      this.currentPage.set(vm.currentPage);
      this.totalPages.set(vm.totalPages);
      this.tableDataSource.data = vm.prescriptions;
    }),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  readonly listState = toSignal(this.listState$, {
    initialValue: this.emptyViewModel({ loading: true }),
  });

  ngOnInit(): void {
    this.filterChanged$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadTrigger$.next();
      });

    this.loadTrigger$.next();
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

  private loadPrescriptionsViewModel(): Observable<PrescriptionsListViewModel> {
    return this.pharmacyApi.listPrescriptions(this.buildRequest()).pipe(
      switchMap((res) => {
        const items = res.items ?? [];
        if (items.length === 0 && res.totalCount > 0 && res.currentPage > res.totalPages) {
          this.currentPage.set(1);
          return this.pharmacyApi.listPrescriptions({ ...this.buildRequest(), pageNumber: 1 });
        }
        return of(res);
      }),
      map((res) => this.toViewModel(res)),
      catchError((err) => {
        this.toaster.error(getApiErrorMessage(err, 'Greška pri učitavanju recepata.'));
        return of(this.emptyViewModel({ error: true }));
      }),
      startWith(this.emptyViewModel({ loading: true }))
    );
  }

  private toViewModel(res: {
    items: PrescriptionDto[];
    totalCount: number;
    totalPages: number;
    currentPage: number;
    summary: {
      totalPrescriptions: number;
      pendingPrescriptions: number;
      dispensedPrescriptions: number;
      totalRevenue: number;
    };
  }): PrescriptionsListViewModel {
    const items = res.items ?? [];
    const summary = res.summary ?? {
      totalPrescriptions: 0,
      pendingPrescriptions: 0,
      dispensedPrescriptions: 0,
      totalRevenue: 0,
    };
    const isOutOfRangePage =
      items.length === 0 && res.totalCount > 0 && res.currentPage > res.totalPages;

    return {
      loading: false,
      error: false,
      prescriptions: items,
      totalCount: res.totalCount ?? items.length,
      totalPages: res.totalPages ?? 1,
      currentPage: res.currentPage ?? 1,
      totalPrescriptions: summary.totalPrescriptions,
      pendingPrescriptions: summary.pendingPrescriptions,
      dispensedPrescriptions: summary.dispensedPrescriptions,
      totalRevenue: summary.totalRevenue,
      totalRevenueLabel: `${Math.round(summary.totalRevenue).toLocaleString('bs-BA')} KM`,
      isOutOfRangePage,
      tableEmptyMessage: this.buildTableEmptyMessage(isOutOfRangePage, res.totalCount ?? 0, res.currentPage ?? 1),
    };
  }

  private emptyViewModel(opts: { loading?: boolean; error?: boolean }): PrescriptionsListViewModel {
    return {
      loading: opts.loading ?? false,
      error: opts.error ?? false,
      prescriptions: [],
      totalCount: 0,
      totalPages: 0,
      currentPage: this.currentPage(),
      totalPrescriptions: 0,
      pendingPrescriptions: 0,
      dispensedPrescriptions: 0,
      totalRevenue: 0,
      totalRevenueLabel: '0 KM',
      isOutOfRangePage: false,
      tableEmptyMessage: this.buildTableEmptyMessage(false, 0, this.currentPage()),
    };
  }

  private buildTableEmptyMessage(
    isOutOfRangePage: boolean,
    totalCount: number,
    currentPage: number
  ): string {
    if (isOutOfRangePage) {
      return `Nema recepata na stranici ${currentPage}. Pronađeno ${totalCount} za odabrane filtere.`;
    }

    if (this.hasActiveFilters()) {
      return 'Nema recepata koji odgovaraju odabranim filterima.';
    }

    return 'Još nema unesenih recepata.';
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
    this.reloadActivities();
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

  goToFirstPage(): void {
    if (this.currentPage() <= 1) return;
    this.currentPage.set(1);
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

    const validation = validatePrescriptionStock(prescription);
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
              this.toaster.error(
                resolvePharmacyApiErrorMessage(err, 'Greška pri izdavanju recepta.')
              );
              this.loadTrigger$.next();
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
    if (this.isExporting()) {
      return;
    }

    this.isExporting.set(true);
    this.pharmacyApi
      .exportPrescriptionsPdf(this.buildRequest())
      .pipe(
        finalize(() => this.isExporting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => {
          this.pharmacyApi.downloadBlobResponse(res, 'prescriptions.pdf');
          this.toaster.success('PDF recepata preuzet.');
        },
        error: (err) => this.toaster.error(getApiErrorMessage(err, 'Greška pri exportu PDF.')),
      });
  }

  printPrescription(prescription: PrescriptionDto): void {
    this.router.navigate(['/pharmacy/prescriptions', prescription.id], {
      queryParams: { print: '1' },
    });
  }
}
