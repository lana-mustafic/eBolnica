import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { debounceTime, distinctUntilChanged, map } from 'rxjs/operators';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';

/**
 * Service to manage unified filter state for Pharmacy module
 * Handles filter combination with AND logic
 */
@Injectable({
  providedIn: 'root'
})
export class PharmacyFilterService {
  private filters$ = new BehaviorSubject<PharmacyFilters>({
    pageNumber: 1,
    pageSize: 10
  });

  /**
   * Get current filter state as observable
   * Debounced to prevent excessive updates
   */
  getFilters$(): Observable<PharmacyFilters> {
    return this.filters$.pipe(
      debounceTime(200),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
    );
  }

  /**
   * Get current filter state synchronously
   */
  getFilters(): PharmacyFilters {
    return this.filters$.value;
  }

  /**
   * Update filters with partial updates
   * Automatically resets pageNumber to 1 when filters change
   */
  updateFilters(updates: Partial<PharmacyFilters>): void {
    const current = this.filters$.value;
    
    // Check if any filter (except pagination) is changing
    const hasFilterChange = Object.keys(updates).some(key => 
      key !== 'pageNumber' && key !== 'pageSize' && 
      updates[key as keyof PharmacyFilters] !== current[key as keyof PharmacyFilters]
    );

    // Reset page to 1 if filters changed (not just pagination)
    const merged: PharmacyFilters = {
      ...current,
      ...updates,
      ...(hasFilterChange ? { pageNumber: 1 } : {})
    };

    // Remove null/undefined/empty string values
    this.cleanFilters(merged);
    
    this.filters$.next(merged);
  }

  /**
   * Clear all filters and reset to defaults
   */
  clearFilters(): void {
    this.filters$.next({
      pageNumber: 1,
      pageSize: 10
    });
  }

  /**
   * Clear specific filter by key
   */
  clearFilter(key: keyof PharmacyFilters): void {
    const current = this.filters$.value;
    const updated = { ...current };
    
    // Don't clear pagination or pageSize
    if (key !== 'pageNumber' && key !== 'pageSize') {
      delete updated[key];
      updated.pageNumber = 1; // Reset to first page
    }
    
    this.filters$.next(updated);
  }

  /**
   * Get count of active filters (excluding pagination)
   */
  getActiveFilterCount(): number {
    const filters = this.filters$.value;
    let count = 0;
    
    if (filters.searchTerm?.trim()) count++;
    if (filters.category) count++;
    if (filters.status) count++;
    if (filters.stockStatus) count++;
    if (filters.requiresPrescription !== undefined) count++;
    if (filters.isActive !== undefined) count++;
    if (filters.minPrice !== undefined) count++;
    if (filters.maxPrice !== undefined) count++;
    if (filters.prescriptionStatus) count++;
    if (filters.urgency) count++;
    if (filters.supplier) count++;
    
    return count;
  }

  /**
   * Get list of active filters for display
   */
  getActiveFilters(): Array<{ key: string; label: string; value: string; type: string }> {
    const filters = this.filters$.value;
    const active: Array<{ key: string; label: string; value: string; type: string }> = [];

    if (filters.searchTerm?.trim()) {
      active.push({
        key: 'searchTerm',
        label: 'Search',
        value: filters.searchTerm,
        type: 'search'
      });
    }

    if (filters.category) {
      active.push({
        key: 'category',
        label: 'Category',
        value: filters.category,
        type: 'dropdown'
      });
    }

    if (filters.status) {
      active.push({
        key: 'status',
        label: 'Status',
        value: filters.status,
        type: 'dropdown'
      });
    }

    if (filters.stockStatus) {
      active.push({
        key: 'stockStatus',
        label: 'Stock Status',
        value: filters.stockStatus,
        type: 'dropdown'
      });
    }

    if (filters.requiresPrescription !== undefined) {
      active.push({
        key: 'requiresPrescription',
        label: 'Prescription',
        value: filters.requiresPrescription ? 'Required' : 'Not Required',
        type: 'boolean'
      });
    }

    if (filters.isActive !== undefined) {
      active.push({
        key: 'isActive',
        label: 'Active Status',
        value: filters.isActive ? 'Active' : 'Inactive',
        type: 'boolean'
      });
    }

    if (filters.minPrice !== undefined || filters.maxPrice !== undefined) {
      const range = [
        filters.minPrice !== undefined ? `$${filters.minPrice}` : '',
        filters.maxPrice !== undefined ? `$${filters.maxPrice}` : ''
      ].filter(Boolean).join(' - ');
      
      active.push({
        key: 'priceRange',
        label: 'Price Range',
        value: range,
        type: 'range'
      });
    }

    if (filters.prescriptionStatus) {
      active.push({
        key: 'prescriptionStatus',
        label: 'Prescription Status',
        value: filters.prescriptionStatus,
        type: 'dropdown'
      });
    }

    if (filters.urgency) {
      active.push({
        key: 'urgency',
        label: 'Urgency',
        value: filters.urgency,
        type: 'dropdown'
      });
    }

    if (filters.supplier) {
      active.push({
        key: 'supplier',
        label: 'Supplier',
        value: filters.supplier,
        type: 'dropdown'
      });
    }

    return active;
  }

  /**
   * Remove null, undefined, and empty string values from filters
   */
  private cleanFilters(filters: PharmacyFilters): void {
    Object.keys(filters).forEach(key => {
      const value = filters[key as keyof PharmacyFilters];
      if (value === null || value === undefined || value === '') {
        if (key !== 'pageNumber' && key !== 'pageSize') {
          delete filters[key as keyof PharmacyFilters];
        }
      }
    });
  }
}
