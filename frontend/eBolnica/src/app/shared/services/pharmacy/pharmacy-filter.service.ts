import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ActiveFilter, PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { PharmacyFilterContext } from './pharmacy-filter-context.model';

/**
 * Manages isolated filter state per pharmacy list page (medications, inventory, prescriptions).
 */
@Injectable({
  providedIn: 'root'
})
export class PharmacyFilterService {
  private static readonly PAGINATION_KEYS = new Set<keyof PharmacyFilters>(['pageNumber', 'pageSize']);

  private readonly filtersByContext = new Map<PharmacyFilterContext, BehaviorSubject<PharmacyFilters>>();

  private getContextSubject(context: PharmacyFilterContext): BehaviorSubject<PharmacyFilters> {
    let subject = this.filtersByContext.get(context);
    if (!subject) {
      subject = new BehaviorSubject<PharmacyFilters>({
        pageNumber: 1,
        pageSize: 10
      });
      this.filtersByContext.set(context, subject);
    }
    return subject;
  }

  getFilters$(context: PharmacyFilterContext): Observable<PharmacyFilters> {
    return this.getContextSubject(context).pipe(
      debounceTime(200),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
    );
  }

  getFilters(context: PharmacyFilterContext): PharmacyFilters {
    return this.getContextSubject(context).value;
  }

  updateFilters(context: PharmacyFilterContext, updates: Partial<PharmacyFilters>): void {
    const subject = this.getContextSubject(context);
    const current = subject.value;

    const hasNonPaginationChange = Object.keys(updates).some(key => {
      const filterKey = key as keyof PharmacyFilters;
      if (PharmacyFilterService.PAGINATION_KEYS.has(filterKey)) {
        return false;
      }
      return updates[filterKey] !== current[filterKey];
    });

    const sanitizedUpdates = { ...updates };
    if (hasNonPaginationChange) {
      delete sanitizedUpdates.pageNumber;
    }

    const merged: PharmacyFilters = {
      ...current,
      ...sanitizedUpdates,
      ...(hasNonPaginationChange ? { pageNumber: 1 } : {})
    };

    this.cleanFilters(merged);
    subject.next(merged);
  }

  updateSort(context: PharmacyFilterContext, sortBy: string, sortOrder: 'asc' | 'desc'): void {
    this.updateFilters(context, { sortBy, sortOrder });
  }

  getSortParams(context: PharmacyFilterContext): Pick<PharmacyFilters, 'sortBy' | 'sortOrder'> {
    const { sortBy, sortOrder } = this.getContextSubject(context).value;
    return { sortBy, sortOrder };
  }

  clearFilters(context: PharmacyFilterContext): void {
    const defaultState: PharmacyFilters = {
      pageNumber: 1,
      pageSize: 10,
      searchTerm: undefined,
      category: undefined,
      status: undefined,
      requiresPrescription: undefined,
      isActive: undefined,
      minPrice: undefined,
      maxPrice: undefined,
      stockStatus: undefined,
      minStock: undefined,
      maxStock: undefined,
      expiryStatus: undefined,
      expiryAfter: undefined,
      expiryBefore: undefined,
      prescriptionStatus: undefined,
      urgency: undefined,
      supplier: undefined,
      sortBy: undefined,
      sortOrder: undefined
    };

    this.getContextSubject(context).next(defaultState);
  }

  clearAllFilters(context: PharmacyFilterContext): void {
    this.clearFilters(context);
  }

  clearFilter(context: PharmacyFilterContext, key: keyof PharmacyFilters): void {
    const subject = this.getContextSubject(context);
    const current = subject.value;
    const updated = { ...current };

    if (key !== 'pageNumber' && key !== 'pageSize') {
      delete updated[key];
      updated.pageNumber = 1;
    }

    subject.next(updated);
  }

  syncPaginationFromResponse(context: PharmacyFilterContext, pageNumber: number, pageSize: number): void {
    const subject = this.getContextSubject(context);
    const current = subject.value;

    if (current.pageNumber === pageNumber && current.pageSize === pageSize) {
      return;
    }

    subject.next({
      ...current,
      pageNumber,
      pageSize
    });
  }

  getActiveFilterCount(context: PharmacyFilterContext): number {
    const filters = this.getContextSubject(context).value;
    let count = 0;

    if (filters.searchTerm?.trim()) count++;
    if (filters.category) count++;
    if (filters.status) count++;
    if (filters.stockStatus) count++;
    else if (filters.minStock !== undefined || filters.maxStock !== undefined) count++;
    if (filters.expiryStatus) count++;
    if (filters.requiresPrescription !== undefined) count++;
    if (filters.isActive !== undefined) count++;
    if (filters.minPrice !== undefined) count++;
    if (filters.maxPrice !== undefined) count++;
    if (filters.prescriptionStatus) count++;
    if (filters.urgency) count++;
    if (filters.supplier) count++;

    return count;
  }

  clearFilterByBadgeKey(context: PharmacyFilterContext, key: string): void {
    if (key === 'priceRange') {
      this.updateFilters(context, { minPrice: undefined, maxPrice: undefined });
      return;
    }

    this.clearFilter(context, key as keyof PharmacyFilters);
  }

  getActiveFilters(context: PharmacyFilterContext): ActiveFilter[] {
    const filters = this.getContextSubject(context).value;
    const active: ActiveFilter[] = [];

    if (filters.searchTerm?.trim()) {
      active.push({
        key: 'searchTerm',
        label: 'Search',
        value: filters.searchTerm.trim(),
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
        value: this.formatStockStatusLabel(filters.stockStatus),
        type: 'dropdown'
      });
    } else if (filters.minStock !== undefined || filters.maxStock !== undefined) {
      active.push({
        key: 'stockStatus',
        label: 'Stock Status',
        value: 'Critical Stock',
        type: 'dropdown'
      });
    }

    if (filters.expiryStatus) {
      active.push({
        key: 'expiryStatus',
        label: 'Expiry Status',
        value: this.formatExpiryStatusLabel(filters.expiryStatus),
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

    if (filters.minPrice !== undefined) {
      active.push({
        key: 'minPrice',
        label: 'Min Price',
        value: this.formatCurrency(filters.minPrice),
        type: 'range'
      });
    }

    if (filters.maxPrice !== undefined) {
      active.push({
        key: 'maxPrice',
        label: 'Max Price',
        value: this.formatCurrency(filters.maxPrice),
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

  private formatExpiryStatusLabel(value: string): string {
    const labels: Record<string, string> = {
      good: 'Good (>90 days)',
      warning: 'Warning (30-90 days)',
      critical: 'Expiring Soon (<30 days)',
      expired: 'Expired'
    };

    return labels[value] ?? value;
  }

  private formatStockStatusLabel(value: string): string {
    const labels: Record<string, string> = {
      'low stock': 'Low Stock',
      'critical stock': 'Critical Stock',
      'out of stock': 'Out of Stock',
      'normal stock': 'Normal Stock'
    };

    return labels[value.toLowerCase()] ?? value;
  }

  private formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  }

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
