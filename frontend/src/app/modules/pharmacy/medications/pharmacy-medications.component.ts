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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
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
import {
  MedicationAutocompleteSuggestion,
  MedicationDto,
  MedicationImportResult,
} from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { DialogButton, DialogType } from '../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../shared/services/dialog-helper.service';
import { MedicationImageUrlService } from '../services/medication-image-url.service';
import { getMedicationCategoryLabel, MEDICATION_CATEGORIES } from '../constants/medication-categories.constant';

interface MedicationsListViewModel {
  loading: boolean;
  error: boolean;
  medications: MedicationDto[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  lowStockOnPageCount: number;
  expiringSoonOnPageCount: number;
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

  readonly categoryLabel = getMedicationCategoryLabel;
  readonly categories = MEDICATION_CATEGORIES;

  currentPage = signal(1);
  totalPages = signal(0);
  pageSize = 10;
  medicationsOnPageCount = signal(0);

  search = '';
  selectedCategory = '';
  selectedStockStatus = '';
  selectedRequiresPrescription = '';
  showInactive = false;

  sortBy = 'createdAt';
  sortOrder: 'asc' | 'desc' = 'desc';

  autocompleteSuggestions = signal<MedicationAutocompleteSuggestion[]>([]);
  showAutocomplete = signal(false);
  selectedSuggestionIndex = signal(-1);
  isImporting = signal(false);
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
      if (!vm.loading && !vm.error) {
        this.loadThumbnailUrls(vm.medications);
      }
    }),
    shareReplay({ bufferSize: 1, refCount: true })
  );

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
    return this.pharmacyApi.listMedications(this.buildRequest()).pipe(
      map((res) => this.toViewModel(res)),
      catchError((err) => {
        this.thumbnailUrls.set(new Map());
        this.toaster.error(getApiErrorMessage(err, 'Greška pri učitavanju lijekova.'));
        return of(this.emptyViewModel({ error: true }));
      }),
      startWith(this.emptyViewModel({ loading: true }))
    );
  }

  private toViewModel(res: {
    items: MedicationDto[];
    totalCount: number;
    totalPages: number;
    currentPage: number;
  }): MedicationsListViewModel {
    const now = new Date();
    const horizon = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);

    return {
      loading: false,
      error: false,
      medications: res.items,
      totalCount: res.totalCount,
      totalPages: res.totalPages,
      currentPage: res.currentPage,
      lowStockOnPageCount: res.items.filter(
        (m) => m.isActive && m.stockQuantity > 0 && m.stockQuantity < m.minimumStockLevel
      ).length,
      expiringSoonOnPageCount: res.items.filter((m) => {
        if (!m.expiryDate) return false;
        const expiry = new Date(m.expiryDate);
        return expiry >= now && expiry <= horizon;
      }).length,
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
      lowStockOnPageCount: 0,
      expiringSoonOnPageCount: 0,
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
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onSort(column);
    }
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
      isActive: this.showInactive ? undefined : true,
      includeInactive: this.showInactive,
      pageNumber: this.currentPage(),
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
    };
  }

  clearFilters(): void {
    this.search = '';
    this.selectedCategory = '';
    this.selectedStockStatus = '';
    this.selectedRequiresPrescription = '';
    this.showInactive = false;
    this.currentPage.set(1);
    this.loadTrigger$.next();
  }

  hasActiveFilters(): boolean {
    return !!(
      this.search ||
      this.selectedCategory ||
      this.selectedStockStatus ||
      this.selectedRequiresPrescription ||
      this.showInactive
    );
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

  onSearchInput(): void {
    this.searchChanged$.next();
    this.autocompleteQuery$.next(this.search);
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
    this.search = s.name;
    this.showAutocomplete.set(false);
    this.selectedSuggestionIndex.set(-1);
    this.activeSuggestionId.set(null);
    this.onFilterChange();
  }

  onFilterChange(): void {
    this.searchChanged$.next();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
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
    if (this.sortBy !== column) return 'none';
    return this.sortOrder === 'asc' ? 'ascending' : 'descending';
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
              this.toaster.error(getApiErrorMessage(err, 'Greška pri deaktivaciji lijeka.'));
            },
          });
      });
  }

  exportCsv(): void {
    this.pharmacyApi
      .exportMedicationsCsv(this.buildRequest())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const blob = res.body;
          if (!blob) return;
          const disposition = res.headers.get('content-disposition') ?? '';
          const match = disposition.match(/filename="?([^";]+)"?/i);
          this.downloadBlob(blob, match?.[1] ?? 'medications-export.csv');
        },
        error: (err) => {
          this.toaster.error(getApiErrorMessage(err, 'Greška pri exportu CSV.'));
        },
      });
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

  downloadTemplate(): void {
    this.pharmacyApi
      .downloadImportTemplate()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const blob = res.body;
          if (!blob) return;
          this.downloadBlob(blob, 'medication-import-template.csv');
        },
        error: (err) => this.toaster.error(getApiErrorMessage(err, 'Greška pri preuzimanju templatea.')),
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
          if (summary.successCount > 0) this.loadTrigger$.next();
          this.toaster.success(`Import: ${summary.successCount} uspješno, ${summary.failureCount} grešaka.`);
        },
        error: (err) => {
          this.isImporting.set(false);
          this.toaster.error(getApiErrorMessage(err, 'Greška pri importu CSV.'));
        },
      });
  }

  imageUrl(m: MedicationDto): string | null {
    const cached = this.thumbnailUrls().get(m.id);
    if (cached) {
      return cached;
    }
    if (m.primaryImageUrl) {
      return this.imageUrlService.getLegacyUrl(m.primaryImageUrl);
    }
    return null;
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
            if (medication.primaryImageUrl) {
              const legacy = this.imageUrlService.getLegacyUrl(medication.primaryImageUrl);
              if (legacy) {
                this.thumbnailUrls.update((map) => {
                  const next = new Map(map);
                  next.set(medication.id, legacy);
                  return next;
                });
              }
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
