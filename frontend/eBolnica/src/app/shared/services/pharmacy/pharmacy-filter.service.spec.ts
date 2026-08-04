import { PharmacyFilterService } from './pharmacy-filter.service';

describe('PharmacyFilterService inventory sort', () => {
  let service: PharmacyFilterService;
  const inventoryContext = 'inventory' as const;

  beforeEach(() => {
    service = new PharmacyFilterService();
  });

  it('updateSort stores sortBy and sortOrder in filter state', () => {
    service.updateSort(inventoryContext, 'expiryDate', 'desc');

    expect(service.getSortParams(inventoryContext)).toEqual({
      sortBy: 'expiryDate',
      sortOrder: 'desc'
    });
  });

  it('updateSort resets pageNumber to 1', () => {
    service.updateFilters(inventoryContext, { pageNumber: 3, category: 'painkiller' });

    service.updateSort(inventoryContext, 'name', 'asc');

    expect(service.getFilters(inventoryContext).pageNumber).toBe(1);
    expect(service.getFilters(inventoryContext).category).toBe('painkiller');
    expect(service.getFilters(inventoryContext).sortBy).toBe('name');
    expect(service.getFilters(inventoryContext).sortOrder).toBe('asc');
  });

  it('preserves sort when only pageNumber changes', () => {
    service.updateFilters(inventoryContext, {
      sortBy: 'expiryDate',
      sortOrder: 'desc',
      category: 'antibiotics',
      pageNumber: 1
    });

    service.updateFilters(inventoryContext, { pageNumber: 2 });

    expect(service.getFilters(inventoryContext)).toEqual(
      jasmine.objectContaining({
        sortBy: 'expiryDate',
        sortOrder: 'desc',
        category: 'antibiotics',
        pageNumber: 2
      })
    );
  });

  it('clearFilters removes sort params', () => {
    service.updateSort(inventoryContext, 'stockQuantity', 'asc');

    service.clearFilters(inventoryContext);

    expect(service.getSortParams(inventoryContext)).toEqual({});
  });

  it('isolates filter state between list contexts', () => {
    service.updateFilters('medications', { searchTerm: 'aspirin', pageNumber: 3 });
    service.updateFilters('prescriptions', { prescriptionStatus: 'Pending', pageNumber: 2 });

    expect(service.getFilters('medications').searchTerm).toBe('aspirin');
    expect(service.getFilters('medications').pageNumber).toBe(3);
    expect(service.getFilters('prescriptions').prescriptionStatus).toBe('Pending');
    expect(service.getFilters('prescriptions').searchTerm).toBeUndefined();
  });
});
