/**
 * Query string names aligned with backend PharmacyQueryParameters (ASP.NET camelCase binding).
 * @see backend/eBolnicaAPI/eBolnicaAPI/Models/DTOs/PharmacyQueryParameters.cs
 */
export const PHARMACY_MEDICATION_QUERY_PARAMS = {
  pageNumber: 'pageNumber',
  pageSize: 'pageSize',
  sortBy: 'sortBy',
  sortOrder: 'sortOrder',
  searchTerm: 'searchTerm',
  category: 'category',
  stockStatus: 'stockStatus',
  requiresPrescription: 'requiresPrescription',
  isActive: 'isActive',
  minPrice: 'minPrice',
  maxPrice: 'maxPrice',
} as const;

export type PharmacyMedicationQueryParam =
  (typeof PHARMACY_MEDICATION_QUERY_PARAMS)[keyof typeof PHARMACY_MEDICATION_QUERY_PARAMS];
