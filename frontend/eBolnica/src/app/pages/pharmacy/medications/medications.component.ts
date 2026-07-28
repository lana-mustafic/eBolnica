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
    // Initialize filters from service
    const currentFilters = this.filterService.getFilters();
    this.syncUIFromFilters(currentFilters);

    // Load initial data
    this.loadMedications();

    // Setup debounced search that combines with dropdown filters
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.pushFiltersFromUI();
    });

    // Subscribe to filter changes from service
    this.filterService.getFilters$().pipe(
      debounceTime(150), // Additional debounce for combined filters
      switchMap(filters => {
        this.isSearching = true;
        this.errorMessage = null; // Clear previous errors
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
        this.totalCount = response.totalCount || 0;
        this.totalPages = response.totalPages || 0;
        this.currentPage = response.currentPage || 1;
        this.pageSize = response.pageSize || 10;
        this.extractCategories();
        this.updateActiveFilters();
        this.errorMessage = null;
      }
    });
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
    this.currentPage = filters.pageNumber || 1;
    this.pageSize = filters.pageSize || 10;
    if (filters.sortBy) this.sortColumn = filters.sortBy;
    if (filters.sortOrder) this.sortOrder = filters.sortOrder as 'asc' | 'desc';
  }

  /**
   * Build PharmacyFilters from current UI state.
   * Explicit undefined values clear filters in PharmacyFilterService.
   */
  private buildFiltersFromUI(): Partial<PharmacyFilters> {
    return {
      pageNumber: this.currentPage,
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
   */
  private pushFiltersFromUI(): void {
    const filters = this.buildFiltersFromUI();
    this.priceFilterError = this.validatePriceRange(filters.minPrice, filters.maxPrice);

    if (this.priceFilterError) {
      return;
    }

    this.filterService.updateFilters(filters);
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
    this.currentPage = 1;
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
    this.currentPage = 1;
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

  getActiveFilterCount(): number {
    return this.filterService.getActiveFilterCount();
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
      this.currentPage = 1;
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
}
