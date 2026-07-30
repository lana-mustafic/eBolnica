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
import { MedicationDto } from '../../../models/medication.dto';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { InventoryResponse } from '../../../models/inventory-response.dto';
import { PagedResponse } from '../../../models/paged-response.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil, tap, catchError, of, EMPTY } from 'rxjs';
import {
  INVENTORY_SORT_DISPLAY_NAMES,
  mapInventorySortColumn,
  TABLE_DEFAULT_SORTS
} from '../../../constants/sort.constants';
import { formatLocalDateParam } from '../../../shared/utils/date-only.util';
import { getPageRangeEnd, getPageRangeStart } from '../../../shared/utils/paged-response.util';
import { buildInventoryExportCsv, getInventoryExportFilename } from '../../../shared/utils/inventory-csv.util';
import { downloadCsv } from '../../../shared/utils/csv.util';

type StockStatus = 'adequate' | 'low' | 'critical' | 'out-of-stock';
type ExpiryStatus = 'good' | 'warning' | 'critical' | 'expired';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FilterSummaryComponent, ActiveFiltersComponent, SortStatusComponent],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css'
})
export class InventoryComponent implements OnInit, OnDestroy {
  protected pharmacyService = inject(PharmacyService);
  protected filterService = inject(PharmacyFilterService);
  protected readonly filterContext = 'inventory' as const;
  private notificationService = inject(NotificationService);

  inventoryItems: MedicationDto[] = [];
  lowStockAlerts: MedicationDto[] = [];
  expiryAlerts: MedicationDto[] = [];
  isLoading: boolean = false;
  isSearching: boolean = false; // Separate flag for search loading
  isSorting: boolean = false;
  isGeneratingPdf: boolean = false; // PDF generation state
  pdfProgress: number = 0; // PDF generation progress (0-100)
  showPdfProgress: boolean = false; // Show progress indicator
  wasGeneratingPdf: boolean = false; // Track state changes for screen reader (public for template)
  private pdfSubscription?: any; // Track PDF request subscription for cancellation
  private progressInterval?: any; // Progress simulation interval
  errorMessage: string | null = null;

  // Pagination
  currentPage: number = 1;
  pageSize: number = 50; // Larger page size for inventory
  totalCount: number = 0;
  totalPages: number = 0;

  // Filters
  selectedStockFilter: string = 'all';
  selectedExpiryFilter: string = 'all';
  selectedCategory: string = '';
  searchTerm: string = '';
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  // Available categories
  categories: string[] = [];

  // Sort state (server-side)
  sortColumn: string = TABLE_DEFAULT_SORTS.INVENTORY.column;
  sortOrder: 'asc' | 'desc' = TABLE_DEFAULT_SORTS.INVENTORY.order;
  private previousSortColumn: string = TABLE_DEFAULT_SORTS.INVENTORY.column; // For error recovery
  private previousSortOrder: 'asc' | 'desc' = TABLE_DEFAULT_SORTS.INVENTORY.order; // For error recovery
  private sortDebounceTimer?: ReturnType<typeof setTimeout>;

