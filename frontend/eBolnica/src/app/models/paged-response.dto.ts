export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
  hasNext?: boolean;
  hasPrevious?: boolean;
}
