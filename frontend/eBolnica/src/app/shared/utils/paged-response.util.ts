import { PagedResponse } from '../../models/paged-response.dto';

type RawPagedResponse<T> = Partial<PagedResponse<T>> & Record<string, unknown>;

/**
 * Normalizes API pagination metadata to the frontend PagedResponse shape.
 * Supports camelCase and PascalCase payloads from the backend.
 */
export function normalizePagedResponse<T>(
  raw: RawPagedResponse<T> | null | undefined,
  defaultPageSize = 10
): PagedResponse<T> {
  const items = (raw?.items ?? raw?.['Items'] ?? []) as T[];
  const totalCount = toNumber(raw?.totalCount ?? raw?.['TotalCount'], 0);
  const pageSize = Math.max(1, toNumber(raw?.pageSize ?? raw?.['PageSize'], defaultPageSize));
  const currentPage = Math.max(1, toNumber(raw?.currentPage ?? raw?.['CurrentPage'], 1));

  let totalPages = toNumber(raw?.totalPages ?? raw?.['TotalPages'], -1);
  if (totalPages < 0) {
    totalPages = pageSize > 0 ? Math.ceil(totalCount / pageSize) : 0;
  }

  const hasNext = raw?.hasNext ?? raw?.['HasNext'];
  const hasPrevious = raw?.hasPrevious ?? raw?.['HasPrevious'];

  return {
    items,
    totalCount,
    currentPage,
    pageSize,
    totalPages,
    hasNext: typeof hasNext === 'boolean' ? hasNext : currentPage < totalPages,
    hasPrevious: typeof hasPrevious === 'boolean' ? hasPrevious : currentPage > 1
  };
}

export function getPageRangeStart(currentPage: number, pageSize: number, totalCount: number): number {
  if (totalCount <= 0) {
    return 0;
  }

  return ((currentPage - 1) * pageSize) + 1;
}

export function getPageRangeEnd(currentPage: number, pageSize: number, totalCount: number): number {
  if (totalCount <= 0) {
    return 0;
  }

  return Math.min(currentPage * pageSize, totalCount);
}

export function getPageRangeLabel(currentPage: number, pageSize: number, totalCount: number): string {
  if (totalCount <= 0) {
    return 'No results found';
  }

  const start = getPageRangeStart(currentPage, pageSize, totalCount);
  const end = getPageRangeEnd(currentPage, pageSize, totalCount);
  const noun = totalCount === 1 ? 'result' : 'results';

  return `Showing ${start}-${end} of ${totalCount} ${noun}`;
}

function toNumber(value: unknown, fallback: number): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}
