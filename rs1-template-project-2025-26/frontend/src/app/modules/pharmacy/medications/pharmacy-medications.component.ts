import { Component, ElementRef, inject, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import {
  MedicationAutocompleteSuggestion,
  MedicationDto,
  MedicationImportResult,
} from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-pharmacy-medications',
  standalone: false,
  templateUrl: './pharmacy-medications.component.html',
  styleUrl: './pharmacy-medications.component.scss',
})
export class PharmacyMedicationsComponent implements OnInit {
  @ViewChild('csvInput') csvInput?: ElementRef<HTMLInputElement>;

  private pharmacyApi = inject(PharmacyApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);

  medications: MedicationDto[] = [];
  isLoading = false;
  isImporting = false;
  totalCount = 0;
  totalPages = 0;
  currentPage = 1;
  pageSize = 10;

  search = '';
  selectedCategory = '';
  selectedStockStatus = '';
  showInactive = false;

  autocompleteSuggestions: MedicationAutocompleteSuggestion[] = [];
  showAutocomplete = false;
  importSummary: MedicationImportResult | null = null;

  private searchChanged$ = new Subject<void>();
  private autocompleteQuery$ = new Subject<string>();

  displayedColumns = ['name', 'category', 'price', 'stockQuantity', 'expiryDate', 'status', 'actions'];

  ngOnInit(): void {
    this.searchChanged$.pipe(debounceTime(300)).subscribe(() => {
      this.currentPage = 1;
      this.loadMedications();
    });

    this.autocompleteQuery$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((q) => {
          if (q.trim().length < 2) {
            this.showAutocomplete = false;
            return of([]);
          }
          return this.pharmacyApi.getAutocomplete(q);
        })
      )
      .subscribe((suggestions) => {
        this.autocompleteSuggestions = suggestions;
        this.showAutocomplete = suggestions.length > 0;
      });

    this.loadMedications();
  }

  loadMedications(): void {
    this.isLoading = true;
    this.pharmacyApi.listMedications(this.buildRequest()).subscribe({
      next: (res) => {
        this.medications = res.items;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.currentPage = res.currentPage;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toaster.error('Greška pri učitavanju lijekova.');
      },
    });
  }

  private buildRequest() {
    return {
      search: this.search || undefined,
      category: this.selectedCategory || undefined,
      stockStatus: this.selectedStockStatus || undefined,
      isActive: this.showInactive ? undefined : true,
      includeInactive: this.showInactive,
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      sortBy: 'createdAt',
      sortOrder: 'desc' as const,
    };
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
    this.loadMedications();
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
        this.loadMedications();
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
        if (summary.successCount > 0) this.loadMedications();
        this.toaster.success(`Import: ${summary.successCount} uspješno, ${summary.failureCount} grešaka.`);
      },
      error: (err) => {
        this.isImporting = false;
        this.toaster.error(err?.error?.message ?? 'Greška pri importu CSV.');
      },
    });
  }

  imageUrl(m: MedicationDto): string | null {
    return m.primaryImageUrl ? this.pharmacyApi.imageFullUrl(m.primaryImageUrl) : null;
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

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
