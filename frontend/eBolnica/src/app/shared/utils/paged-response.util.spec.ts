import { normalizeInventoryResponse } from './paged-response.util';

describe('normalizeInventoryResponse', () => {
  it('normalizes camelCase inventory payloads', () => {
    const result = normalizeInventoryResponse({
      items: [{ id: 1 }],
      totalCount: 12,
      currentPage: 2,
      pageSize: 5,
      totalPages: 3,
      hasNext: true,
      hasPrevious: true,
      lowStockAlerts: [{ id: 2 }],
      expiryAlerts: [{ id: 3 }]
    }, 5);

    expect(result.items).toEqual([{ id: 1 }]);
    expect(result.totalCount).toBe(12);
    expect(result.currentPage).toBe(2);
    expect(result.lowStockAlerts).toEqual([{ id: 2 }]);
    expect(result.expiryAlerts).toEqual([{ id: 3 }]);
  });

  it('normalizes PascalCase inventory payloads', () => {
    const result = normalizeInventoryResponse({
      Items: [{ id: 4 }],
      TotalCount: 1,
      CurrentPage: 1,
      PageSize: 10,
      LowStockAlerts: [{ id: 5 }],
      ExpiryAlerts: [{ id: 6 }]
    });

    expect(result.items).toEqual([{ id: 4 }]);
    expect(result.totalCount).toBe(1);
    expect(result.lowStockAlerts).toEqual([{ id: 5 }]);
    expect(result.expiryAlerts).toEqual([{ id: 6 }]);
  });
});
