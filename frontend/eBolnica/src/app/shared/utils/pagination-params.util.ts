/** Matches backend PharmacyController.NormalizePagination bounds. */
export const PHARMACY_PAGE_SIZE_MIN = 1;
export const PHARMACY_PAGE_SIZE_MAX = 100;
export const PHARMACY_PAGE_SIZE_DEFAULT = 10;

export interface NormalizedPaginationParams {
  pageNumber: number;
  pageSize: number;
}

export function clampPageNumber(pageNumber?: number | null): number {
  const value = pageNumber ?? 1;
  return value < 1 ? 1 : Math.floor(value);
}

export function clampPageSize(
  pageSize?: number | null,
  defaultSize: number = PHARMACY_PAGE_SIZE_DEFAULT
): number {
  const value = pageSize ?? defaultSize;
  return Math.max(
    PHARMACY_PAGE_SIZE_MIN,
    Math.min(PHARMACY_PAGE_SIZE_MAX, Math.floor(value))
  );
}

export function normalizePaginationParams(
  pageNumber?: number | null,
  pageSize?: number | null,
  defaultPageSize: number = PHARMACY_PAGE_SIZE_DEFAULT
): NormalizedPaginationParams {
  return {
    pageNumber: clampPageNumber(pageNumber),
    pageSize: clampPageSize(pageSize, defaultPageSize)
  };
}
