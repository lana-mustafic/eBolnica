import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { PharmacyFilterService } from '../../../shared/services/pharmacy/pharmacy-filter.service';
import { MedicationDto } from '../../../models/medication.dto';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil, tap } from 'rxjs';

type StockStatus = 'adequate' | 'low' | 'critical' | 'out-of-stock';
type ExpiryStatus = 'good' | 'warning' | 'critical' | 'expired';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css'
})
export class InventoryComponent implements OnInit, OnDestroy {
  private pharmacyService = inject(PharmacyService);
  private filterService = inject(PharmacyFilterService);

  inventoryItems: MedicationDto[] = [];
  lowStockAlerts: MedicationDto[] = [];
  expiryAlerts: MedicationDto[] = [];
  isLoading: boolean = false;
  isSearching: boolean = false; // Separate flag for search loading
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

  // Summary statistics
  totalItems: number = 0;
  lowStockCount: number = 0;
  expiringSoonCount: number = 0;
  outOfStockCount: number = 0;
  criticalStockCount: number = 0;

  // Active filters for display
  activeFilters = this.filterService.getActiveFilters();

  ngOnInit(): void {
    // Initialize filters from service
    const currentFilters = this.filterService.getFilters();
    this.syncUIFromFilters(currentFilters);

    // Load initial data
    this.loadInventory();

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
        this.syncUIFromFilters(filters);
        return this.pharmacyService.getInventoryWithFilters(filters).pipe(
          finalize(() => this.isSearching = false)
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response: any) => {
        this.inventoryItems = response.items || [];
        this.lowStockAlerts = response.LowStockAlerts || [];
        this.expiryAlerts = response.ExpiryAlerts || [];
        this.totalCount = response.totalCount || 0;
        this.totalPages = response.totalPages || 0;
        this.currentPage = response.currentPage || 1;
        this.pageSize = response.pageSize || 50;
        this.extractCategories();
        this.calculateSummaryStats();
        this.activeFilters = this.filterService.getActiveFilters();
        this.errorMessage = null;
      },
      error: (error) => {
        this.isSearching = false;
        this.errorMessage = 'Failed to load inventory. Please try again.';
        console.error('Error loading inventory:', error);
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
    this.currentPage = filters.pageNumber || 1;
    this.pageSize = filters.pageSize || 50;
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

    return filters;
  }

  /**
   * Update filters in service (triggers API call)
   */
  private updateFilters(updates: Partial<PharmacyFilters>): void {
    this.filterService.updateFilters(updates);
  }

  loadInventory(): void {
    const filters = this.buildFiltersFromUI();
    this.updateFilters(filters);
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
    // Use total count from backend for accurate statistics
    this.totalItems = this.totalCount || this.inventoryItems.length;
    
    // Calculate stats from current page items (approximate)
    // Note: For accurate stats, backend should provide these counts
    this.lowStockCount = this.lowStockAlerts.length || this.inventoryItems.filter(item => 
      this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel) === 'low'
    ).length;
    this.criticalStockCount = this.inventoryItems.filter(item => 
      this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel) === 'critical'
    ).length;
    this.outOfStockCount = this.inventoryItems.filter(item => 
      item.stockQuantity === 0
    ).length;
    this.expiringSoonCount = this.expiryAlerts.length || this.inventoryItems.filter(item => 
      item.expiryDate && this.calculateExpiryStatus(item.expiryDate) === 'critical'
    ).length;
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
    this.updateFilters({ category: category || undefined });
  }

  onFilterChange(): void {
    // Legacy method - now handled by individual change handlers
    this.loadInventory();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStockFilter = 'all';
    this.selectedExpiryFilter = 'all';
    this.selectedCategory = '';
    this.filterService.clearFilters();
  }

  removeFilter(filterKey: string): void {
    this.filterService.clearFilter(filterKey as keyof PharmacyFilters);
    
    switch (filterKey) {
      case 'searchTerm':
        this.searchTerm = '';
        break;
      case 'category':
        this.selectedCategory = '';
        break;
    }
  }

  getActiveFilterCount(): number {
    return this.filterService.getActiveFilterCount();
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

  exportToCSV(): void {
    if (this.inventoryItems.length === 0) {
      alert('No data to export.');
      return;
    }

    const headers = [
      'Medication Name',
      'Generic Name',
      'Category',
      'Manufacturer',
      'Current Stock',
      'Minimum Stock Level',
      'Stock Status',
      'Price',
      'Expiry Date',
      'Days Until Expiry',
      'Expiry Status',
      'Batch Number',
      'Last Updated'
    ];

    const csvData = this.inventoryItems.map(item => [
      this.escapeCSV(item.name),
      this.escapeCSV(item.genericName || ''),
      this.escapeCSV(item.category || ''),
      this.escapeCSV(item.manufacturer || ''),
      item.stockQuantity.toString(),
      item.minimumStockLevel.toString(),
      this.getStockStatusText(item),
      item.price.toString(),
      item.expiryDate ? this.formatDate(item.expiryDate) : '',
      item.expiryDate ? this.getDaysUntilExpiry(item.expiryDate).toString() : '',
      this.getExpiryStatusText(item),
      this.escapeCSV(item.batchNumber || ''),
      item.updatedAt ? this.formatDate(item.updatedAt) : this.formatDate(item.createdAt)
    ]);

    const csvContent = [headers, ...csvData]
      .map(row => row.join(','))
      .join('\n');

    const today = new Date().toISOString().split('T')[0];
    const filename = `pharmacy-inventory-${today}.csv`;

    this.downloadCSV(csvContent, filename);
  }

  private escapeCSV(value: string): string {
    if (!value) return '';
    // Escape quotes and wrap in quotes if contains comma, quote, or newline
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
