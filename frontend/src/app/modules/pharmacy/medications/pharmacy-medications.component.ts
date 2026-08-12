import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  OnDestroy,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  forkJoin,
  map,
  Observable,
  of,
  shareReplay,
  Subject,
  switchMap,
  tap,
  finalize,
} from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import {
  InventoryResponse,
  MedicationAutocompleteSuggestion,
  MedicationDto,
  MedicationImportResult,
} from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { DialogButton, DialogType } from '../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../shared/services/dialog-helper.service';
import { MedicationImageUrlService } from '../services/medication-image-url.service';
import { getMedicationCategoryLabel, MEDICATION_CATEGORIES } from '../constants/medication-categories.constant';
import {
  buildMedicationListQuery,
  clearMedicationListFilters,
  hasMedicationListFilters,
  MedicationListFilters,
  MedicationListSort,
} from '../shared/utils/medication-list-query.util';
import { pipeListLoad } from '../shared/utils/pharmacy-list-load.util';
import { resolvePharmacyApiErrorMessage } from '../shared/utils/pharmacy-api-error.util';
import {
  canGoToPage,
  onTableSortKeydown,
  sortAriaSort,
  sortIndicator,
  toggleSortColumn,
} from '../shared/utils/pharmacy-table.util';
import { MatTableDataSource } from '@angular/material/table';

