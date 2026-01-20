import { Component, inject, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { PharmacyFilterService } from '../../../shared/services/pharmacy/pharmacy-filter.service';
import { FilterSummaryComponent } from '../../../shared/components/filter-summary/filter-summary.component';
import { ActiveFiltersComponent } from '../../../shared/components/active-filters/active-filters.component';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { PagedResponse } from '../../../models/paged-response.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil, tap, catchError, of } from 'rxjs';

@Component({
  selector: 'app-prescriptions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FilterSummaryComponent, ActiveFiltersComponent],
  templateUrl: './prescriptions.component.html',
  styleUrl: './prescriptions.component.css'
})
export class PrescriptionsComponent implements OnInit, OnDestroy {
  protected pharmacyService = inject(PharmacyService);
  protected filterService = inject(PharmacyFilterService);

  prescriptions: PrescriptionDto[] = [];
  isLoading: boolean = false;
  isSearching: boolean = false;
  errorMessage: string | null = null;

  // Pagination
  currentPage: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  totalPages: number = 0;

  // Status filter
  selectedStatus: string = 'Pending';
  statusFilters = [
    { value: 'All', label: 'All Prescriptions', count: 0 },
    { value: 'Pending', label: 'Pending', count: 0 },
    { value: 'Dispensed', label: 'Dispensed', count: 0 },
    { value: 'Cancelled', label: 'Cancelled', count: 0 }
  ];

  // Search
  searchTerm: string = '';
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  // Sort
  sortBy: string = 'date';
  sortOrder: 'asc' | 'desc' = 'desc';
  sortColumn: string = 'prescribedDate'; // Default sort column for header clicks

  // Active filters for display
  activeFilters = this.filterService.getActiveFilters();

  // Success message for clear operation
  clearSuccessMessage: string | null = null;

  ngOnInit(): void {
    // Initialize filters from service
    const currentFilters = this.filterService.getFilters();
    this.syncUIFromFilters(currentFilters);

    // Load initial data
    this.loadPrescriptions();

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
      debounceTime(150),
      switchMap(filters => {
        this.isSearching = true;
        this.errorMessage = null; // Clear previous errors
        this.syncUIFromFilters(filters);
        return this.pharmacyService.getPrescriptionsWithFilters(filters).pipe(
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
            } as PagedResponse<PrescriptionDto>);
          })
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        this.prescriptions = response.items || [];
        this.totalCount = response.totalCount || 0;
        this.totalPages = response.totalPages || 0;
        this.currentPage = response.currentPage || 1;
        this.pageSize = response.pageSize || 10;
        this.updateFilterCounts();
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
    this.selectedStatus = filters.prescriptionStatus || 'Pending';
    this.currentPage = filters.pageNumber || 1;
    this.pageSize = filters.pageSize || 10;
    if (filters.sortBy) {
      this.sortBy = filters.sortBy;
      this.sortColumn = filters.sortBy;
    }
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

    if (this.selectedStatus && this.selectedStatus !== 'All') {
      filters.prescriptionStatus = this.selectedStatus;
    }

    if (this.sortBy) {
      filters.sortBy = this.sortBy;
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

  loadPrescriptions(): void {
    const filters = this.buildFiltersFromUI();
    this.updateFilters(filters);
  }

  updateFilterCounts(): void {
    this.statusFilters.forEach(filter => {
      if (filter.value === 'All') {
        filter.count = this.totalCount;
      } else {
        const pageCount = this.prescriptions.filter(p => p.status === filter.value).length;
        if (this.totalCount > 0 && this.prescriptions.length > 0) {
          filter.count = Math.round((pageCount / this.prescriptions.length) * this.totalCount);
        } else {
          filter.count = pageCount;
        }
      }
    });
  }

  onStatusFilterChange(status: string): void {
    this.selectedStatus = status;
    if (status === 'All') {
      this.filterService.clearFilter('prescriptionStatus');
    } else {
      this.updateFilters({ prescriptionStatus: status });
    }
  }

  onSearchChange(searchTerm: string): void {
    this.searchTerm = searchTerm;
    this.searchSubject.next(searchTerm);
  }

  onSortChange(sortBy: string): void {
    if (this.sortBy === sortBy) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = sortBy;
      this.sortOrder = 'desc';
    }
    this.sortColumn = this.sortBy; // Sync with header sort
    this.updateFilters({ sortBy: this.sortBy, sortOrder: this.sortOrder });
  }

  /**
   * Handle column header sort click
   * Maps frontend column names to backend sort field names
   */
  onSort(column: string): void {
    // Map frontend column names to backend field names
    const columnMapping: { [key: string]: string } = {
      'patientName': 'prescribedDate', // Sort by date since patient name is computed
      'patient': 'prescribedDate',
      'medication': 'prescribedDate',
      'status': 'status',
      'totalAmount': 'totalAmount',
      'amount': 'totalAmount',
      'createdAt': 'createdAt',
      'createdDate': 'createdAt',
      'prescribedDate': 'prescribedDate',
      'date': 'prescribedDate'
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
      'patientName': 'prescribedDate',
      'patient': 'prescribedDate',
      'medication': 'prescribedDate',
      'status': 'status',
      'totalAmount': 'totalAmount',
      'amount': 'totalAmount',
      'createdAt': 'createdAt',
      'createdDate': 'createdAt',
      'prescribedDate': 'prescribedDate',
      'date': 'prescribedDate'
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
      'patientName': 'prescribedDate',
      'patient': 'prescribedDate',
      'medication': 'prescribedDate',
      'status': 'status',
      'totalAmount': 'totalAmount',
      'amount': 'totalAmount',
      'createdAt': 'createdAt',
      'createdDate': 'createdAt',
      'prescribedDate': 'prescribedDate',
      'date': 'prescribedDate'
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
   * Clear all filters and reset to default state
   */
  clearFilters(): void {
    // Clear service state
    this.filterService.clearAllFilters();

    // Clear template-bound properties
    this.searchTerm = '';
    this.selectedStatus = 'All';

    // Reset pagination to defaults
    this.currentPage = 1;
    this.pageSize = 10;

    // Reset sorting to defaults
    this.sortBy = 'date';
    this.sortOrder = 'desc';
    this.sortColumn = 'prescribedDate'; // Reset header sort column

    // Update active filters display
    this.updateActiveFilters();

    // Show success feedback
    this.showClearSuccessMessage();
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
   */
  @HostListener('document:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent): void {
    const hasActiveFilters = this.getActiveFilterCount() > 0;
    
    if (event.ctrlKey && event.shiftKey && event.key === 'C' && hasActiveFilters) {
      event.preventDefault();
      this.clearFilters();
    }
    
    if (event.key === 'Escape' && hasActiveFilters) {
      const target = event.target as HTMLElement;
      if (target.tagName !== 'INPUT' && target.tagName !== 'TEXTAREA') {
        event.preventDefault();
        this.clearFilters();
      }
    }
  }

  removeFilter(filterKey: string): void {
    this.filterService.clearFilter(filterKey as keyof PharmacyFilters);
    
    switch (filterKey) {
      case 'searchTerm':
        this.searchTerm = '';
        break;
      case 'prescriptionStatus':
        this.selectedStatus = 'All';
        break;
    }
    
    this.updateActiveFilters();
  }

  getActiveFilterCount(): number {
    return this.filterService.getActiveFilterCount();
  }

  updateActiveFilters(): void {
    this.activeFilters = this.filterService.getActiveFilters();
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

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'status-pending';
      case 'Dispensed':
        return 'status-dispensed';
      case 'Cancelled':
        return 'status-cancelled';
      default:
        return 'status-default';
    }
  }

  getPatientName(prescription: PrescriptionDto): string {
    if (prescription.patient) {
      return `${prescription.patient.firstName} ${prescription.patient.lastName}`;
    }
    return `Patient #${prescription.patientId}`;
  }

  getDoctorName(prescription: PrescriptionDto): string {
    if (prescription.doctor) {
      return `Dr. ${prescription.doctor.firstName} ${prescription.doctor.lastName}`;
    }
    return `Doctor #${prescription.doctorId}`;
  }

  formatDate(dateString: string): string {
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
      this.errorMessage = 'Failed to load prescriptions. Please try again.';
    }

    console.error('[PrescriptionsComponent] Error loading prescriptions:', error);
  }

  /**
   * Retry loading prescriptions after an error
   */
  retryLoad(): void {
    this.errorMessage = null;
    this.loadPrescriptions();
  }
}
