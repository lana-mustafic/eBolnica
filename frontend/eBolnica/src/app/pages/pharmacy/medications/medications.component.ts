import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService, MedicationFilterParams } from '../../../shared/services/pharmacy/pharmacy.service';
import { MedicationDto } from '../../../models/medication.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil, tap, of } from 'rxjs';

@Component({
  selector: 'app-medications',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './medications.component.html',
  styleUrl: './medications.component.css'
})
export class MedicationsComponent implements OnInit, OnDestroy {
  private pharmacyService = inject(PharmacyService);

  medications: MedicationDto[] = [];
  isLoading: boolean = false;
  isSearching: boolean = false; // Separate flag for search loading
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

  // Filters
  selectedCategory: string = '';
  selectedStockStatus: string = '';
  selectedRequiresPrescription: string = '';
  selectedActiveStatus: string = '';

  // Available categories (populated from medications)
  categories: string[] = [];

  ngOnInit(): void {
    this.loadMedications();
    
    // Setup debounced search with switchMap to cancel previous requests
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      tap(() => {
        this.isSearching = true;
        this.currentPage = 1; // Reset to first page on search
      }),
      switchMap(searchTerm => {
        this.searchTerm = searchTerm;
        // Build filters with search term
        const filters = this.buildFilterParams();
        return this.pharmacyService.getAllMedications(filters).pipe(
          finalize(() => this.isSearching = false)
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
        this.errorMessage = null;
      },
      error: (error) => {
        this.isSearching = false;
        this.errorMessage = 'Search failed. Please try again.';
        console.error('Error searching medications:', error);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Build filter parameters object from current component state
   */
  private buildFilterParams(): MedicationFilterParams {
    const filters: MedicationFilterParams = {
      page: this.currentPage,
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

    return filters;
  }

  loadMedications(): void {
    this.isLoading = true;
    this.errorMessage = null;

    const filters = this.buildFilterParams();

    this.pharmacyService.getAllMedications(filters).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (response) => {
        this.medications = response.items || [];
        this.totalCount = response.totalCount || 0;
        this.totalPages = response.totalPages || 0;
        this.currentPage = response.currentPage || 1;
        this.pageSize = response.pageSize || 10;
        
        // Extract categories from current page medications
        this.extractCategories();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load medications. Please try again later.';
        console.error('Error loading medications:', error);
      }
    });
  }

  private extractCategories(): void {
    // Extract unique categories from current page medications
    // Note: This shows categories from current page only
    // For complete category list, could load all medications once or use separate endpoint
    const categorySet = new Set<string>();
    this.medications.forEach(med => {
      if (med.category) {
        categorySet.add(med.category);
      }
    });
    this.categories = Array.from(categorySet).sort();
  }

  onSearchChange(searchTerm: string): void {
    // Update local search term immediately for UI responsiveness
    this.searchTerm = searchTerm;
    // Emit to subject for debounced search
    this.searchSubject.next(searchTerm);
  }

  onFilterChange(): void {
    this.currentPage = 1; // Reset to first page on filter change
    this.loadMedications();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategory = '';
    this.selectedStockStatus = '';
    this.selectedRequiresPrescription = '';
    this.selectedActiveStatus = '';
    this.currentPage = 1;
    this.loadMedications();
  }

  // Pagination methods
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadMedications();
      // Scroll to top of table
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
    this.pageSize = size;
    this.currentPage = 1; // Reset to first page when changing page size
    this.loadMedications();
  }

  /**
   * Generate smart pagination array with ellipsis for many pages
   * Returns array of page numbers and ellipsis strings
   * Example: [1, 2, 3, '...', 10] or [1, '...', 8, 9, 10]
   */
  getPageNumbers(): (number | string)[] {
    const pages: (number | string)[] = [];
    const maxVisiblePages = 7;

    if (this.totalPages <= maxVisiblePages) {
      // Show all pages if few pages
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Smart pagination with ellipsis
      if (this.currentPage <= 4) {
        // Near beginning: 1, 2, 3, 4, 5, ..., last
        for (let i = 1; i <= 5; i++) {
          pages.push(i);
        }
        pages.push('...');
        pages.push(this.totalPages);
      } else if (this.currentPage >= this.totalPages - 3) {
        // Near end: 1, ..., last-4, last-3, last-2, last-1, last
        pages.push(1);
        pages.push('...');
        for (let i = this.totalPages - 4; i <= this.totalPages; i++) {
          pages.push(i);
        }
      } else {
        // Middle: 1, ..., current-1, current, current+1, ..., last
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

  /**
   * Handle page number click - only navigate if it's a number
   */
  onPageNumberClick(pageNum: number | string): void {
    if (typeof pageNum === 'number') {
      this.goToPage(pageNum);
    }
  }

  /**
   * Check if page number is ellipsis
   */
  isEllipsis(pageNum: number | string): boolean {
    return pageNum === '...';
  }

  /**
   * TrackBy function for page numbers to improve performance
   */
  trackByPageNum(index: number, pageNum: number | string): number | string {
    return pageNum;
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
