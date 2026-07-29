import { Component, inject, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { PharmacyFilterService } from '../../../shared/services/pharmacy/pharmacy-filter.service';
import { FilterSummaryComponent } from '../../../shared/components/filter-summary/filter-summary.component';
import { ActiveFiltersComponent } from '../../../shared/components/active-filters/active-filters.component';
import { SortStatusComponent } from '../../../shared/components/sort-status/sort-status.component';
import { MedicationThumbnailComponent } from './medication-thumbnail.component';
import { MedicationDto } from '../../../models/medication.dto';
import { ActiveFilter, PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { PagedResponse } from '../../../models/paged-response.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil, catchError, of } from 'rxjs';
import { TABLE_DEFAULT_SORTS } from '../../../constants/sort.constants';
import { getPageRangeEnd, getPageRangeStart } from '../../../shared/utils/paged-response.util';

/** Debounce delay for medication search input before combining with other filters */
const MEDICATION_SEARCH_DEBOUNCE_MS = 300;

/** CSV columns used for medication import (and export, except Status on export-only). */
const MEDICATION_IMPORT_CSV_HEADERS = [
  'Name',
  'Generic Name',
  'Category',
  'Manufacturer',
  'Description',
  'Price',
  'Stock Quantity',
  'Minimum Stock Level',
  'Expiry Date',
  'Batch Number',
  'Dosage Form',
  'Strength',
  'Requires Prescription',
  'Active'
] as const;

@Component({
  selector: 'app-medications',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FilterSummaryComponent, ActiveFiltersComponent, SortStatusComponent, MedicationThumbnailComponent],
  templateUrl: './medications.component.html',
  styleUrl: './medications.component.css'
})
export class MedicationsComponent implements OnInit, OnDestroy {
  protected pharmacyService = inject(PharmacyService);
  protected filterService = inject(PharmacyFilterService);

  medications: MedicationDto[] = [];
  isLoading: boolean = false;
  isSearching: boolean = false;
  isSorting: boolean = false; // Specific flag for sort operations
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Pagination
  currentPage: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  totalPages: number = 0;

  // Search
  searchTerm: string = '';
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  // Filters (UI state)
  selectedCategory: string = '';
  selectedStockStatus: string = '';
  selectedRequiresPrescription: string = '';
  selectedActiveStatus: string = '';

  /**
   * RS1 requires 5+ functional filter parameters. The medications list already exposes five:
   * search, category, stock status, requires prescription, and active/inactive.
   * Price range UI is intentionally omitted; backend support (minPrice/maxPrice) remains available.
   */
  priceFilterError: string | null = null;

  // Available categories (populated from medications)
  categories: string[] = [];

  // Sort state (default: newest first)
  sortColumn: string = TABLE_DEFAULT_SORTS.MEDICATIONS.column;
  sortOrder: 'asc' | 'desc' = TABLE_DEFAULT_SORTS.MEDICATIONS.order;
  private previousSortColumn: string = TABLE_DEFAULT_SORTS.MEDICATIONS.column; // For error recovery
  private previousSortOrder: 'asc' | 'desc' = TABLE_DEFAULT_SORTS.MEDICATIONS.order; // For error recovery
  private sortDebounceTimer: any; // Timer for debouncing sort requests

  // Active filters for display
  activeFilters: ActiveFilter[] = this.filterService.getActiveFilters();

  // Success message for clear operation
  clearSuccessMessage: string | null = null;

  ngOnInit(): void {
    this.syncUIFromFilters(this.filterService.getFilters());

    // Debounced search: waits 300ms, then pushes full filter set (search + dropdowns)
    this.searchSubject.pipe(
      debounceTime(MEDICATION_SEARCH_DEBOUNCE_MS),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.pushFiltersFromUI();
    });

    // Single API pipeline — all filter changes (search + dropdowns + paging + sort) flow here
    this.filterService.getFilters$().pipe(
      switchMap(filters => {
        this.isSearching = true;
        this.errorMessage = null;
        this.syncUIFromFilters(filters);
        return this.pharmacyService.getMedicationsWithFilters(filters).pipe(
          finalize(() => {
            this.isSearching = false;
            this.isSorting = false;
          }),
          catchError((error) => {
            this.handleApiError(error);
            return of({
              items: [],
              totalCount: 0,
              totalPages: 0,
              currentPage: 1,
              pageSize: filters.pageSize || 10,
              hasNext: false,
              hasPrevious: false
            } as PagedResponse<MedicationDto>);
          })
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        this.medications = response.items || [];
        this.applyPaginationFromResponse(response);
        this.extractCategories();
        this.updateActiveFilters();
        this.errorMessage = null;
      }
    });

    // Initial load (one emission; debounce in getFilters$ coalesces with subscription setup)
    this.pushFiltersFromUI();
  }

  ngOnDestroy(): void {
    // Clear sort debounce timer
    if (this.sortDebounceTimer) {
      clearTimeout(this.sortDebounceTimer);
    }

    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Sync UI state from filter service state
   */
  private syncUIFromFilters(filters: PharmacyFilters): void {
    this.searchTerm = filters.searchTerm || '';
    this.selectedCategory = filters.category || '';
    this.selectedStockStatus = this.toStockStatusDisplayLabel(filters.stockStatus);
    this.selectedRequiresPrescription = filters.requiresPrescription !== undefined 
      ? (filters.requiresPrescription ? 'Yes' : 'No') 
      : '';
    this.selectedActiveStatus = filters.isActive !== undefined
      ? (filters.isActive ? 'Active' : 'Inactive')
      : '';
    if (filters.sortBy) this.sortColumn = filters.sortBy;
    if (filters.sortOrder) this.sortOrder = filters.sortOrder as 'asc' | 'desc';
  }

  private applyPaginationFromResponse(response: PagedResponse<MedicationDto>): void {
    this.totalCount = response.totalCount;
    this.totalPages = response.totalPages;
    this.currentPage = response.currentPage;
    this.pageSize = response.pageSize;
    this.filterService.syncPaginationFromResponse(response.currentPage, response.pageSize);
  }

  get paginationRangeStart(): number {
    return getPageRangeStart(this.currentPage, this.pageSize, this.totalCount);
  }

  get paginationRangeEnd(): number {
    return getPageRangeEnd(this.currentPage, this.pageSize, this.totalCount);
  }

  /**
   * Build PharmacyFilters from current UI state.
   * Explicit undefined values clear filters in PharmacyFilterService.
   */
  private buildFiltersFromUI(): Partial<PharmacyFilters> {
    return {
      pageSize: this.pageSize,
      searchTerm: this.searchTerm?.trim() || undefined,
      category: this.selectedCategory || undefined,
      stockStatus: this.selectedStockStatus
        ? this.selectedStockStatus.toLowerCase()
        : undefined,
      requiresPrescription: this.selectedRequiresPrescription
        ? this.selectedRequiresPrescription === 'Yes'
        : undefined,
      isActive: this.selectedActiveStatus
        ? this.selectedActiveStatus === 'Active'
        : undefined,
      minPrice: undefined,
      maxPrice: undefined,
      sortBy: this.sortColumn || undefined,
      sortOrder: this.sortOrder || undefined
    };
  }

  /**
   * Push all current UI filter values to PharmacyFilterService (triggers API reload).
   * Skips update when the serialized filter set is unchanged to avoid duplicate API calls.
   */
  private pushFiltersFromUI(): void {
    const filters = this.buildFiltersFromUI();
    this.priceFilterError = this.validatePriceRange(filters.minPrice, filters.maxPrice);

    if (this.priceFilterError) {
      return;
    }

    if (this.filtersMatchServiceState(filters)) {
      return;
    }

    this.filterService.updateFilters(filters);
  }

  private filtersMatchServiceState(filters: Partial<PharmacyFilters>): boolean {
    return this.serializeMedicationFilters(filters)
      === this.serializeMedicationFilters(this.filterService.getFilters());
  }

  private serializeMedicationFilters(filters: Partial<PharmacyFilters>): string {
    return JSON.stringify({
      searchTerm: filters.searchTerm?.trim() || undefined,
      category: filters.category || undefined,
      stockStatus: filters.stockStatus || undefined,
      requiresPrescription: filters.requiresPrescription,
      isActive: filters.isActive,
      minPrice: filters.minPrice,
      maxPrice: filters.maxPrice,
      pageSize: filters.pageSize ?? 10,
      sortBy: filters.sortBy,
      sortOrder: filters.sortOrder
    });
  }

  /**
   * Validates optional price range filters before sending them to the backend.
   */
  private validatePriceRange(minPrice?: number, maxPrice?: number): string | null {
    const hasMin = minPrice !== undefined && minPrice !== null;
    const hasMax = maxPrice !== undefined && maxPrice !== null;

    if (!hasMin && !hasMax) {
      return null;
    }

    if (hasMin && minPrice! < 0) {
      return 'Minimum price cannot be negative.';
    }

    if (hasMax && maxPrice! < 0) {
      return 'Maximum price cannot be negative.';
    }

    if (hasMin && hasMax && minPrice! > maxPrice!) {
      return 'Minimum price cannot be greater than maximum price.';
    }

    return null;
  }

  /**
   * Update filters in service (triggers API call)
   */
  private updateFilters(updates: Partial<PharmacyFilters>): void {
    this.filterService.updateFilters(updates);
  }

  loadMedications(): void {
    this.pushFiltersFromUI();
  }

  private toStockStatusDisplayLabel(value?: string): string {
    if (!value) {
      return '';
    }

    const labels: Record<string, string> = {
      'low stock': 'Low Stock',
      'out of stock': 'Out of Stock',
      'normal stock': 'Normal Stock'
    };

    return labels[value.toLowerCase()] ?? value;
  }

  private extractCategories(): void {
    const categorySet = new Set<string>();
    this.medications.forEach(med => {
      if (med.category) {
        categorySet.add(med.category);
      }
    });
    this.categories = Array.from(categorySet).sort();
  }

  onSearchChange(searchTerm: string): void {
    // Update input immediately; API call is debounced via searchSubject → pushFiltersFromUI
    this.searchTerm = searchTerm;
    this.searchSubject.next(searchTerm);
  }

  onCategoryChange(category: string): void {
    this.selectedCategory = category;
    this.pushFiltersFromUI();
  }

  onStockStatusChange(stockStatus: string): void {
    this.selectedStockStatus = stockStatus;
    this.pushFiltersFromUI();
  }

  onRequiresPrescriptionChange(value: string): void {
    this.selectedRequiresPrescription = value;
    this.pushFiltersFromUI();
  }

  onActiveStatusChange(value: string): void {
    this.selectedActiveStatus = value;
    this.pushFiltersFromUI();
  }

  /**
   * Clear all filters and reset to default state
   * Resets all UI controls and reloads data with default filters
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategory = '';
    this.selectedStockStatus = '';
    this.selectedRequiresPrescription = '';
    this.selectedActiveStatus = '';
    this.priceFilterError = null;
    this.pageSize = 10;
    this.resetSortingToDefault();

    this.filterService.clearAllFilters();
    this.pushFiltersFromUI();

    this.updateActiveFilters();
    this.showClearSuccessMessage();
  }

  /**
   * Reset sorting to default (newest first)
   */
  resetSortingToDefault(): void {
    this.sortColumn = TABLE_DEFAULT_SORTS.MEDICATIONS.column;
    this.sortOrder = TABLE_DEFAULT_SORTS.MEDICATIONS.order;
  }

  /**
   * Check if current sort is default sort
   */
  isDefaultSort(): boolean {
    return this.sortColumn === TABLE_DEFAULT_SORTS.MEDICATIONS.column &&
           this.sortOrder === TABLE_DEFAULT_SORTS.MEDICATIONS.order;
  }

  /**
   * Get display name for current sort column
   */
  getSortDisplayName(): string {
    const columnNames: { [key: string]: string } = {
      'name': 'Name',
      'category': 'Category',
      'price': 'Price',
      'stockQuantity': 'Stock Quantity',
      'createdAt': 'Date Created',
      'updatedAt': 'Date Updated'
    };
    return columnNames[this.sortColumn] || this.sortColumn;
  }

  /**
   * Reset to default sort and reload data
   */
  resetToDefaultSort(): void {
    this.resetSortingToDefault();
    this.pushFiltersFromUI();
  }

  /**
   * Show success message after clearing filters
   */
  private showClearSuccessMessage(): void {
    this.clearSuccessMessage = 'All filters cleared. Showing all results.';
    setTimeout(() => {
      this.clearSuccessMessage = null;
    }, 3000);
  }

  /**
   * Keyboard shortcut handler for clearing filters
   * Ctrl+Shift+C or Escape (when filters are active)
   */
  @HostListener('document:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent): void {
    const hasActiveFilters = this.getActiveFilterCount() > 0;
    
    // Ctrl+Shift+C to clear all filters
    if (event.ctrlKey && event.shiftKey && event.key === 'C' && hasActiveFilters) {
      event.preventDefault();
      this.clearFilters();
    }
    
    // Escape key to clear filters when active
    if (event.key === 'Escape' && hasActiveFilters && 
        !(event.target instanceof HTMLInputElement && (event.target as HTMLInputElement).type === 'text')) {
      // Don't clear if user is typing in an input field
      const target = event.target as HTMLElement;
      if (target.tagName !== 'INPUT' && target.tagName !== 'TEXTAREA') {
        event.preventDefault();
        this.clearFilters();
      }
    }
  }

  removeFilter(filterKey: string): void {
    const uiResetters: Record<string, () => void> = {
      searchTerm: () => { this.searchTerm = ''; },
      category: () => { this.selectedCategory = ''; },
      stockStatus: () => { this.selectedStockStatus = ''; },
      requiresPrescription: () => { this.selectedRequiresPrescription = ''; },
      isActive: () => { this.selectedActiveStatus = ''; }
    };

    if (uiResetters[filterKey]) {
      uiResetters[filterKey]();
      this.pushFiltersFromUI();
    } else {
      this.filterService.clearFilterByBadgeKey(filterKey);
      this.syncUIFromFilters(this.filterService.getFilters());
    }

    this.updateActiveFilters();
  }

  // Pagination methods
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.updateFilters({ pageNumber: page });
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }

  changePageSize(size: number): void {
    this.updateFilters({ pageSize: size, pageNumber: 1 });
  }

  getPageNumbers(): (number | string)[] {
    const pages: (number | string)[] = [];
    const maxVisiblePages = 7;

    if (this.totalPages <= maxVisiblePages) {
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      if (this.currentPage <= 4) {
        for (let i = 1; i <= 5; i++) {
          pages.push(i);
        }
        pages.push('...');
        pages.push(this.totalPages);
      } else if (this.currentPage >= this.totalPages - 3) {
        pages.push(1);
        pages.push('...');
        for (let i = this.totalPages - 4; i <= this.totalPages; i++) {
          pages.push(i);
        }
      } else {
        pages.push(1);
        pages.push('...');
        pages.push(this.currentPage - 1);
        pages.push(this.currentPage);
        pages.push(this.currentPage + 1);
        pages.push('...');
        pages.push(this.totalPages);
      }
    }

    return pages;
  }

  onPageNumberClick(pageNum: number | string): void {
    if (typeof pageNum === 'number') {
      this.goToPage(pageNum);
    }
  }

  isEllipsis(pageNum: number | string): boolean {
    return pageNum === '...';
  }

  trackByPageNum(index: number, pageNum: number | string): number | string {
    return pageNum;
  }

  trackByMedicationId(_: number, medication: MedicationDto): number {
    return medication.id;
  }

  trackByFilterKey(_: number, filter: ActiveFilter): string {
    return filter.key;
  }

  getActiveFilterCount(): number {
    return this.filterService.getActiveFilterCount();
  }

  hasActiveFilters(): boolean {
    return this.getActiveFilterCount() > 0;
  }

  getEmptyStateTitle(): string {
    return this.hasActiveFilters()
      ? 'No medications match your filters'
      : 'No medications yet';
  }

  getEmptyStateDescription(): string {
    if (this.hasActiveFilters()) {
      const filterWord = this.getActiveFilterCount() === 1 ? 'filter' : 'filters';
      return `None of your medications match the current ${this.getActiveFilterCount()} active ${filterWord}. Try removing a filter or broadening your search.`;
    }

    return 'Get started by adding your first medication to the pharmacy inventory.';
  }

  updateActiveFilters(): void {
    this.activeFilters = this.filterService.getActiveFilters();
  }

  deleteMedication(medication: MedicationDto): void {
    if (confirm(`Are you sure you want to delete "${medication.name}"? This action cannot be undone.`)) {
      this.isLoading = true;
      this.errorMessage = null;
      this.successMessage = null;

      this.pharmacyService.deleteMedication(medication.id).subscribe({
        next: () => {
          this.isLoading = false;
          this.successMessage = `Medication "${medication.name}" deleted successfully.`;
          this.loadMedications();
          setTimeout(() => this.successMessage = null, 3000);
        },
        error: (error) => {
          this.isLoading = false;
          if (error.error?.message) {
            this.errorMessage = error.error.message;
          } else {
            this.errorMessage = 'Failed to delete medication. Please try again.';
          }
          console.error('Error deleting medication:', error);
        }
      });
    }
  }

  getStockStatus(medication: MedicationDto): { label: string; class: string } {
    if (!medication.isActive) {
      return { label: 'Inactive', class: 'status-inactive' };
    }
    
    if (medication.expiryDate) {
      const expiry = new Date(medication.expiryDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (expiry < today) {
        return { label: 'Expired', class: 'status-expired' };
      }
    }

    if (medication.stockQuantity === 0) {
      return { label: 'Out of Stock', class: 'status-out-of-stock' };
    }

    if (medication.stockQuantity < medication.minimumStockLevel) {
      return { label: 'Low Stock', class: 'status-low-stock' };
    }

    return { label: 'Active', class: 'status-active' };
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  // Expose Math to template
  Math = Math;

  /**
   * Handle column header sort click - SERVER-SIDE SORTING
   * Maps frontend column names to backend sort field names
   * Includes debouncing and request cancellation
   */
  onSort(column: string): void {
    // Map frontend column names to backend field names
    const columnMapping: { [key: string]: string } = {
      'name': 'name',
      'category': 'category',
      'price': 'price',
      'stockQuantity': 'stockQuantity',
      'stock': 'stockQuantity',
      'status': 'name', // Status is derived, sort by name
      'createdAt': 'createdAt',
      'createdDate': 'createdAt',
      'dateCreated': 'createdAt'
    };

    const backendColumn = columnMapping[column] || column;

    // Store previous sort state for error recovery
    this.previousSortColumn = this.sortColumn;
    this.previousSortOrder = this.sortOrder;

    if (this.sortColumn === backendColumn) {
      // Toggle order if same column
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      // New column, default to ascending
      this.sortColumn = backendColumn;
      this.sortOrder = 'asc';
    }

    // Set sorting flag
    this.isSorting = true;
    this.errorMessage = null;

    // Cancel any pending sort debounce
    if (this.sortDebounceTimer) {
      clearTimeout(this.sortDebounceTimer);
    }

    // Debounce sort requests (200ms) to avoid rapid API calls
    this.sortDebounceTimer = setTimeout(() => {
      this.pushFiltersFromUI();
    }, 200);
  }

  /**
   * Handle sort-specific errors
   */
  private handleSortError(error: any): void {
    console.error('Sort failed:', error);
    
    // Revert to previous sort state
    this.revertSortState();
    
    // Show user-friendly error
    if (error?.status === 400) {
      this.errorMessage = 'Invalid sort column. Please try a different column.';
      console.warn('Invalid sort parameters:', error.error);
    } else if (error?.status === 0) {
      this.errorMessage = 'Network error. Unable to sort medications. Please check your connection.';
    } else {
      this.errorMessage = 'Unable to sort medications. Please try again.';
    }
    
    this.isSorting = false;
  }

  /**
   * Revert sort state to previous values or default
   */
  private revertSortState(): void {
    this.sortColumn = this.previousSortColumn || TABLE_DEFAULT_SORTS.MEDICATIONS.column;
    this.sortOrder = this.previousSortOrder || TABLE_DEFAULT_SORTS.MEDICATIONS.order;
  }

  /**
   * Get sort icon class for a column
   */
  getSortIconClass(column: string, direction: 'asc' | 'desc'): string {
    const columnMapping: { [key: string]: string } = {
      'name': 'name',
      'category': 'category',
      'price': 'price',
      'stockQuantity': 'stockQuantity',
      'stock': 'stockQuantity',
      'status': 'name',
      'createdAt': 'createdAt',
      'createdDate': 'createdAt',
      'dateCreated': 'createdAt'
    };

    const backendColumn = columnMapping[column] || column;
    if (this.sortColumn !== backendColumn) return '';
    return this.sortOrder === direction ? 'active' : '';
  }

  /**
   * Get aria-sort attribute value
   */
  getAriaSort(column: string): string {
    const columnMapping: { [key: string]: string } = {
      'name': 'name',
      'category': 'category',
      'price': 'price',
      'stockQuantity': 'stockQuantity',
      'stock': 'stockQuantity',
      'status': 'name',
      'createdAt': 'createdAt',
      'createdDate': 'createdAt',
      'dateCreated': 'createdAt'
    };

    const backendColumn = columnMapping[column] || column;
    if (this.sortColumn !== backendColumn) return 'none';
    return this.sortOrder === 'asc' ? 'ascending' : 'descending';
  }

  /**
   * Handle keyboard events for sort headers
   */
  onSortKeydown(event: KeyboardEvent, column: string): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onSort(column);
    }
  }

  /**
   * Handle API errors with user-friendly messages
   */
  private handleApiError(error: any): void {
    this.isSearching = false;
    this.isLoading = false;

    if (error?.message) {
      this.errorMessage = error.message;
    } else if (error?.status === 0) {
      this.errorMessage = 'Network error. Please check your connection and try again.';
    } else if (error?.status === 400) {
      this.errorMessage = 'Invalid filters. Please adjust your search criteria.';
    } else if (error?.status === 401 || error?.status === 403) {
      this.errorMessage = 'Access denied. Please log in again.';
    } else if (error?.status === 500) {
      this.errorMessage = 'Server error. Please try again later.';
    } else {
      this.errorMessage = 'Failed to load medications. Please try again.';
    }

    console.error('[MedicationsComponent] Error loading medications:', error);
  }

  /**
   * Retry loading medications after an error
   */
  retryLoad(): void {
    this.errorMessage = null;
    this.loadMedications();
  }

  /**
   * Export medications on the **current page** to CSV.
   * Respects active filters and sort order; does not fetch all pages.
   */
  exportToCSV(): void {
    if (this.medications.length === 0) {
      return;
    }

    const headers = [
      ...MEDICATION_IMPORT_CSV_HEADERS,
      'Status'
    ];

    const csvData = this.medications.map(medication => [
      this.escapeCSV(medication.name),
      this.escapeCSV(medication.genericName || ''),
      this.escapeCSV(medication.category || ''),
      this.escapeCSV(medication.manufacturer || ''),
      this.escapeCSV(medication.description || ''),
      medication.price.toString(),
      medication.stockQuantity.toString(),
      medication.minimumStockLevel.toString(),
      medication.expiryDate ? this.formatDateForCsv(medication.expiryDate) : '',
      this.escapeCSV(medication.batchNumber || ''),
      this.escapeCSV(medication.dosageForm || ''),
      this.escapeCSV(medication.strength || ''),
      medication.requiresPrescription ? 'Yes' : 'No',
      medication.isActive ? 'Yes' : 'No',
      this.escapeCSV(this.getStockStatus(medication).label)
    ]);

    const csvContent = [headers, ...csvData]
      .map(row => row.join(','))
      .join('\n');

    const today = new Date().toISOString().split('T')[0];
    this.downloadCSV(csvContent, `pharmacy-medications-${today}.csv`);
  }

  /**
   * Download import template with required headers and one example row (validation hints in cells).
   */
  downloadCsvTemplate(): void {
    const exampleRow = [
      'Paracetamol (required, 3-100 characters)',
      'Acetaminophen (optional)',
      'Painkillers (required)',
      'PharmaCorp (optional)',
      'Pain reliever (optional, max 500 characters)',
      '9.99 (required, > 0)',
      '100 (required, integer >= 0)',
      '20 (required, integer >= 0)',
      '2026-12-31 (required, YYYY-MM-DD, must be future date)',
      'BATCH-001 (optional)',
      'Tablet (optional)',
      '500mg (optional)',
      'No (required: Yes or No)',
      'Yes (required: Yes or No)'
    ].map(value => this.escapeCSV(value));

    const csvContent = [MEDICATION_IMPORT_CSV_HEADERS.join(','), exampleRow.join(',')].join('\n');
    this.downloadCSV(csvContent, 'medication-import-template.csv');
  }

  private formatDateForCsv(dateString: string): string {
    const date = new Date(dateString);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return date.toISOString().split('T')[0];
  }

  private escapeCSV(value: string): string {
    if (!value) {
      return '';
    }

    if (value.includes(',') || value.includes('"') || value.includes('\n')) {
      return `"${value.replace(/"/g, '""')}"`;
    }

    return value;
  }

  private downloadCSV(content: string, filename: string): void {
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }
}