interface MedicationsListViewModel {
  loading: boolean;
  error: boolean;
  medications: MedicationDto[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  totalMedications: number;
  lowStockAlertCount: number;
  expiryAlertCount: number;
}

@Component({
  selector: 'app-pharmacy-medications',
  standalone: false,
  templateUrl: './pharmacy-medications.component.html',
  styleUrl: './pharmacy-medications.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PharmacyMedicationsComponent implements OnInit, OnDestroy {
  @ViewChild('csvInput') csvInput?: ElementRef<HTMLInputElement>;

  private pharmacyApi = inject(PharmacyApiService);
  private imageUrlService = inject(MedicationImageUrlService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private dialog = inject(DialogHelperService);
  private destroyRef = inject(DestroyRef);
  auth = inject(AuthFacadeService);

  readonly tableDataSource = new MatTableDataSource<MedicationDto>([]);

  readonly categoryLabel = getMedicationCategoryLabel;
  readonly categories = MEDICATION_CATEGORIES;

  isLoading = signal(false);
  loadError = signal(false);
  currentPage = signal(1);
  totalPages = signal(0);
  readonly pageSize = 10;
  medicationsOnPageCount = signal(0);

  filters: MedicationListFilters = {
    search: '',
    selectedCategory: '',
    selectedStockStatus: '',
    selectedRequiresPrescription: '',
    showInactive: false,
  };

  sort: MedicationListSort = { sortBy: 'createdAt', sortOrder: 'desc' };

  autocompleteSuggestions = signal<MedicationAutocompleteSuggestion[]>([]);
  showAutocomplete = signal(false);
  selectedSuggestionIndex = signal(-1);
  isImporting = signal(false);
  isExporting = signal(false);
  importSummary = signal<MedicationImportResult | null>(null);
  selectedMedication = signal<MedicationDto | null>(null);

  readonly activeSuggestionId = signal<string | null>(null);

  private thumbnailUrls = signal(new Map<number, string>());
  private thumbnailLoadGeneration = 0;
  private searchBlurTimeoutId?: ReturnType<typeof setTimeout>;

  private searchChanged$ = new Subject<void>();
  private autocompleteQuery$ = new Subject<string>();
  private loadTrigger$ = new Subject<void>();

  readonly listState$ = this.loadTrigger$.pipe(
    switchMap(() => this.loadMedicationsViewModel()),
    tap((vm) => {
      this.currentPage.set(vm.currentPage);
      this.totalPages.set(vm.totalPages);
      this.medicationsOnPageCount.set(vm.medications.length);
      this.tableDataSource.data = vm.medications;
      if (!vm.loading && !vm.error) {
        this.loadThumbnailUrls(vm.medications);
      }
    }),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  readonly listState = toSignal(this.listState$, {
    initialValue: this.emptyViewModel({ loading: true }),
  });

  displayedColumns = ['name', 'category', 'stockQuantity', 'expiryDate', 'createdAt', 'status', 'actions'];

  ngOnInit(): void {
    this.searchChanged$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadTrigger$.next();
      });

    this.autocompleteQuery$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((q) => {
          if (q.trim().length < 2) {
            this.showAutocomplete.set(false);
            return of([] as MedicationAutocompleteSuggestion[]);
          }
          return this.pharmacyApi.getAutocomplete(q).pipe(
            catchError(() => of([] as MedicationAutocompleteSuggestion[]))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((suggestions) => {
        this.autocompleteSuggestions.set(suggestions);
        this.showAutocomplete.set(suggestions.length > 0);
        this.selectedSuggestionIndex.set(suggestions.length > 0 ? 0 : -1);
        this.activeSuggestionId.set(
          suggestions.length > 0 ? `medication-suggestion-0` : null
        );
      });

    this.loadTrigger$.next();
  }

  ngOnDestroy(): void {
    this.clearSearchBlurTimeout();
    this.imageUrlService.revokeAll();
  }

  private loadMedicationsViewModel(): Observable<MedicationsListViewModel> {
    return pipeListLoad(
      forkJoin({
        list: this.pharmacyApi.listMedications(this.buildRequest()),
        stats: this.pharmacyApi
          .getInventory({ pageNumber: 1, pageSize: 1 })
          .pipe(catchError(() => of(null as InventoryResponse | null))),
      }).pipe(map(({ list, stats }) => this.toViewModel(list, stats))),
      { isLoading: this.isLoading, loadError: this.loadError },
      (opts) => this.emptyViewModel(opts),
      'Greška pri učitavanju lijekova.',
      (message) => {
        this.thumbnailUrls.set(new Map());
        this.toaster.error(message);
      }
    );
  }

  private toViewModel(
    res: {
      items: MedicationDto[];
      totalCount: number;
      totalPages: number;
      currentPage: number;
    },
    stats: InventoryResponse | null
  ): MedicationsListViewModel {
    const medications = res.items ?? [];
    return {
      loading: false,
      error: false,
      medications,
      totalCount: res.totalCount ?? medications.length,
      totalPages: res.totalPages ?? 1,
      currentPage: res.currentPage ?? 1,
      totalMedications: stats?.totalMedications ?? res.totalCount ?? medications.length,
      lowStockAlertCount: stats?.lowStockAlertCount ?? 0,
      expiryAlertCount: stats?.expiryAlertCount ?? 0,
    };
  }

  private emptyViewModel(opts: { loading?: boolean; error?: boolean }): MedicationsListViewModel {
    return {
      loading: opts.loading ?? false,
      error: opts.error ?? false,
      medications: [],
      totalCount: 0,
      totalPages: 0,
      currentPage: this.currentPage(),
      totalMedications: 0,
      lowStockAlertCount: 0,
      expiryAlertCount: 0,
    };
  }

  retryLoad(): void {
    this.loadTrigger$.next();
  }

  reload(): void {
    this.loadTrigger$.next();
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadTrigger$.next();
  }

  onSortKeydown(event: KeyboardEvent, column: string): void {
    onTableSortKeydown(event, column, (col) => this.onSort(col));
  }

  private buildRequest() {
    return buildMedicationListQuery(
      this.filters,
      { pageNumber: this.currentPage(), pageSize: this.pageSize },
      this.sort,
      { includeActiveFlags: true }
    );
  }

  clearFilters(): void {
    clearMedicationListFilters(this.filters);
    this.currentPage.set(1);
    this.loadTrigger$.next();
  }

  hasActiveFilters(): boolean {
    return hasMedicationListFilters(this.filters);
  }

  onSort(column: string): void {
    this.sort = toggleSortColumn(this.sort, column);
    this.currentPage.set(1);
    this.loadTrigger$.next();
  }

  sortIndicator(column: string): string {
    return sortIndicator(this.sort.sortBy, this.sort.sortOrder, column);
  }

  onSearchInput(): void {
    this.searchChanged$.next();
    this.autocompleteQuery$.next(this.filters.search);
  }

  onSearchKeydown(event: KeyboardEvent): void {
    const suggestions = this.autocompleteSuggestions();
    if (!this.showAutocomplete() || suggestions.length === 0) {
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      const nextIndex = Math.min(this.selectedSuggestionIndex() + 1, suggestions.length - 1);
      this.selectedSuggestionIndex.set(nextIndex);
      this.activeSuggestionId.set(`medication-suggestion-${nextIndex}`);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      const nextIndex = Math.max(this.selectedSuggestionIndex() - 1, 0);
      this.selectedSuggestionIndex.set(nextIndex);
      this.activeSuggestionId.set(`medication-suggestion-${nextIndex}`);
    } else if (event.key === 'Enter' && this.selectedSuggestionIndex() >= 0) {
      event.preventDefault();
      this.selectSuggestion(suggestions[this.selectedSuggestionIndex()]);
    } else if (event.key === 'Escape') {
      this.showAutocomplete.set(false);
      this.selectedSuggestionIndex.set(-1);
      this.activeSuggestionId.set(null);
    }
  }

  onSearchBlur(): void {
    this.clearSearchBlurTimeout();
    this.searchBlurTimeoutId = window.setTimeout(() => {
      this.showAutocomplete.set(false);
      this.selectedSuggestionIndex.set(-1);
      this.activeSuggestionId.set(null);
      this.searchBlurTimeoutId = undefined;
    }, 150);
  }

  private clearSearchBlurTimeout(): void {
    if (this.searchBlurTimeoutId != null) {
      clearTimeout(this.searchBlurTimeoutId);
      this.searchBlurTimeoutId = undefined;
    }
  }

  selectSuggestion(s: MedicationAutocompleteSuggestion): void {
    this.filters.search = s.name;
    this.showAutocomplete.set(false);
    this.selectedSuggestionIndex.set(-1);
    this.activeSuggestionId.set(null);
    this.onFilterChange();
  }

  onFilterChange(): void {
    this.searchChanged$.next();
  }

  goToPage(page: number): void {
    if (!canGoToPage(page, this.totalPages())) return;
    this.currentPage.set(page);
    this.loadTrigger$.next();
  }

  createNew(): void {
    this.router.navigate(['/pharmacy/medications/new']);
  }

  openWizard(): void {
    this.router.navigate(['/pharmacy/medications/wizard']);
  }

  editMedication(id: number): void {
    this.router.navigate(['/pharmacy/medications', id, 'edit']);
  }

  openDetail(id: number): void {
    this.router.navigate(['/pharmacy/medications', id]);
  }

  sortAriaSort(column: string): 'ascending' | 'descending' | 'none' {
    return sortAriaSort(this.sort.sortBy, this.sort.sortOrder, column);
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
              if (this.medicationsOnPageCount() === 1 && this.currentPage() > 1) {
                this.currentPage.update((page) => page - 1);
              }
              this.loadTrigger$.next();
            },
            error: (err) => {
              this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri deaktivaciji lijeka.'));
            },
          });
      });
  }

