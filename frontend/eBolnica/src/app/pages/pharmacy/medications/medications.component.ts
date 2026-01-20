import { Component, inject, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { PharmacyFilterService } from '../../../shared/services/pharmacy/pharmacy-filter.service';
import { FilterSummaryComponent } from '../../../shared/components/filter-summary/filter-summary.component';
import { ActiveFiltersComponent } from '../../../shared/components/active-filters/active-filters.component';
import { MedicationDto } from '../../../models/medication.dto';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { PagedResponse } from '../../../models/paged-response.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil, tap, combineLatest, catchError, of } from 'rxjs';

@Component({
  selector: 'app-medications',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FilterSummaryComponent, ActiveFiltersComponent],
  templateUrl: './medications.component.html',
  styleUrl: './medications.component.css'
})
export class MedicationsComponent implements OnInit, OnDestroy {
  protected pharmacyService = inject(PharmacyService);
  protected filterService = inject(PharmacyFilterService);

  medications: MedicationDto[] = [];
  isLoading: boolean = false;
  isSearching: boolean = false;
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

  // Available categories (populated from medications)
  categories: string[] = [];

  // Sort state
  sortColumn: string = 'createdAt'; // Default sort column
  sortOrder: 'asc' | 'desc' = 'desc'; // Default sort order

  // Active filters for display
  activeFilters = this.filterService.getActiveFilters();

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
      this.updateFilters({ searchTerm: searchTerm || undefined });
    });

    // Subscribe to filter changes from service
    this.filterService.getFilters$().pipe(
      debounceTime(150), // Additional debounce for combined filters
      switchMap(filters => {
        this.isSearching = true;
        this.errorMessage = null; // Clear previous errors
        this.syncUIFromFilters(filters);
        return this.pharmacyService.getMedicationsWithFilters(filters).pipe(
          finalize(() => this.isSearching = false),
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
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Sync UI state from filter service state
   */
  private syncUIFromFilters(filters: PharmacyFilters): void {
    this.searchTerm = filters.searchTerm || '';
    this.selectedCategory = filters.category || '';
    this.selectedStockStatus = filters.stockStatus || '';
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
   * Build PharmacyFilters from current UI state
   */
  private buildFiltersFromUI(): Partial<PharmacyFilters> {
    const filters: Partial<PharmacyFilters> = {
      pageNumber: this.currentPage,
      pageSize: this.pageSize
    };

    if (this.searchTerm?.trim()) {
      filters.searchTerm = this.searchTerm.trim();
    }

    if (this.selectedCategory) {
      filters.category = this.selectedCategory;
    }

    if (this.selectedStockStatus) {
      filters.stockStatus = this.selectedStockStatus.toLowerCase();
    }

    if (this.selectedRequiresPrescription) {
      filters.requiresPrescription = this.selectedRequiresPrescription === 'Yes';
    }

    if (this.selectedActiveStatus) {
      filters.isActive = this.selectedActiveStatus === 'Active';
    }

    // Add sort parameters
    if (this.sortColumn) {
      filters.sortBy = this.sortColumn;
    }
    if (this.sortOrder) {
      filters.sortOrder = this.sortOrder;
    }

    return filters;
  }

  /**
   * Update filters in service (triggers API call)
   */
  private updateFilters(updates: Partial<PharmacyFilters>): void {
    this.filterService.updateFilters(updates);
  }

  loadMedications(): void {
    const filters = this.buildFiltersFromUI();
    this.updateFilters(filters);
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
    this.updateFilters({ category: category || undefined });
  }

  onStockStatusChange(stockStatus: string): void {
    this.selectedStockStatus = stockStatus;
    this.updateFilters({ stockStatus: stockStatus ? stockStatus.toLowerCase() : undefined });
  }

  onRequiresPrescriptionChange(value: string): void {
    this.selectedRequiresPrescription = value;
    if (value) {
      this.updateFilters({ requiresPrescription: value === 'Yes' });
    } else {
      this.filterService.clearFilter('requiresPrescription');
    }
  }

  onActiveStatusChange(value: string): void {
    this.selectedActiveStatus = value;
    if (value) {
      this.updateFilters({ isActive: value === 'Active' });
    } else {
      this.filterService.clearFilter('isActive');
    }
  }

  onFilterChange(): void {
    // Legacy method - now handled by individual change handlers
    this.loadMedications();
  }

  /**
   * Clear all filters and reset to default state
   * Resets all UI controls and reloads data with default filters
   */
  clearFilters(): void {
    // Clear service state
    this.filterService.clearAllFilters();

    // Clear template-bound properties
    this.searchTerm = '';
    this.selectedCategory = '';
    this.selectedStockStatus = '';
    this.selectedRequiresPrescription = '';
    this.selectedActiveStatus = '';

    // Reset pagination to defaults
    this.currentPage = 1;
    this.pageSize = 10;

    // Update active filters display
    this.updateActiveFilters();

    // Show success feedback
    this.showClearSuccessMessage();

    // Data will be reloaded automatically via filterService subscription
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
    this.filterService.clearFilter(filterKey as keyof PharmacyFilters);
    
    // Update UI state
    switch (filterKey) {
      case 'searchTerm':
        this.searchTerm = '';
        break;
      case 'category':
        this.selectedCategory = '';
        break;
      case 'stockStatus':
        this.selectedStockStatus = '';
        break;
      case 'requiresPrescription':
        this.selectedRequiresPrescription = '';
        break;
      case 'isActive':
        this.selectedActiveStatus = '';
        break;
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
   * Handle column header sort click
   * Maps frontend column names to backend sort field names
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

    if (this.sortColumn === backendColumn) {
      // Toggle order if same column
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      // New column, default to ascending
      this.sortColumn = backendColumn;
      this.sortOrder = 'asc';
    }

    // Update filters with new sort parameters
    this.updateFilters({ 
      sortBy: this.sortColumn, 
      sortOrder: this.sortOrder,
      pageNumber: 1 // Reset to first page on sort
    });
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
