import { PharmacyFilterService } from './pharmacy-filter.service';

describe('PharmacyFilterService inventory sort', () => {
  let service: PharmacyFilterService;

  beforeEach(() => {
    service = new PharmacyFilterService();
  });

  it('updateSort stores sortBy and sortOrder in filter state', () => {
    service.updateSort('expiryDate', 'desc');

    expect(service.getSortParams()).toEqual({
      sortBy: 'expiryDate',
      sortOrder: 'desc'
    });
  });

  it('updateSort resets pageNumber to 1', () => {
    service.updateFilters({ pageNumber: 3, category: 'painkiller' });

    service.updateSort('name', 'asc');

    expect(service.getFilters().pageNumber).toBe(1);
    expect(service.getFilters().category).toBe('painkiller');
    expect(service.getFilters().sortBy).toBe('name');
    expect(service.getFilters().sortOrder).toBe('asc');
  });

  it('preserves sort when only pageNumber changes', () => {
    service.updateFilters({
      sortBy: 'expiryDate',
      sortOrder: 'desc',
      category: 'antibiotics',
      pageNumber: 1
    });

    service.updateFilters({ pageNumber: 2 });

    expect(service.getFilters()).toEqual(
      jasmine.objectContaining({
        sortBy: 'expiryDate',
        sortOrder: 'desc',
        category: 'antibiotics',
        pageNumber: 2
      })
    );
  });

  it('clearFilters removes sort params', () => {
    service.updateSort('stockQuantity', 'asc');

    service.clearFilters();

    expect(service.getSortParams()).toEqual({});
  });
});
