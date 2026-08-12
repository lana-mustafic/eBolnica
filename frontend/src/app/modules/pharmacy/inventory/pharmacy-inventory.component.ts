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
  debounceTime,
  map,
  Observable,
  shareReplay,
  Subject,
  switchMap,
  tap,
  finalize,
} from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { getMedicationCategoryLabel, MEDICATION_CATEGORIES } from '../constants/medication-categories.constant';
import { ToasterService } from '../../../core/services/toaster.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { DialogButton, DialogType } from '../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../shared/services/dialog-helper.service';
import {
  buildMedicationListQuery,
  clearMedicationListFilters,
  hasMedicationListFilters,
  MedicationListFilters,
  MedicationListSort,
} from '../shared/utils/medication-list-query.util';
import { pipeListLoad } from '../shared/utils/pharmacy-list-load.util';
import { resolvePharmacyApiErrorMessage } from '../shared/utils/pharmacy-api-error.util';
import { MedicationImageUrlService } from '../services/medication-image-url.service';
import {
  canGoToPage,
  onTableSortKeydown,
  sortAriaSort,
  sortIndicator,
  toggleSortColumn,
} from '../shared/utils/pharmacy-table.util';

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
  private dialog = inject(DialogHelperService);
  private destroyRef = inject(DestroyRef);
  private imageUrlService = inject(MedicationImageUrlService);
  auth = inject(AuthFacadeService);

  readonly categoryLabel = getMedicationCategoryLabel;
  readonly categories = MEDICATION_CATEGORIES;
  readonly pageSize = 10;

  isLoading = signal(false);
  isExporting = signal(false);
  loadError = signal(false);
  currentPage = signal(1);
  totalPages = signal(0);

  filters: MedicationListFilters = {
    search: '',
    selectedCategory: '',
    selectedStockStatus: '',
    selectedRequiresPrescription: '',
  };

  sort: MedicationListSort = { sortBy: 'name', sortOrder: 'asc' };

  displayedColumns = ['name', 'stock', 'expiry', 'status', 'actions'];
  selectedMedication: MedicationDto | null = null;

  private thumbnailUrls = signal(new Map<number, string>());
  private thumbnailLoadGeneration = 0;

  private filterChanged$ = new Subject<void>();
  private loadTrigger$ = new Subject<void>();

  readonly listState$ = this.loadTrigger$.pipe(
    switchMap(() => this.loadInventoryViewModel()),
    tap((vm) => {
      this.currentPage.set(vm.currentPage);
      this.totalPages.set(vm.totalPages);
      if (!this.isLoading() && !this.loadError()) {
        this.loadThumbnailUrls(vm.items);
      }
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
    return hasMedicationListFilters(this.filters);
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

  imageUrl(m: MedicationDto): string | null {
    return this.thumbnailUrls().get(m.id) ?? null;
  }

  onImageError(medicationId: number): void {
    this.thumbnailUrls.update((map) => {
      const next = new Map(map);
      next.delete(medicationId);
      return next;
    });
  }

  viewMedication(id: number): void {
    this.router.navigate(['/pharmacy/medications', id]);
  }

  editMedication(id: number): void {
    this.router.navigate(['/pharmacy/medications', id, 'edit']);
  }

  addStock(id: number): void {
    this.router.navigate(['/pharmacy/medications', id]);
  }

  deleteMedication(medication: MedicationDto): void {
    this.dialog
      .showCustom({
        type: DialogType.WARNING,
        title: 'Deaktiviraj lijek',
        message: `Jeste li sigurni da želite deaktivirati lijek "${medication.name}"?`,
        buttons: [
          { type: DialogButton.CANCEL, label: 'Odustani' },
          { type: DialogButton.DELETE, label: 'Deaktiviraj', color: 'warn' },
        ],
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result?.button !== DialogButton.DELETE) {
          return;
        }

        this.pharmacyApi
          .deleteMedication(medication.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.toaster.success('Lijek deaktiviran.');
              this.loadTrigger$.next();
            },
            error: (err) => {
              this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri deaktivaciji lijeka.'));
            },
          });
      });
  }

  private loadInventoryViewModel(): Observable<InventoryListViewModel> {
    return pipeListLoad(
      this.pharmacyApi.getInventory(this.buildRequest()).pipe(map((res) => this.toViewModel(res))),
      { isLoading: this.isLoading, loadError: this.loadError },
      (opts) => this.emptyViewModel(opts),
      'Greška pri učitavanju inventara.',
      (message) => this.toaster.error(message)
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
    return buildMedicationListQuery(
      this.filters,
      { pageNumber: this.currentPage(), pageSize: this.pageSize },
      this.sort
    );
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
    if (this.isExporting()) {
      return;
    }

    this.isExporting.set(true);
    this.pharmacyApi
      .exportInventoryPdf(this.buildRequest())
      .pipe(
        finalize(() => this.isExporting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => {
          this.pharmacyApi.downloadBlobResponse(res, 'inventory.pdf');
          this.toaster.success('PDF inventar preuzet.');
        },
        error: (err) =>
          this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri exportu PDF.')),
      });
  }

  clearFilters(): void {
    clearMedicationListFilters(this.filters);
    this.currentPage.set(1);
    this.loadTrigger$.next();
  }

  retryLoad(): void {
    this.loadTrigger$.next();
  }

  goToPage(page: number): void {
    if (!canGoToPage(page, this.totalPages())) return;
    this.currentPage.set(page);
    this.loadTrigger$.next();
  }

  onSort(column: string): void {
    this.sort = toggleSortColumn(this.sort, column);
    this.currentPage.set(1);
    this.loadTrigger$.next();
  }

  sortIndicator(column: string): string {
    return sortIndicator(this.sort.sortBy, this.sort.sortOrder, column);
  }

  sortAriaSort(column: string): 'ascending' | 'descending' | 'none' {
    return sortAriaSort(this.sort.sortBy, this.sort.sortOrder, column);
  }

  onSortKeydown(event: KeyboardEvent, column: string): void {
    onTableSortKeydown(event, column, (col) => this.onSort(col));
  }

  private loadThumbnailUrls(medications: MedicationDto[]): void {
    const generation = ++this.thumbnailLoadGeneration;
    this.imageUrlService.revokeAll();
    this.thumbnailUrls.set(new Map());

    for (const medication of medications) {
      if (!medication.primaryImageId) {
        continue;
      }

      this.imageUrlService
        .getAuthenticatedUrl(medication.id, medication.primaryImageId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (url) => {
            if (generation !== this.thumbnailLoadGeneration) {
              return;
            }
            this.thumbnailUrls.update((map) => {
              const next = new Map(map);
              next.set(medication.id, url);
              return next;
            });
          },
          error: () => {
            if (generation !== this.thumbnailLoadGeneration) {
              return;
            }
          },
        });
    }
  }
}
