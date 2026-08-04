import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActiveFilter, PharmacyFilters } from '../../../models/pharmacy-filters.model';

/**
 * Reusable component for displaying active filter badges
 * Shows each active filter as a removable badge
 */
@Component({
  selector: 'app-active-filters',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './active-filters.component.html',
  styleUrl: './active-filters.component.css'
})
export class ActiveFiltersComponent {
  @Input() filters: PharmacyFilters | null = null;
  @Input() activeFilters: ActiveFilter[] = [];

  @Output() removeFilter = new EventEmitter<string>();

  get hasActiveFilters(): boolean {
    return this.activeFilters.length > 0;
  }

  onRemoveFilter(filterKey: string): void {
    this.removeFilter.emit(filterKey);
  }

  getBadgeClass(filterType: ActiveFilter['type']): string {
    return `filter-badge filter-badge-${filterType}`;
  }

  trackByFilterKey(_: number, filter: ActiveFilter): string {
    return filter.key;
  }
}
