import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';

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

  @Output() clearAll = new EventEmitter<void>();

  get hasActiveFilters(): boolean {
    return this.activeFilterCount > 0;
  }

  get startIndex(): number {
    if (this.totalCount === 0) return 0;
    return ((this.currentPage - 1) * this.pageSize) + 1;
  }

  get endIndex(): number {
    if (this.totalCount === 0) return 0;
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
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

    const baseText = `Showing ${this.startIndex}-${this.endIndex} of ${this.totalCount} result${this.totalCount !== 1 ? 's' : ''}`;
    
    if (this.hasActiveFilters && this.totalUnfilteredCount && this.totalUnfilteredCount > this.totalCount) {
      return `${baseText} (filtered from ${this.totalUnfilteredCount} total)`;
    }

    return baseText;
  }

  onClearAll(): void {
    this.clearAll.emit();
  }
}