  exportCsv(): void {
    if (this.isExporting()) {
      return;
    }

    this.isExporting.set(true);
    this.pharmacyApi
      .exportMedicationsCsv(this.buildRequest())
      .pipe(
        finalize(() => this.isExporting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => {
          const blob = res.body;
          if (!blob) return;
          const disposition = res.headers.get('content-disposition') ?? '';
          const match = disposition.match(/filename="?([^";]+)"?/i);
          this.downloadBlob(blob, match?.[1] ?? 'medications-export.csv');
        },
        error: (err) => {
          this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri exportu CSV.'));
        },
      });
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
        error: (err) => this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri exportu PDF.')),
      });
  }

  downloadTemplate(): void {
    if (this.isExporting()) {
      return;
    }

    this.isExporting.set(true);
    this.pharmacyApi
      .downloadImportTemplate()
      .pipe(
        finalize(() => this.isExporting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => {
          const blob = res.body;
          if (!blob) return;
          this.downloadBlob(blob, 'medication-import-template.csv');
        },
        error: (err) => this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri preuzimanju templatea.')),
      });
  }

  triggerImport(): void {
    this.csvInput?.nativeElement.click();
  }

  onImportFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.isImporting.set(true);
    this.importSummary.set(null);
    this.pharmacyApi
      .importMedicationsCsv(file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary) => {
          this.isImporting.set(false);
          this.importSummary.set(summary);
          if (summary.successCount > 0) {
            this.loadTrigger$.next();
          }

          if (!summary.committed) {
            this.toaster.error(
              summary.batchError ??
                `Import nije sačuvao nijedan lijek (${summary.failureCount} grešaka).`
            );
            return;
          }

          if (summary.isPartialImport) {
            this.toaster.warning(
              `Djelomičan import: ${summary.successCount} uspješno, ${summary.failureCount} preskočeno.`
            );
            return;
          }

          this.toaster.success(`Import završen: ${summary.successCount} lijek(ova) uvezeno.`);
        },
        error: (err) => {
          this.isImporting.set(false);
          this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri importu CSV.'));
        },
      });
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

  getDisplayStatus(m: MedicationDto): string {
    if (!m.isActive) return 'Neaktivan';
    if (this.isExpired(m)) return 'Istekao';
    if (m.stockQuantity === 0 || m.stockQuantity < m.minimumStockLevel) return 'Niska';
    return 'Dostupno';
  }

  getDisplayStatusClass(m: MedicationDto): string {
    const status = this.getDisplayStatus(m);
    if (status === 'Neaktivan') return 'inactive';
    if (status === 'Istekao') return 'expired';
    if (status === 'Niska') return 'low';
    return 'ok';
  }

  getStockProgressPercent(m: MedicationDto): number {
    const target = Math.max(m.minimumStockLevel * 2, m.minimumStockLevel + 10, 1);
    return Math.min(100, Math.round((m.stockQuantity / target) * 100));
  }

  getStockProgressClass(m: MedicationDto): string {
    if (this.isExpired(m) || m.stockQuantity === 0) return 'out';
    if (m.stockQuantity < m.minimumStockLevel) return 'low';
    return 'ok';
  }

  getExpiryDotClass(m: MedicationDto): string {
    if (this.isExpired(m)) return 'expired';
    if (this.isExpiringSoon(m)) return 'soon';
    return 'ok';
  }

  getExpiryHint(m: MedicationDto): string {
    if (this.isExpired(m)) return 'Istekao';
    if (this.isExpiringSoon(m)) return 'Ističe uskoro';
    return 'Dostupno';
  }

  isExpired(m: MedicationDto): boolean {
    if (!m.expiryDate) return false;
    const expiry = new Date(m.expiryDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return expiry < today;
  }

  isExpiringSoon(m: MedicationDto): boolean {
    if (!m.expiryDate || this.isExpired(m)) return false;
    const expiry = new Date(m.expiryDate);
    const horizon = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000);
    return expiry <= horizon;
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

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
