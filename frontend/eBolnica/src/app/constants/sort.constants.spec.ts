import {
  INVENTORY_SORT_COLUMN_MAP,
  mapInventorySortColumn
} from './sort.constants';

describe('sort.constants inventory mappings', () => {
  it('maps UI column keys to backend sortBy fields', () => {
    expect(mapInventorySortColumn('name')).toBe('name');
    expect(mapInventorySortColumn('medicationName')).toBe('name');
    expect(mapInventorySortColumn('stock')).toBe('stockQuantity');
    expect(mapInventorySortColumn('quantity')).toBe('stockQuantity');
    expect(mapInventorySortColumn('stockStatus')).toBe('stockQuantity');
    expect(mapInventorySortColumn('expiryDate')).toBe('expiryDate');
    expect(mapInventorySortColumn('createdAt')).toBe('createdAt');
  });

  it('passes through unknown columns unchanged', () => {
    expect(mapInventorySortColumn('unknownColumn')).toBe('unknownColumn');
  });

  it('defines aliases for every inventory sortable header key', () => {
    expect(INVENTORY_SORT_COLUMN_MAP.name).toBe('name');
    expect(INVENTORY_SORT_COLUMN_MAP.stock).toBe('stockQuantity');
    expect(INVENTORY_SORT_COLUMN_MAP.stockStatus).toBe('stockQuantity');
    expect(INVENTORY_SORT_COLUMN_MAP.expiryDate).toBe('expiryDate');
  });
});
