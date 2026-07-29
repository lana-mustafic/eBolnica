import {
  clampPageNumber,
  clampPageSize,
  normalizePaginationParams,
  PHARMACY_PAGE_SIZE_MAX,
  PHARMACY_PAGE_SIZE_MIN
} from './pagination-params.util';

describe('pagination-params.util', () => {
  it('clampPageNumber defaults invalid values to 1', () => {
    expect(clampPageNumber(undefined)).toBe(1);
    expect(clampPageNumber(null)).toBe(1);
    expect(clampPageNumber(0)).toBe(1);
    expect(clampPageNumber(-5)).toBe(1);
    expect(clampPageNumber(3)).toBe(3);
  });

  it('clampPageSize enforces backend bounds 1-100', () => {
    expect(clampPageSize(undefined)).toBe(10);
    expect(clampPageSize(0)).toBe(PHARMACY_PAGE_SIZE_MIN);
    expect(clampPageSize(-10)).toBe(PHARMACY_PAGE_SIZE_MIN);
    expect(clampPageSize(200)).toBe(PHARMACY_PAGE_SIZE_MAX);
    expect(clampPageSize(50)).toBe(50);
  });

  it('normalizePaginationParams clamps both values', () => {
    expect(normalizePaginationParams(-2, 250)).toEqual({
      pageNumber: 1,
      pageSize: 100
    });
  });
});
