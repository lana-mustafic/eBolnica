import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService, MedicationFilterParams } from '../../../shared/services/pharmacy/pharmacy.service';
import { MedicationDto } from '../../../models/medication.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize } from 'rxjs';

@Component({
  selector: 'app-medications',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './medications.component.html',
  styleUrl: './medications.component.css'
})
export class MedicationsComponent implements OnInit {
  private pharmacyService = inject(PharmacyService);

  medications: MedicationDto[] = [];
  filteredMedications: MedicationDto[] = [];
  isLoading: boolean = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Pagination
  page: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  totalPages: number = 0;

  // Search
  searchTerm: string = '';
  private searchSubject = new Subject<string>();

  // Filters
  selectedCategory: string = '';
  selectedStockStatus: string = '';
  selectedRequiresPrescription: string = '';
  selectedActiveStatus: string = '';

  // Available categories (populated from medications)
  categories: string[] = [];

  ngOnInit(): void {
    this.loadMedications();
    
    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.page = 1; // Reset to first page on search
      this.loadMedications();
    });
  }

  loadMedications(): void {
    this.isLoading = true;
    this.errorMessage = null;

    // Build filter parameters object
    // Map UI filter values to API format: "Low Stock" -> "low stock", "Out of Stock" -> "out of stock"
    const filters: MedicationFilterParams = {
      page: this.page,
      pageSize: this.pageSize
    };

    if (this.selectedCategory) {
      filters.category = this.selectedCategory;
    }

    if (this.searchTerm.trim()) {
      filters.search = this.searchTerm.trim();
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

    this.pharmacyService.getAllMedications(filters).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (response) => {
        this.medications = response.data;
        this.filteredMedications = response.data; // For backwards compatibility
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.page = response.page;
        this.pageSize = response.pageSize;
        
        // Extract categories from all medications (we might need to load all for this)
        // For now, extract from current page
        this.extractCategories();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load medications. Please try again later.';
        console.error('Error loading medications:', error);
      }
    });
  }

  extractCategories(): void {
    // Extract categories from current page
    // Note: This might not show all categories. Consider loading all medications once for categories
    const categorySet = new Set<string>();
    this.medications.forEach(med => {
      if (med.category) {
        categorySet.add(med.category);
      }
    });
    this.categories = Array.from(categorySet).sort();
  }

  onSearchChange(searchTerm: string): void {
    this.searchSubject.next(searchTerm);
  }

  onFilterChange(): void {
    this.page = 1; // Reset to first page on filter change
    this.loadMedications();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategory = '';
    this.selectedStockStatus = '';
    this.selectedRequiresPrescription = '';
    this.selectedActiveStatus = '';
    this.page = 1;
    this.loadMedications();
  }

  onPageChange(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.page = newPage;
      this.loadMedications();
    }
  }

  onPageSizeChange(newPageSize: number): void {
    this.pageSize = newPageSize;
    this.page = 1; // Reset to first page when changing page size
    this.loadMedications();
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
          // Clear success message after 3 seconds
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
}
