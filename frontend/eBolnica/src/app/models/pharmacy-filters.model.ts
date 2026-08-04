/**
 * Unified filter interface for Pharmacy module
 * All filters are combined with AND logic
 */
export interface PharmacyFilters {
  // Search
  searchTerm?: string;

  // Medication filters
  category?: string;
  status?: string;
  requiresPrescription?: boolean;
  isActive?: boolean;
  minPrice?: number;
  maxPrice?: number;
  stockStatus?: string;
  minStock?: number;
  maxStock?: number;

  // Inventory expiry filters (expiryStatus is UI value; expiryAfter/expiryBefore are API params)
  expiryStatus?: string;
  expiryAfter?: string;
  expiryBefore?: string;

  // Prescription filters
  prescriptionStatus?: string;
  urgency?: string;

  // Inventory filters
  supplier?: string;

  // Pagination
  pageNumber: number;
  pageSize: number;

  // Sorting
  sortBy?: string;
  sortOrder?: string;
}

/**
 * Active filter badge for display
 */
export interface ActiveFilter {
  key: string;
  label: string;
  value: string;
  type: 'search' | 'dropdown' | 'range' | 'boolean';
}
