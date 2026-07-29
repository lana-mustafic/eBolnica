import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { getPageRangeEnd, getPageRangeStart } from '../../../shared/utils/paged-response.util';

/**
 * Reusable component for displaying filter state summary
 * Shows result count, loading state, and clear filters button
 */
@Component({
  selector: 'app-filter-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './filter-summary.component.html',
  styleUrl: './filter-summary.component.css'
})
export class FilterSummaryComponent {
  @Input() filters: PharmacyFilters | null = null;
  @Input() totalCount: number = 0;
  @Input() currentPage: number = 1;
  @Input() pageSize: number = 10;
  @Input() isLoading: boolean = false;
  @Input() activeFilterCount: number = 0;
  @Input() totalUnfilteredCount?: number; // Optional: total count without filters

  @Input() totalPages: number = 0;

  @Output() clearAll = new EventEmitter<void>();

  get hasActiveFilters(): boolean {
    return this.activeFilterCount > 0;
  }

  get startIndex(): number {
    return getPageRangeStart(this.currentPage, this.pageSize, this.totalCount);
  }

  get endIndex(): number {
    return getPageRangeEnd(this.currentPage, this.pageSize, this.totalCount);
  }

  get pageCountLabel(): string {
    if (this.totalCount === 0) {
      return '';
    }

    if (this.totalPages <= 1) {
      return '';
    }

    return `(page ${this.currentPage} of ${this.totalPages})`;
  }

  getResultText(): string {
    if (this.isLoading) {
      return 'Loading results...';
    }

    if (this.totalCount === 0) {
      return 'No results found';
    }

    if (this.totalCount === 1) {
      return 'Showing 1 result';
    }

    const baseText = `Showing ${this.startIndex}-${this.endIndex} of ${this.totalCount} result${this.totalCount !== 1 ? 's' : ''}${this.pageCountLabel ? ` ${this.pageCountLabel}` : ''}`;
    
    if (this.hasActiveFilters && this.totalUnfilteredCount && this.totalUnfilteredCount > this.totalCount) {
      return `${baseText} (filtered from ${this.totalUnfilteredCount} total)`;
    }

    return baseText;
  }

  onClearAll(): void {
    if (this.hasActiveFilters) {
      this.clearAll.emit();
    }
  }

  getClearButtonText(): string {
    if (this.activeFilterCount === 0) {
      return 'Clear all';
    }
    if (this.activeFilterCount === 1) {
      return `Clear all (1 filter)`;
    }
    return `Clear all (${this.activeFilterCount} filters)`;
  }
}
