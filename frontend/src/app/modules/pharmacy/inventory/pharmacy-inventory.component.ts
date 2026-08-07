import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import {
  catchError,
  debounceTime,
  map,
  Observable,
  of,
  shareReplay,
  startWith,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { getMedicationCategoryLabel, MEDICATION_CATEGORIES } from '../constants/medication-categories.constant';
import { ToasterService } from '../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

interface InventoryListViewModel {
  loading: boolean;
  error: boolean;
  items: MedicationDto[];
  lowStockAlerts: MedicationDto[];
  expiryAlerts: MedicationDto[];
  lowStockAlertCount: number;
  expiryAlertCount: number;
  inventoryValue: number;
  inventoryValueLabel: string;
  totalMedications: number;
  totalCount: number;
  currentPage: number;
  totalPages: number;
  firstLowStockAlert: MedicationDto | null;
}

@Component({
  selector: 'app-pharmacy-inventory',
  standalone: false,
  templateUrl: './pharmacy-inventory.component.html',
  styleUrl: './pharmacy-inventory.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyInventoryComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private toaster = inject(ToasterService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  auth = inject(AuthFacadeService);

  readonly categoryLabel = getMedicationCategoryLabel;
  readonly categories = MEDICATION_CATEGORIES;

  currentPage = signal(1);
  totalPages = signal(0);

  search = '';
  selectedCategory = '';
  selectedStockStatus = '';
  selectedRequiresPrescription = '';

  sortBy = 'name';
  sortOrder: 'asc' | 'desc' = 'asc';

  displayedColumns = ['name', 'stock', 'expiry', 'status', 'actions'];
  selectedMedication: MedicationDto | null = null;

  private filterChanged$ = new Subject<void>();
  private loadTrigger$ = new Subject<void>();

  readonly listState$ = this.loadTrigger$.pipe(
    switchMap(() => this.loadInventoryViewModel()),
    tap((vm) => {
      this.currentPage.set(vm.currentPage);
      this.totalPages.set(vm.totalPages);
    }),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  ngOnInit(): void {
    this.filterChanged$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadTrigger$.next();
      });

    this.loadTrigger$.next();
  }

  formatMedicationCount(count: number): string {
    if (count === 1) return '1 lijek';
    if (count >= 2 && count <= 4) return `${count} lijeka`;
    return `${count} lijekova`;
  }

  getDisplayStatus(m: MedicationDto): string {
    if (m.stockQuantity <= 0) return 'Kritično';
    if (this.getExpiryStatus(m) === 'Istekao') return 'Kritično';
    if (m.stockQuantity < m.minimumStockLevel) return 'Niska';
    if (this.getExpiryStatus(m) === 'Ističe uskoro') return 'Niska';
    return 'Dostupno';
  }

  getDisplayStatusClass(m: MedicationDto): string {
    const status = this.getDisplayStatus(m);
    if (status === 'Kritično') return 'critical';
    if (status === 'Niska') return 'low';
    return 'ok';
  }

  hasActiveFilters(): boolean {
    return !!(
      this.search ||
      this.selectedCategory ||
      this.selectedStockStatus ||
      this.selectedRequiresPrescription
    );
  }

  getStockStatus(m: MedicationDto): string {
    if (m.stockQuantity <= 0) return 'Kritično';
    if (m.stockQuantity < m.minimumStockLevel) return 'Niska zaliha';
    return 'Dostupno';
  }

  getExpiryStatus(m: MedicationDto): string {
    if (!m.expiryDate) return '-';

    const now = new Date();
    const expiry = new Date(m.expiryDate);
    const horizon = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);

    if (expiry < now) return 'Istekao';
    if (expiry <= horizon) return 'Ističe uskoro';
    return 'Važi';
  }

  getExpiryStatusClass(m: MedicationDto): string {
    const status = this.getExpiryStatus(m);
    if (status === 'Ističe uskoro') return 'low';
    if (status === 'Istekao') return 'critical';
    if (status === 'Važi') return 'ok';
    return 'neutral';
  }

  getStockStatusClass(m: MedicationDto): string {
    const status = this.getStockStatus(m);
    if (status === 'Niska zaliha') return 'low';
    if (status === 'Kritično') return 'critical';
    return 'ok';
  }

  viewMedication(id: number): void {
    this.router.navigate(['/pharmacy/medications', id]);
  }

  editMedication(id: number): void {
    this.router.navigate(['/pharmacy/medications', id, 'edit']);
  }

  private loadInventoryViewModel(): Observable<InventoryListViewModel> {
    return this.pharmacyApi.getInventory(this.buildRequest()).pipe(
      map((res) => this.toViewModel(res)),
      catchError((err) => {
        this.toaster.error(getApiErrorMessage(err, 'Greška pri učitavanju inventara.'));
        return of(this.emptyViewModel({ error: true }));
      }),
      startWith(this.emptyViewModel({ loading: true }))
    );
  }

  private toViewModel(res: {
    items: MedicationDto[];
    lowStockAlerts: MedicationDto[];
    expiryAlerts: MedicationDto[];
    lowStockAlertCount?: number;
    expiryAlertCount?: number;
    totalMedications: number;
    inventoryValue: number;
    totalCount: number;
    totalPages: number;
    currentPage: number;
  }): InventoryListViewModel {
    const inventoryValue = res.inventoryValue;
    return {
      loading: false,
      error: false,
      items: res.items,
      lowStockAlerts: res.lowStockAlerts,
      expiryAlerts: res.expiryAlerts,
      lowStockAlertCount: res.lowStockAlertCount ?? res.lowStockAlerts.length,
      expiryAlertCount: res.expiryAlertCount ?? res.expiryAlerts.length,
      inventoryValue,
      inventoryValueLabel: `${Math.round(inventoryValue).toLocaleString('bs-BA')} KM`,
      totalMedications: res.totalMedications,
      totalCount: res.totalCount,
      currentPage: res.currentPage,
      totalPages: res.totalPages,
      firstLowStockAlert: res.lowStockAlerts[0] ?? null,
    };
  }

  private emptyViewModel(opts: { loading?: boolean; error?: boolean }): InventoryListViewModel {
    return {
      loading: opts.loading ?? false,
      error: opts.error ?? false,
      items: [],
      lowStockAlerts: [],
      expiryAlerts: [],
      lowStockAlertCount: 0,
      expiryAlertCount: 0,
      inventoryValue: 0,
      inventoryValueLabel: '0 KM',
      totalMedications: 0,
      totalCount: 0,
      currentPage: this.currentPage(),
      totalPages: 0,
      firstLowStockAlert: null,
    };
  }

  private buildRequest() {
    const requiresPrescription =
      this.selectedRequiresPrescription === 'true'
        ? true
        : this.selectedRequiresPrescription === 'false'
          ? false
          : undefined;

    return {
      search: this.search || undefined,
      category: this.selectedCategory || undefined,
      stockStatus: this.selectedStockStatus || undefined,
      requiresPrescription,
      pageNumber: this.currentPage(),
      pageSize: 10,
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
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

  exportPdf(): void {
    this.pharmacyApi
      .exportInventoryPdf(this.buildRequest())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.pharmacyApi.downloadBlobResponse(res, 'inventory.pdf');
          this.toaster.success('PDF inventar preuzet.');
        },
        error: (err) => this.toaster.error(getApiErrorMessage(err, 'Greška pri exportu PDF.')),
      });
  }

  clearFilters(): void {
    this.search = '';
    this.selectedCategory = '';
    this.selectedStockStatus = '';
    this.selectedRequiresPrescription = '';
    this.currentPage.set(1);
    this.loadTrigger$.next();
  }

  retryLoad(): void {
    this.loadTrigger$.next();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
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
}
