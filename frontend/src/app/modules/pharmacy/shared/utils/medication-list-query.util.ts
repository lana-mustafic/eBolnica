export type SortOrder = 'asc' | 'desc';

export interface MedicationListFilters {
  search: string;
  selectedCategory: string;
  selectedStockStatus: string;
  selectedRequiresPrescription: string;
  showInactive?: boolean;
}

export interface MedicationListSort {
  sortBy: string;
  sortOrder: SortOrder;
}

export interface MedicationListPagination {
  pageNumber: number;
  pageSize: number;
}

export function parseRequiresPrescriptionFilter(value: string): boolean | undefined {
  if (value === 'true') {
    return true;
  }

  if (value === 'false') {
    return false;
  }

  return undefined;
}

export function hasMedicationListFilters(filters: MedicationListFilters): boolean {
  return !!(
    filters.search ||
    filters.selectedCategory ||
    filters.selectedStockStatus ||
    filters.selectedRequiresPrescription ||
    filters.showInactive
  );
}

export function clearMedicationListFilters(filters: MedicationListFilters): void {
  filters.search = '';
  filters.selectedCategory = '';
  filters.selectedStockStatus = '';
  filters.selectedRequiresPrescription = '';
  filters.showInactive = false;
}

export function buildMedicationListQuery(
  filters: MedicationListFilters,
  pagination: MedicationListPagination,
  sort: MedicationListSort,
  options: { includeActiveFlags?: boolean } = {}
) {
  const query = {
    search: filters.search || undefined,
    category: filters.selectedCategory || undefined,
    stockStatus: filters.selectedStockStatus || undefined,
    requiresPrescription: parseRequiresPrescriptionFilter(filters.selectedRequiresPrescription),
    pageNumber: pagination.pageNumber,
    pageSize: pagination.pageSize,
    sortBy: sort.sortBy,
    sortOrder: sort.sortOrder,
  };

  if (!options.includeActiveFlags) {
    return query;
  }

  return {
    ...query,
    isActive: filters.showInactive ? undefined : true,
    includeInactive: filters.showInactive ?? false,
  };
}