  // Summary statistics
  totalItems: number = 0;
  lowStockCount: number = 0;
  expiringSoonCount: number = 0;
  outOfStockCount: number = 0;
  criticalStockCount: number = 0;

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
        this.isSearching = true;
        this.errorMessage = null;
        this.syncUIFromFilters(filters);
        return this.pharmacyService.getInventoryWithFilters(filters).pipe(
          finalize(() => {
            this.isSearching = false;
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
              lowStockAlerts: [],
              expiryAlerts: [],
              totalCount: 0,
              totalPages: 0,
              currentPage: 1,
              pageSize: filters.pageSize || 50,
              hasNext: false,
              hasPrevious: false
            } satisfies InventoryResponse);
          })
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response: InventoryResponse) => {
        this.inventoryItems = response.items || [];
        this.lowStockAlerts = response.lowStockAlerts || [];
        this.expiryAlerts = response.expiryAlerts || [];
        this.applyPaginationFromResponse(response);
        this.extractCategories();
        this.calculateSummaryStats();
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
      console.log('[InventoryComponent] PDF generation interrupted by navigation');
    }

    // Clear any timeouts/intervals
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = undefined;
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
    this.selectedCategory = filters.category || '';
    this.selectedStockFilter = this.mapApiStockFilterToUi(filters);
    this.selectedExpiryFilter = filters.expiryStatus || 'all';
    if (filters.sortBy) {
      this.sortColumn = filters.sortBy;
    }
    if (filters.sortOrder) {
      this.sortOrder = filters.sortOrder as 'asc' | 'desc';
    }
  }

  private applyPaginationFromResponse(response: PagedResponse<MedicationDto>): void {
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
   * Build PharmacyFilters from current UI state (server-side paging, filtering, sorting)
   */
  private buildFiltersFromUI(): Partial<PharmacyFilters> {
    const stockFilters = this.mapStockFilterToApi(this.selectedStockFilter);
    const expiryFilters = this.mapExpiryFilterToApi(this.selectedExpiryFilter);

    return {
      pageSize: this.pageSize,
      searchTerm: this.searchTerm?.trim() || undefined,
      category: this.selectedCategory || undefined,
      stockStatus: stockFilters.stockStatus,
      minStock: stockFilters.minStock,
      maxStock: stockFilters.maxStock,
      expiryStatus: expiryFilters.expiryStatus,
      expiryAfter: expiryFilters.expiryAfter,
      expiryBefore: expiryFilters.expiryBefore,
      ...this.buildSortFiltersFromUI()
    };
  }

  private buildSortFiltersFromUI(): Pick<PharmacyFilters, 'sortBy' | 'sortOrder'> {
    return {
      sortBy: this.sortColumn || TABLE_DEFAULT_SORTS.INVENTORY.column,
      sortOrder: this.sortOrder || TABLE_DEFAULT_SORTS.INVENTORY.order
    };
  }

  private pushFiltersFromUI(): void {
    this.filterService.updateFilters(this.filterContext,this.buildFiltersFromUI());
  }

  private mapStockFilterToApi(stockFilter: string): Pick<PharmacyFilters, 'stockStatus' | 'minStock' | 'maxStock'> {
    switch (stockFilter) {
      case 'adequate':
        return { stockStatus: 'normal stock', minStock: undefined, maxStock: undefined };
      case 'low':
        return { stockStatus: 'low stock', minStock: undefined, maxStock: undefined };
      case 'critical':
        return { stockStatus: 'critical stock', minStock: undefined, maxStock: undefined };
      case 'out-of-stock':
        return { stockStatus: 'out of stock', minStock: undefined, maxStock: undefined };
      default:
        return { stockStatus: undefined, minStock: undefined, maxStock: undefined };
    }
  }

  private mapApiStockFilterToUi(filters: PharmacyFilters): string {
    switch (filters.stockStatus?.toLowerCase()) {
      case 'low stock':
        return 'low';
      case 'critical stock':
        return 'critical';
      case 'out of stock':
        return 'out-of-stock';
      case 'normal stock':
      case 'in stock':
        return 'adequate';
      default:
        return 'all';
    }
  }

  private mapExpiryFilterToApi(expiryFilter: string): Pick<PharmacyFilters, 'expiryStatus' | 'expiryAfter' | 'expiryBefore'> {
    if (!expiryFilter || expiryFilter === 'all') {
      return { expiryStatus: undefined, expiryAfter: undefined, expiryBefore: undefined };
    }

    const today = this.startOfDay(new Date());

    switch (expiryFilter) {
      case 'good':
        return {
          expiryStatus: expiryFilter,
          expiryAfter: this.formatDateParam(this.addDays(today, 90)),
          expiryBefore: undefined
        };
      case 'warning':
        return {
          expiryStatus: expiryFilter,
          expiryAfter: this.formatDateParam(this.addDays(today, 30)),
          expiryBefore: this.formatDateParam(this.addDays(today, 89))
        };
      case 'critical':
        return {
          expiryStatus: expiryFilter,
          expiryAfter: this.formatDateParam(today),
          expiryBefore: this.formatDateParam(this.addDays(today, 29))
        };
      case 'expired':
        return {
          expiryStatus: expiryFilter,
          expiryAfter: undefined,
          expiryBefore: this.formatDateParam(this.addDays(today, -1))
        };
      default:
        return { expiryStatus: undefined, expiryAfter: undefined, expiryBefore: undefined };
    }
  }

  private startOfDay(date: Date): Date {
    const normalized = new Date(date);
    normalized.setHours(0, 0, 0, 0);
    return normalized;
  }

  private addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  private formatDateParam(date: Date): string {
    return formatLocalDateParam(date);
  }

  loadInventory(): void {
    this.pushFiltersFromUI();
  }

  extractCategories(): void {
    const categorySet = new Set<string>();
    this.inventoryItems.forEach(item => {
      if (item.category) {
        categorySet.add(item.category);
      }
    });
    this.categories = Array.from(categorySet).sort();
  }

  calculateSummaryStats(): void {
    this.totalItems = this.totalCount;
    this.lowStockCount = this.lowStockAlerts.filter(item =>
      this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel) === 'low'
    ).length;
    this.criticalStockCount = this.lowStockAlerts.filter(item =>
      this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel) === 'critical'
    ).length;
    this.outOfStockCount = this.lowStockAlerts.filter(item =>
      item.stockQuantity === 0
    ).length;
    this.expiringSoonCount = this.expiryAlerts.length;
  }

  calculateStockStatus(stock: number, minimum: number): StockStatus {
    if (stock === 0) return 'out-of-stock';
    if (stock < 5) return 'critical';
    if (stock < minimum) return 'low';
    return 'adequate';
  }

  calculateExpiryStatus(expiryDate: string | undefined): ExpiryStatus {
    if (!expiryDate) return 'good';
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const expiry = new Date(expiryDate);
    expiry.setHours(0, 0, 0, 0);
    
    const daysUntilExpiry = Math.floor((expiry.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    
    if (daysUntilExpiry < 0) return 'expired';
    if (daysUntilExpiry < 30) return 'critical';
    if (daysUntilExpiry < 90) return 'warning';
    return 'good';
  }

  getDaysUntilExpiry(expiryDate: string | undefined): number {
    if (!expiryDate) return -1;
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const expiry = new Date(expiryDate);
    expiry.setHours(0, 0, 0, 0);
    
    return Math.floor((expiry.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
  }

  onSearchChange(searchTerm: string): void {
    this.searchTerm = searchTerm;
    this.searchSubject.next(searchTerm);
  }

  onCategoryChange(category: string): void {
    this.selectedCategory = category;
    this.pushFiltersFromUI();
  }

  onFilterChange(): void {
    this.pushFiltersFromUI();
  }

  /**
   * Clear all filters and reset to default state
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStockFilter = 'all';
    this.selectedExpiryFilter = 'all';
    this.selectedCategory = '';
    this.pageSize = 50;
    this.resetSortingToDefault();

    this.filterService.clearAllFilters(this.filterContext);
    this.pushFiltersFromUI();

    this.updateActiveFilters();
    this.showClearSuccessMessage();
  }

  /**
   * Reset sorting to default (newest first)
   */
  resetSortingToDefault(): void {
    this.sortColumn = TABLE_DEFAULT_SORTS.INVENTORY.column;
    this.sortOrder = TABLE_DEFAULT_SORTS.INVENTORY.order;
  }

  /**
   * Check if current sort is default sort
   */
  isDefaultSort(): boolean {
    return this.sortColumn === TABLE_DEFAULT_SORTS.INVENTORY.column &&
           this.sortOrder === TABLE_DEFAULT_SORTS.INVENTORY.order;
  }

  /**
   * Get display name for current sort column
   */
  getSortDisplayName(): string {
    return INVENTORY_SORT_DISPLAY_NAMES[this.sortColumn] ?? this.sortColumn;
  }

  /**
   * Reset to default sort and reload from server
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
    switch (filterKey) {
      case 'searchTerm':
        this.searchTerm = '';
        this.filterService.clearFilter(this.filterContext,'searchTerm');
        break;
      case 'category':
        this.selectedCategory = '';
        this.filterService.clearFilter(this.filterContext,'category');
        break;
      case 'stockStatus':
        this.selectedStockFilter = 'all';
        this.filterService.updateFilters(this.filterContext,{
          stockStatus: undefined,
          minStock: undefined,
          maxStock: undefined
        });
        break;
      case 'expiryStatus':
        this.selectedExpiryFilter = 'all';
        this.filterService.updateFilters(this.filterContext,{
          expiryStatus: undefined,
          expiryAfter: undefined,
          expiryBefore: undefined
        });
        break;
      default:
        this.filterService.clearFilter(this.filterContext,filterKey as keyof PharmacyFilters);
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
      this.filterService.updateFilters(this.filterContext,{ pageNumber: page });
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
    this.filterService.updateFilters(this.filterContext,{ pageSize: size, pageNumber: 1 });
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

  /**
   * Handle column header sort click — server-side sorting via API
   */
  onSort(column: string): void {
    const backendColumn = mapInventorySortColumn(column);

    this.previousSortColumn = this.sortColumn;
    this.previousSortOrder = this.sortOrder;

    if (this.sortColumn === backendColumn) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = backendColumn;
      this.sortOrder = 'asc';
    }

    this.isSorting = true;
    this.errorMessage = null;

    if (this.sortDebounceTimer) {
      clearTimeout(this.sortDebounceTimer);
    }

    this.sortDebounceTimer = setTimeout(() => {
      this.pushFiltersFromUI();
    }, 200);
  }

  /**
   * Handle sort-specific errors
   */
  private handleSortError(error: any): void {
    console.error('Sort failed:', error);

    this.revertSortState();

    if (error?.status === 400) {
      this.errorMessage = 'Invalid sort column. Please try a different column.';
      console.warn('Invalid sort parameters:', error.error);
    } else if (error?.status === 0) {
      this.errorMessage = 'Network error. Unable to sort inventory. Please check your connection.';
    } else {
      this.errorMessage = 'Unable to sort inventory. Please try again.';
    }

    this.isSorting = false;
  }

  /**
   * Revert sort state to previous values or default
   */
  private revertSortState(): void {
    this.sortColumn = this.previousSortColumn || TABLE_DEFAULT_SORTS.INVENTORY.column;
    this.sortOrder = this.previousSortOrder || TABLE_DEFAULT_SORTS.INVENTORY.order;
    this.pushFiltersFromUI();
  }

  isSortColumnActive(column: string): boolean {
    return this.sortColumn === mapInventorySortColumn(column);
  }

  getSortIconClass(column: string, direction: 'asc' | 'desc'): string {
    if (!this.isSortColumnActive(column)) {
      return '';
    }
    return this.sortOrder === direction ? 'active' : '';
  }

  getAriaSort(column: string): string {
    if (!this.isSortColumnActive(column)) {
      return 'none';
    }
    return this.sortOrder === 'asc' ? 'ascending' : 'descending';
  }

  onSortKeydown(event: KeyboardEvent, column: string): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onSort(column);
    }
  }

  Math = Math;

  getStockStatusClass(item: MedicationDto): string {
    const status = this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel);
    return `stock-status-${status}`;
  }

  getStockStatusText(item: MedicationDto): string {
    const status = this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel);
    switch (status) {
      case 'adequate':
        return 'Adequate';
      case 'low':
        return 'Low Stock';
      case 'critical':
        return 'Critical';
      case 'out-of-stock':
        return 'Out of Stock';
      default:
        return 'Unknown';
    }
  }

  getStockStatusIcon(item: MedicationDto): string {
    const status = this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel);
    switch (status) {
      case 'adequate':
        return '✅';
      case 'low':
        return '⚠️';
      case 'critical':
        return '🔥';
      case 'out-of-stock':
        return '❌';
      default:
        return '❓';
    }
  }

  getExpiryStatusClass(item: MedicationDto): string {
    const status = this.calculateExpiryStatus(item.expiryDate);
    return `expiry-status-${status}`;
  }

  getExpiryStatusText(item: MedicationDto): string {
    const status = this.calculateExpiryStatus(item.expiryDate);
    switch (status) {
      case 'good':
        return 'Good';
      case 'warning':
        return 'Warning';
      case 'critical':
        return 'Expiring Soon';
      case 'expired':
        return 'Expired';
      default:
        return 'Unknown';
    }
  }

  getExpiryStatusIcon(item: MedicationDto): string {
    const status = this.calculateExpiryStatus(item.expiryDate);
    switch (status) {
      case 'good':
        return '✅';
      case 'warning':
        return '⚠️';
      case 'critical':
        return '❌';
      case 'expired':
        return '⛔';
      default:
        return '❓';
    }
  }

  getRowClass(item: MedicationDto): string {
    const classes: string[] = [];
    
    const stockStatus = this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel);
    if (stockStatus === 'critical' || stockStatus === 'out-of-stock') {
      classes.push('row-critical');
    } else if (stockStatus === 'low') {
      classes.push('row-warning');
    }
    
    const expiryStatus = this.calculateExpiryStatus(item.expiryDate);
    if (expiryStatus === 'expired') {
      classes.push('row-expired');
    } else if (expiryStatus === 'critical') {
      classes.push('row-expiring');
    }
    
    return classes.join(' ');
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

  /**
   * Check if PDF export is available
   */
  canExportPdf(): boolean {
    return this.inventoryItems && this.inventoryItems.length > 0;
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
    
    return `Export ${this.inventoryItems.length} item(s) to PDF`;
  }

  /**
   * Export inventory to PDF
   * Uses PharmacyService to download PDF with current filters and sorting
   * Includes loading state management and request deduplication
   */
  exportInventoryToPdf(): void {
    // Prevent duplicate requests
    if (!this.canExportPdf() || this.isGeneratingPdf) {
      if (this.isGeneratingPdf) {
        console.log('[InventoryComponent] PDF generation already in progress');
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
    // Simulate progress removed — spinner reflects isGeneratingPdf until download completes.
    this.showPdfProgress = true;
    this.pdfProgress = 0;

    // Build current filters from component state
    const filters: PharmacyFilters = {
      pageNumber: 1,
      pageSize: this.pageSize,
      ...this.buildFiltersFromUI(),
      sortBy: this.sortColumn,
      sortOrder: this.sortOrder
    };

    // Call service method to download PDF
    this.pdfSubscription = this.pharmacyService.exportInventoryToPdf(filters).pipe(
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
        console.log('[InventoryComponent] PDF download completed');
        
        // Extract file info from response
        const fileInfo = response?.fileInfo || { fileName: 'inventory-report.pdf', fileSize: 0 };
        const itemCount = fileInfo.exportedRowCount ?? this.totalCount;
        
        // Show success notification
        this.showPdfSuccess(fileInfo.fileName, fileInfo.fileSize, itemCount);
      },
      error: (error: any) => {
        console.error('[InventoryComponent] PDF export error:', error);
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
    console.log('[InventoryComponent] PDF generation cancelled');
  }

  /**
   * Get estimated time for PDF generation based on data size
   */
  getEstimatedTime(): string {
    const itemCount = this.inventoryItems.length;
    
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
    const countText = `${itemCount} item${itemCount !== 1 ? 's' : ''}`;
    
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
        onAction: () => this.exportInventoryToPdf()
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

  exportToCSV(): void {
    if (this.inventoryItems.length === 0) {
      alert('No data to export.');
      return;
    }

    downloadCsv(
      buildInventoryExportCsv(this.inventoryItems),
      getInventoryExportFilename()
    );
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
      this.errorMessage = 'Failed to load inventory. Please try again.';
    }

    console.error('[InventoryComponent] Error loading inventory:', error);
  }

  /**
   * Retry loading inventory after an error
   */
  retryLoad(): void {
    this.errorMessage = null;
    this.loadInventory();
  }
}
