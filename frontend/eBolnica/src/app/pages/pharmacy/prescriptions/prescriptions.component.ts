import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService, PrescriptionFilterParams } from '../../../shared/services/pharmacy/pharmacy.service';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize, switchMap, takeUntil, tap } from 'rxjs';

@Component({
  selector: 'app-prescriptions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './prescriptions.component.html',
  styleUrl: './prescriptions.component.css'
})
export class PrescriptionsComponent implements OnInit, OnDestroy {
  private pharmacyService = inject(PharmacyService);

  prescriptions: PrescriptionDto[] = [];
  isLoading: boolean = false;
  isSearching: boolean = false; // Separate flag for search loading
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

  ngOnInit(): void {
    this.loadPrescriptions();
    
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
        return this.pharmacyService.getPrescriptions(filters).pipe(
          finalize(() => this.isSearching = false)
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        this.prescriptions = response.data || [];
        this.totalCount = response.totalCount || 0;
        this.totalPages = response.totalPages || 0;
        this.currentPage = response.page || 1;
        this.pageSize = response.pageSize || 10;
        this.updateFilterCounts();
        this.errorMessage = null;
      },
      error: (error) => {
        this.isSearching = false;
        this.errorMessage = 'Search failed. Please try again.';
        console.error('Error searching prescriptions:', error);
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
  private buildFilterParams(): PrescriptionFilterParams {
    const filters: PrescriptionFilterParams = {
      page: this.currentPage,
      pageSize: this.pageSize
    };

    if (this.selectedStatus && this.selectedStatus !== 'All') {
      filters.status = this.selectedStatus;
    }

    if (this.searchTerm.trim()) {
      filters.search = this.searchTerm.trim();
    }

    return filters;
  }

  loadPrescriptions(): void {
    this.isLoading = true;
    this.errorMessage = null;

    const filters = this.buildFilterParams();

    this.pharmacyService.getPrescriptions(filters).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (response) => {
        this.prescriptions = response.data || [];
        this.totalCount = response.totalCount || 0;
        this.totalPages = response.totalPages || 0;
        this.currentPage = response.page || 1;
        this.pageSize = response.pageSize || 10;
        this.updateFilterCounts();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load prescriptions. Please try again later.';
        console.error('Error loading prescriptions:', error);
      }
    });
  }

  updateFilterCounts(): void {
    // Note: With pagination, we only have counts for current page
    // For accurate counts, would need separate API calls or total counts from backend
    this.statusFilters.forEach(filter => {
      if (filter.value === 'All') {
        filter.count = this.totalCount; // Use total count from backend
      } else {
        // Approximate count based on current page (not perfect but better than nothing)
        const pageCount = this.prescriptions.filter(p => p.status === filter.value).length;
        // Estimate: if we have 10 items per page and 3 match, estimate total
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
    this.currentPage = 1; // Reset to first page on filter change
    this.loadPrescriptions();
  }

  onSearchChange(searchTerm: string): void {
    // Update local search term immediately for UI responsiveness
    this.searchTerm = searchTerm;
    // Emit to subject for debounced search
    this.searchSubject.next(searchTerm);
  }

  onSortChange(sortBy: string): void {
    if (this.sortBy === sortBy) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = sortBy;
      this.sortOrder = 'desc';
    }
    // Note: Sorting is now handled by backend, but we can still apply client-side sorting as fallback
    // For now, reload with current filters
    this.loadPrescriptions();
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
}
