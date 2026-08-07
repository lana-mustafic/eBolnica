import type { SortOrder } from './medication-list-query.util';

export function toggleSortColumn(
  current: { sortBy: string; sortOrder: SortOrder },
  column: string
): { sortBy: string; sortOrder: SortOrder } {
  if (current.sortBy === column) {
    return {
      sortBy: column,
      sortOrder: current.sortOrder === 'asc' ? 'desc' : 'asc',
    };
  }

  return { sortBy: column, sortOrder: 'asc' };
}

export function sortIndicator(sortBy: string, sortOrder: SortOrder, column: string): string {
  if (sortBy !== column) {
    return '';
  }

  return sortOrder === 'asc' ? ' ▲' : ' ▼';
}

export function sortAriaSort(
  sortBy: string,
  sortOrder: SortOrder,
  column: string
): 'ascending' | 'descending' | 'none' {
  if (sortBy !== column) {
    return 'none';
  }

  return sortOrder === 'asc' ? 'ascending' : 'descending';
}

export function onTableSortKeydown(
  event: KeyboardEvent,
  column: string,
  onSort: (column: string) => void
): void {
  if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault();
    onSort(column);
  }
}

export function canGoToPage(page: number, totalPages: number): boolean {
  return page >= 1 && page <= totalPages;
}
