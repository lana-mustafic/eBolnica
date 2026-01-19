import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { MedicationDto } from '../../../models/medication.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize } from 'rxjs';

type StockStatus = 'adequate' | 'low' | 'critical' | 'out-of-stock';
type ExpiryStatus = 'good' | 'warning' | 'critical' | 'expired';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css'
})
export class InventoryComponent implements OnInit {
  private pharmacyService = inject(PharmacyService);

  inventoryItems: MedicationDto[] = [];
  filteredItems: MedicationDto[] = [];
  isLoading: boolean = false;
  errorMessage: string | null = null;

  // Filters
  selectedStockFilter: string = 'all';
  selectedExpiryFilter: string = 'all';
  selectedCategory: string = '';
  searchTerm: string = '';
  private searchSubject = new Subject<string>();

  // Available categories
  categories: string[] = [];

  // Summary statistics
  totalItems: number = 0;
  lowStockCount: number = 0;
  expiringSoonCount: number = 0;
  outOfStockCount: number = 0;
  criticalStockCount: number = 0;

  ngOnInit(): void {
    this.loadInventory();
    
    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.applyFilters();
    });
  }

  loadInventory(): void {
    this.isLoading = true;
    this.errorMessage = null;

    // Load all active medications with large page size
    this.pharmacyService.getAllMedications(undefined, undefined, undefined, undefined, true, 1, 1000).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (response) => {
        // Only show active medications in inventory
        this.inventoryItems = response.data.filter((m: MedicationDto) => m.isActive);
        this.extractCategories();
        this.calculateSummaryStats();
        this.applyFilters();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load inventory. Please try again later.';
        console.error('Error loading inventory:', error);
      }
    });
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
    this.totalItems = this.inventoryItems.length;
    this.lowStockCount = this.inventoryItems.filter(item => 
      this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel) === 'low'
    ).length;
    this.criticalStockCount = this.inventoryItems.filter(item => 
      this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel) === 'critical'
    ).length;
    this.outOfStockCount = this.inventoryItems.filter(item => 
      item.stockQuantity === 0
    ).length;
    this.expiringSoonCount = this.inventoryItems.filter(item => 
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
    this.searchSubject.next(searchTerm);
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedStockFilter = 'all';
    this.selectedExpiryFilter = 'all';
    this.selectedCategory = '';
    this.applyFilters();
  }

  applyFilters(): void {
    let filtered = [...this.inventoryItems];

    // Stock filter
    if (this.selectedStockFilter !== 'all') {
      filtered = filtered.filter(item => {
        const status = this.calculateStockStatus(item.stockQuantity, item.minimumStockLevel);
        return status === this.selectedStockFilter;
      });
    }

    // Expiry filter
    if (this.selectedExpiryFilter !== 'all') {
      filtered = filtered.filter(item => {
        const expiryStatus = this.calculateExpiryStatus(item.expiryDate);
        return expiryStatus === this.selectedExpiryFilter;
      });
    }

    // Category filter
    if (this.selectedCategory) {
      filtered = filtered.filter(item => item.category === this.selectedCategory);
    }

    // Search filter
    if (this.searchTerm.trim()) {
      const search = this.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(item => 
        item.name.toLowerCase().includes(search) ||
        (item.genericName && item.genericName.toLowerCase().includes(search)) ||
        (item.manufacturer && item.manufacturer.toLowerCase().includes(search))
      );
    }

    this.filteredItems = filtered;
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
    if (this.filteredItems.length === 0) {
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

    const csvData = this.filteredItems.map(item => [
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
