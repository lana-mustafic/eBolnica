import { Component, DestroyRef, ElementRef, inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import {
  MedicationAutocompleteSuggestion,
  MedicationDto,
  MedicationImportResult,
} from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { MedicationImageUrlService } from '../services/medication-image-url.service';

@Component({
  selector: 'app-pharmacy-medications',
  standalone: false,
  templateUrl: './pharmacy-medications.component.html',
  styleUrl: './pharmacy-medications.component.scss',
})
export class PharmacyMedicationsComponent implements OnInit, OnDestroy {
  @ViewChild('csvInput') csvInput?: ElementRef<HTMLInputElement>;

  private pharmacyApi = inject(PharmacyApiService);
  private imageUrlService = inject(MedicationImageUrlService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);

  medications: MedicationDto[] = [];
  isLoading = false;
  loadError = false;
  isImporting = false;
  totalCount = 0;
  totalPages = 0;
  currentPage = 1;
  pageSize = 10;

  search = '';
  selectedCategory = '';
  selectedStockStatus = '';
  selectedRequiresPrescription = '';
  showInactive = false;

  sortBy = 'createdAt';
  sortOrder: 'asc' | 'desc' = 'desc';

  autocompleteSuggestions: MedicationAutocompleteSuggestion[] = [];
  showAutocomplete = false;
  importSummary: MedicationImportResult | null = null;
  thumbnailUrls = new Map<number, string>();

  private searchChanged$ = new Subject<void>();
  private autocompleteQuery$ = new Subject<string>();
  private loadTrigger$ = new Subject<void>();

  displayedColumns = ['name', 'category', 'price', 'stockQuantity', 'expiryDate', 'status', 'actions'];

  ngOnInit(): void {
    this.loadTrigger$
      .pipe(
        switchMap(() => {
          this.isLoading = true;
          this.loadError = false;
          return this.pharmacyApi.listMedications(this.buildRequest()).pipe(
            catchError(() => {
              this.loadError = true;
              this.medications = [];
              this.thumbnailUrls.clear();
              this.toaster.error('Greška pri učitavanju lijekova.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((res) => {
        this.isLoading = false;
        if (!res) {
          return;
        }

        this.medications = res.items;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.currentPage = res.currentPage;
        this.loadThumbnailUrls();
      });

    this.searchChanged$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadTrigger$.next();
      });

    this.autocompleteQuery$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((q) => {
          if (q.trim().length < 2) {
            this.showAutocomplete = false;
            return of([] as MedicationAutocompleteSuggestion[]);
          }
          return this.pharmacyApi.getAutocomplete(q);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((suggestions) => {
        this.autocompleteSuggestions = suggestions;
        this.showAutocomplete = suggestions.length > 0;
      });

    this.loadTrigger$.next();
  }

  ngOnDestroy(): void {
    this.imageUrlService.revokeAll();
  }

  loadMedications(): void {
    this.loadTrigger$.next();
  }

  retryLoad(): void {
    this.loadTrigger$.next();
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
      pageNumber: this.currentPage,
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
    this.currentPage = 1;
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
    this.currentPage = 1;
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

  selectSuggestion(s: MedicationAutocompleteSuggestion): void {
    this.search = s.name;
    this.showAutocomplete = false;
    this.onFilterChange();
  }

  onFilterChange(): void {
    this.searchChanged$.next();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
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

  deleteMedication(medication: MedicationDto): void {
    if (!confirm(`Deaktivirati lijek "${medication.name}"?`)) return;
    this.pharmacyApi.deleteMedication(medication.id).subscribe({
      next: () => {
        this.toaster.success('Lijek deaktiviran.');
        if (this.medications.length === 1 && this.currentPage > 1) {
          this.currentPage--;
        }
        this.loadTrigger$.next();
      },
      error: () => this.toaster.error('Greška pri brisanju lijeka.'),
    });
  }

  exportCsv(): void {
    this.pharmacyApi.exportMedicationsCsv(this.buildRequest()).subscribe({
      next: (res) => {
        const blob = res.body;
        if (!blob) return;
        const disposition = res.headers.get('content-disposition') ?? '';
        const match = disposition.match(/filename="?([^";]+)"?/i);
        this.downloadBlob(blob, match?.[1] ?? 'medications-export.csv');
      },
      error: () => this.toaster.error('Greška pri exportu CSV.'),
    });
  }

  exportPdf(): void {
    this.pharmacyApi.exportInventoryPdf(this.buildRequest()).subscribe({
      next: (res) => {
        this.pharmacyApi.downloadBlobResponse(res, 'inventory.pdf');
        this.toaster.success('PDF inventar preuzet.');
      },
      error: () => this.toaster.error('Greška pri exportu PDF.'),
    });
  }

  downloadTemplate(): void {
    this.pharmacyApi.downloadImportTemplate().subscribe({
      next: (res) => {
        const blob = res.body;
        if (!blob) return;
        this.downloadBlob(blob, 'medication-import-template.csv');
      },
      error: () => this.toaster.error('Greška pri preuzimanju templatea.'),
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

    this.isImporting = true;
    this.importSummary = null;
    this.pharmacyApi.importMedicationsCsv(file).subscribe({
      next: (summary) => {
        this.isImporting = false;
        this.importSummary = summary;
        if (summary.successCount > 0) this.loadTrigger$.next();
        this.toaster.success(`Import: ${summary.successCount} uspješno, ${summary.failureCount} grešaka.`);
      },
      error: (err) => {
        this.isImporting = false;
        this.toaster.error(err?.error?.message ?? 'Greška pri importu CSV.');
      },
    });
  }

  imageUrl(m: MedicationDto): string | null {
    const cached = this.thumbnailUrls.get(m.id);
    if (cached) {
      return cached;
    }
    if (m.primaryImageUrl) {
      return this.imageUrlService.getLegacyUrl(m.primaryImageUrl);
    }
    return null;
  }

  onImageError(medicationId: number): void {
    this.thumbnailUrls.delete(medicationId);
  }

  getStockStatus(m: MedicationDto): string {
    if (!m.isActive) return 'Neaktivan';
    if (m.stockQuantity === 0) return 'Nema na stanju';
    if (m.stockQuantity < m.minimumStockLevel) return 'Nizak nivo';
    return 'OK';
  }

  getStockStatusClass(m: MedicationDto): string {
    const status = this.getStockStatus(m);
    if (status === 'Nizak nivo') return 'low';
    if (status === 'Nema na stanju') return 'out';
    if (status === 'Neaktivan') return 'inactive';
    return 'ok';
  }

  private loadThumbnailUrls(): void {
    this.imageUrlService.revokeAll();
    this.thumbnailUrls.clear();

    for (const medication of this.medications) {
      if (!medication.primaryImageId) {
        continue;
      }

      this.imageUrlService
        .getAuthenticatedUrl(medication.id, medication.primaryImageId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (url) => this.thumbnailUrls.set(medication.id, url),
          error: () => {
            if (medication.primaryImageUrl) {
              const legacy = this.imageUrlService.getLegacyUrl(medication.primaryImageUrl);
              if (legacy) {
                this.thumbnailUrls.set(medication.id, legacy);
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
