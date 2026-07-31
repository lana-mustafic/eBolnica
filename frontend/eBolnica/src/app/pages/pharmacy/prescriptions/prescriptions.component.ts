import { Component, inject, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { PharmacyFilterService } from '../../../shared/services/pharmacy/pharmacy-filter.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { FilterSummaryComponent } from '../../../shared/components/filter-summary/filter-summary.component';
import { ActiveFiltersComponent } from '../../../shared/components/active-filters/active-filters.component';
import { SortStatusComponent } from '../../../shared/components/sort-status/sort-status.component';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { PagedResponse } from '../../../models/paged-response.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil, catchError, of, EMPTY } from 'rxjs';
import { TABLE_DEFAULT_SORTS, mapPrescriptionSortColumn } from '../../../constants/sort.constants';
import { getPageRangeEnd, getPageRangeStart } from '../../../shared/utils/paged-response.util';
import { PharmacyShellComponent } from '../pharmacy-shell/pharmacy-shell.component';

@Component({
  selector: 'app-prescriptions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FilterSummaryComponent, ActiveFiltersComponent, SortStatusComponent, PharmacyShellComponent],
  templateUrl: './prescriptions.component.html',
  styleUrl: './prescriptions.component.css'
})
export class PrescriptionsComponent implements OnInit, OnDestroy {
  protected pharmacyService = inject(PharmacyService);
  protected filterService = inject(PharmacyFilterService);
  protected readonly filterContext = 'prescriptions' as const;
  private notificationService = inject(NotificationService);

  prescriptions: PrescriptionDto[] = [];
  isLoading: boolean = false;
  isSearching: boolean = false;
  isSorting: boolean = false; // Specific flag for sort operations
  isGeneratingPdf: boolean = false; // PDF generation state
  pdfProgress: number = 0; // PDF generation progress (0-100)
  showPdfProgress: boolean = false; // Show progress indicator
  wasGeneratingPdf: boolean = false; // Track state changes for screen reader (public for template)
  private pdfSubscription?: any; // Track PDF request subscription for cancellation
  private progressInterval?: any; // Progress simulation interval
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

  // Sort (default: newest first)
  sortBy: string = 'prescribedDate';
  sortOrder: 'asc' | 'desc' = TABLE_DEFAULT_SORTS.PRESCRIPTIONS.order;
  sortColumn: string = TABLE_DEFAULT_SORTS.PRESCRIPTIONS.column; // Default sort column for header clicks
  private previousSortColumn: string = TABLE_DEFAULT_SORTS.PRESCRIPTIONS.column; // For error recovery
  private previousSortOrder: 'asc' | 'desc' = TABLE_DEFAULT_SORTS.PRESCRIPTIONS.order; // For error recovery
  private sortDebounceTimer: any; // Timer for debouncing sort requests

  // Active filters for display
  activeFilters = this.filterService.getActiveFilters(this.filterContext);

  // Success message for clear operation
  clearSuccessMessage: string | null = null;

  ngOnInit(): void {
    this.syncUIFromFilters(this.filterService.getFilters(this.filterContext));

    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.pushFiltersFromUI();
    });

    this.filterService.getFilters$(this.filterContext).pipe(
      switchMap(filters => {
        if (!this.isSorting) {
          this.isSearching = true;
        }
        this.errorMessage = null;
        this.syncUIFromFilters(filters);
        return this.pharmacyService.getPrescriptionsWithFilters(filters).pipe(
          finalize(() => {
            if (!this.isSorting) {
              this.isSearching = false;
            }
            this.isSorting = false;
          }),
          catchError((error) => {
            if (this.isSorting) {
              this.handleSortError(error);
              return EMPTY;
            }

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
        this.applyPaginationFromResponse(response);
        this.updateFilterCounts();
        this.updateActiveFilters();
        this.errorMessage = null;
      }
    });

    this.pushFiltersFromUI();
  }

  ngOnDestroy(): void {
    // Clean up any ongoing PDF generation
    if (this.isGeneratingPdf && this.pdfSubscription) {
      this.pdfSubscription.unsubscribe();
      console.log('[PrescriptionsComponent] PDF generation interrupted by navigation');
    }

    // Clear any timeouts/intervals
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = undefined;
    }

    // Cancel sort debounce timer
    if (this.sortDebounceTimer) {
      clearTimeout(this.sortDebounceTimer);
    }

    this.destroy$.next();
    this.destroy$.complete();
    this.searchSubject.complete();
  }

  /**
   * Sync UI state from filter service state
   */
  private syncUIFromFilters(filters: PharmacyFilters): void {
    this.searchTerm = filters.searchTerm || '';
    this.selectedStatus = filters.prescriptionStatus || 'Pending';
    if (filters.sortBy) {
      this.sortBy = filters.sortBy;
      this.sortColumn = filters.sortBy;
    }
    if (filters.sortOrder) this.sortOrder = filters.sortOrder as 'asc' | 'desc';
  }

  private applyPaginationFromResponse(response: PagedResponse<PrescriptionDto>): void {
    this.totalCount = response.totalCount;
    this.totalPages = response.totalPages;
    this.currentPage = response.currentPage;
    this.pageSize = response.pageSize;
    this.filterService.syncPaginationFromResponse(this.filterContext,response.currentPage, response.pageSize);
  }

  get paginationRangeStart(): number {
    return getPageRangeStart(this.currentPage, this.pageSize, this.totalCount);
  }

  get paginationRangeEnd(): number {
    return getPageRangeEnd(this.currentPage, this.pageSize, this.totalCount);
  }

  /**
   * Build PharmacyFilters from current UI state (excludes pageNumber — owned by filter service)
   */
  private buildFiltersFromUI(): Partial<PharmacyFilters> {
    const filters: Partial<PharmacyFilters> = {
      pageSize: this.pageSize
    };

    if (this.searchTerm?.trim()) {
      filters.searchTerm = this.searchTerm.trim();
    }

    if (this.selectedStatus && this.selectedStatus !== 'All') {
      filters.prescriptionStatus = this.selectedStatus;
    } else {
      filters.prescriptionStatus = undefined;
    }

    if (this.sortColumn) {
      filters.sortBy = this.sortColumn;
    }
    if (this.sortOrder) {
      filters.sortOrder = this.sortOrder;
    }

    return filters;
  }

  private pushFiltersFromUI(): void {
    this.filterService.updateFilters(this.filterContext,this.buildFiltersFromUI());
  }

  /**
   * Update filters in service (triggers API call)
   */
  private updateFilters(updates: Partial<PharmacyFilters>): void {
    this.filterService.updateFilters(this.filterContext,updates);
  }

  loadPrescriptions(): void {
    this.pushFiltersFromUI();
  }

  updateFilterCounts(): void {
    this.statusFilters.forEach(filter => {
      if (filter.value === 'All') {
        filter.count = this.selectedStatus === 'All' ? this.totalCount : 0;
      } else if (filter.value === this.selectedStatus) {
        filter.count = this.totalCount;
      } else {
        filter.count = 0;
      }
    });
  }

  onStatusFilterChange(status: string): void {
    this.selectedStatus = status;
    if (status === 'All') {
      this.filterService.updateFilters(this.filterContext,{ prescriptionStatus: undefined });
    } else {
      this.updateFilters({ prescriptionStatus: status });
    }
  }

  onSearchChange(searchTerm: string): void {
    this.searchTerm = searchTerm;
    this.searchSubject.next(searchTerm);
  }

  onSortChange(sortBy: string): void {
    const backendSortBy = mapPrescriptionSortColumn(sortBy);
    if (this.sortBy === sortBy) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = sortBy;
      this.sortOrder = 'desc';
    }
    this.sortColumn = backendSortBy;
    this.pushFiltersFromUI();
  }

  /**
   * Handle column header sort click - SERVER-SIDE SORTING
   * Maps frontend column names to backend sort field names
   * Includes debouncing and request cancellation
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

    // Store previous sort state for error recovery
    this.previousSortColumn = this.sortColumn;
    this.previousSortOrder = this.sortOrder;

    if (this.sortColumn === backendColumn) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = backendColumn;
      this.sortOrder = 'asc';
    }
    this.sortBy = this.sortColumn;

    this.isSorting = true;
    this.errorMessage = null;

    if (this.sortDebounceTimer) {
      clearTimeout(this.sortDebounceTimer);
    }

    // Debounce sort requests (200ms) to avoid rapid API calls
    this.sortDebounceTimer = setTimeout(() => {
      this.isSorting = true;
      this.errorMessage = null;
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
      this.errorMessage = 'Network error. Unable to sort prescriptions. Please check your connection.';
    } else {
      this.errorMessage = 'Unable to sort prescriptions. Please try again.';
    }
    
    this.isSorting = false;
  }

  /**
   * Reset sorting to default (newest first)
   */
  resetSortingToDefault(): void {
    this.sortColumn = TABLE_DEFAULT_SORTS.PRESCRIPTIONS.column;
    this.sortOrder = TABLE_DEFAULT_SORTS.PRESCRIPTIONS.order;
    this.sortBy = 'date'; // Legacy property
  }

  /**
   * Check if current sort is default sort
   */
  isDefaultSort(): boolean {
    return this.sortColumn === TABLE_DEFAULT_SORTS.PRESCRIPTIONS.column &&
           this.sortOrder === TABLE_DEFAULT_SORTS.PRESCRIPTIONS.order;
  }

  /**
   * Get display name for current sort column
   */
  getSortDisplayName(): string {
    const columnNames: { [key: string]: string } = {
      'prescribedDate': 'Date Prescribed',
      'status': 'Status',
      'totalAmount': 'Total Amount',
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
   * Revert sort state to previous values or default
   */
  private revertSortState(): void {
    this.sortColumn = this.previousSortColumn || TABLE_DEFAULT_SORTS.PRESCRIPTIONS.column;
    this.sortOrder = this.previousSortOrder || TABLE_DEFAULT_SORTS.PRESCRIPTIONS.order;
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
    this.searchTerm = '';
    this.selectedStatus = 'All';
    this.pageSize = 10;
    this.resetSortingToDefault();

    this.filterService.clearAllFilters(this.filterContext);
    this.pushFiltersFromUI();

    this.updateActiveFilters();
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
    this.filterService.clearFilter(this.filterContext,filterKey as keyof PharmacyFilters);
    
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
    return this.filterService.getActiveFilterCount(this.filterContext);
  }

  updateActiveFilters(): void {
    this.activeFilters = this.filterService.getActiveFilters(this.filterContext);
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
    } else if (this.currentPage <= 4) {
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
      for (let i = this.currentPage - 1; i <= this.currentPage + 1; i++) {
        pages.push(i);
      }
      pages.push('...');
      pages.push(this.totalPages);
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

  trackByPageNum(_: number, pageNum: number | string): number | string {
    return pageNum;
  }

  Math = Math;

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

  /**
   * Check if PDF export is available
   */
  canExportPdf(): boolean {
    return this.prescriptions && this.prescriptions.length > 0;
  }

  /**
   * Get tooltip text for PDF export button with loading state
   */
  getPdfButtonTooltip(): string {
    if (!this.canExportPdf()) {
      return 'No data available to export';
    }
    
    if (this.isGeneratingPdf) {
      if (this.pdfProgress > 0) {
        return `Generating PDF... ${this.pdfProgress}% complete`;
      }
      return 'Generating PDF... Please wait';
    }
    
    return `Export ${this.prescriptions.length} prescription(s) to PDF`;
  }

  /**
   * Export prescriptions to PDF
   * Uses PharmacyService to download PDF with current filters and sorting
   * Includes loading state management and request deduplication
   */
  exportPrescriptionsToPdf(): void {
    // Prevent duplicate requests
    if (!this.canExportPdf() || this.isGeneratingPdf) {
      if (this.isGeneratingPdf) {
        console.log('[PrescriptionsComponent] PDF generation already in progress');
      }
      return;
    }

    // Cancel any existing subscription
    if (this.pdfSubscription) {
      this.pdfSubscription.unsubscribe();
    }

    // Set loading state
    this.isGeneratingPdf = true;
    this.wasGeneratingPdf = false;
    this.showPdfProgress = true;
    this.pdfProgress = 0;

    // Build current filters from component state
    const filters: PharmacyFilters = {
      pageNumber: 1,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm || undefined,
      prescriptionStatus: this.selectedStatus !== 'All' ? this.selectedStatus : undefined,
      sortBy: this.sortColumn,
      sortOrder: this.sortOrder
    };

    // Call service method to download PDF
    this.pdfSubscription = this.pharmacyService.exportPrescriptionsToPdf(filters).pipe(
      finalize(() => {
        // Clear progress interval
        if (this.progressInterval) {
          clearInterval(this.progressInterval);
          this.progressInterval = undefined;
        }

        // Complete progress
        this.pdfProgress = 100;
        this.wasGeneratingPdf = this.isGeneratingPdf;
        this.isGeneratingPdf = false;

        // Hide progress after a short delay
        setTimeout(() => {
          this.showPdfProgress = false;
          this.pdfProgress = 0;
        }, 500);

        // Clear subscription
        this.pdfSubscription = undefined;
      })
    ).subscribe({
      next: (response: any) => {
        // Download handled in service
        console.log('[PrescriptionsComponent] PDF download completed');
        
        // Extract file info from response
        const fileInfo = response?.fileInfo || { fileName: 'prescriptions-report.pdf', fileSize: 0 };
        const itemCount = fileInfo.exportedRowCount ?? this.totalCount;
        
        // Show success notification
        this.showPdfSuccess(fileInfo.fileName, fileInfo.fileSize, itemCount);
      },
      error: (error: any) => {
        console.error('[PrescriptionsComponent] PDF export error:', error);
        const errorMessage = this.getPdfErrorMessage(error);
        this.showPdfError(errorMessage);
      }
    });
  }

  /**
   * Cancel ongoing PDF generation
   */
  cancelPdfGeneration(): void {
    if (this.pdfSubscription) {
      this.pdfSubscription.unsubscribe();
      this.pdfSubscription = undefined;
    }

    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = undefined;
    }

    this.isGeneratingPdf = false;
    this.showPdfProgress = false;
    this.pdfProgress = 0;
    console.log('[PrescriptionsComponent] PDF generation cancelled');
  }

  /**
   * Get estimated time for PDF generation based on data size
   */
  getEstimatedTime(): string {
    const itemCount = this.prescriptions.length;
    
    if (itemCount < 100) return '~10 seconds';
    if (itemCount < 500) return '~30 seconds';
    if (itemCount < 1000) return '~1 minute';
    return 'Several minutes';
  }

  /**
   * Show PDF success notification
   */
  private showPdfSuccess(fileName: string, fileSize: number, itemCount: number): void {
    const sizeText = this.formatFileSize(fileSize);
    const countText = `${itemCount} prescription${itemCount !== 1 ? 's' : ''}`;
    
    this.notificationService.success(
      'PDF Download Complete',
      `${fileName} (${sizeText}) with ${countText} has been downloaded successfully.`,
      {
        duration: 6000,
        position: 'bottom-right',
        icon: 'check-circle'
      }
    );
  }

  /**
   * Show PDF error notification with retry option
   */
  private showPdfError(message: string): void {
    this.notificationService.error(
      'PDF Generation Failed',
      message,
      {
        duration: 8000,
        position: 'bottom-right',
        icon: 'exclamation-triangle',
        actionText: 'Retry',
        onAction: () => this.exportPrescriptionsToPdf()
      }
    );
  }

  /**
   * Get specific error message based on error type
   */
  private getPdfErrorMessage(error: any): string {
    if (error.status === 0) {
      return 'Network error. Please check your internet connection.';
    }
    
    if (error.status === 400) {
      return 'Invalid request parameters. Please try different filters.';
    }
    
    if (error.status === 404) {
      return 'PDF generation service is currently unavailable.';
    }
    
    if (error.status === 413) {
      return 'Report is too large. Try applying more filters.';
    }
    
    if (error.status === 500) {
      return 'Server error during PDF generation. Please try again.';
    }
    
    if (error.message?.includes('timeout')) {
      return 'PDF generation timed out. Try with fewer items.';
    }
    
    return error.message || 'Failed to generate PDF. Please try again.';
  }

  /**
   * Format file size for display
   */
  private formatFileSize(bytes: number): string {
    if (!bytes || bytes === 0) return 'Unknown size';
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / 1048576).toFixed(1) + ' MB';
  }
}
