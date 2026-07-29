import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PharmacyService } from './pharmacy.service';
import { PHARMACY_MEDICATION_QUERY_PARAMS as MED_Q } from '../../../constants/pharmacy-query-params.constants';

describe('PharmacyService inventory sort mapping', () => {
  let service: PharmacyService;
  let httpMock: HttpTestingController;

  const emptyInventoryResponse = {
    items: [],
    totalCount: 0,
    currentPage: 1,
    pageSize: 10,
    totalPages: 0,
    lowStockAlerts: [],
    expiryAlerts: []
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PharmacyService]
    });

    service = TestBed.inject(PharmacyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('maps UI stock column to backend stockQuantity in inventory request', () => {
    service.getInventoryWithFilters({
      pageNumber: 1,
      pageSize: 10,
      sortBy: 'stock',
      sortOrder: 'asc'
    }).subscribe();

    const req = httpMock.expectOne(
      request => request.url.endsWith('/api/pharmacy/inventory')
    );

    expect(req.request.params.get(MED_Q.sortBy)).toBe('stockQuantity');
    expect(req.request.params.get(MED_Q.sortOrder)).toBe('asc');
    req.flush(emptyInventoryResponse);
  });

  it('maps stockStatus alias to stockQuantity', () => {
    service.getInventoryWithFilters({
      pageNumber: 1,
      pageSize: 10,
      sortBy: 'stockStatus',
      sortOrder: 'desc'
    }).subscribe();

    const req = httpMock.expectOne(
      request => request.url.endsWith('/api/pharmacy/inventory')
    );

    expect(req.request.params.get(MED_Q.sortBy)).toBe('stockQuantity');
    expect(req.request.params.get(MED_Q.sortOrder)).toBe('desc');
    req.flush(emptyInventoryResponse);
  });

  it('passes through backend sort columns unchanged', () => {
    service.getInventoryWithFilters({
      pageNumber: 1,
      pageSize: 10,
      sortBy: 'expiryDate',
      sortOrder: 'desc'
    }).subscribe();

    const req = httpMock.expectOne(
      request => request.url.endsWith('/api/pharmacy/inventory')
    );

    expect(req.request.params.get(MED_Q.sortBy)).toBe('expiryDate');
    expect(req.request.params.get(MED_Q.sortOrder)).toBe('desc');
    req.flush(emptyInventoryResponse);
  });

  it('omits invalid sortOrder values', () => {
    service.getInventoryWithFilters({
      pageNumber: 1,
      pageSize: 10,
      sortBy: 'name',
      sortOrder: 'invalid' as 'asc'
    }).subscribe();

    const req = httpMock.expectOne(
      request => request.url.endsWith('/api/pharmacy/inventory')
    );

    expect(req.request.params.get(MED_Q.sortBy)).toBe('name');
    expect(req.request.params.has(MED_Q.sortOrder)).toBeFalse();
    req.flush(emptyInventoryResponse);
  });

  it('omits sort params when sortBy is not provided', () => {
    service.getInventoryWithFilters({
      pageNumber: 1,
      pageSize: 10
    }).subscribe();

    const req = httpMock.expectOne(
      request => request.url.endsWith('/api/pharmacy/inventory')
    );

    expect(req.request.params.has(MED_Q.sortBy)).toBeFalse();
    expect(req.request.params.has(MED_Q.sortOrder)).toBeFalse();
    req.flush(emptyInventoryResponse);
  });

  it('sends sort, filters, and paging together in one inventory query', () => {
    service.getInventoryWithFilters({
      pageNumber: 2,
      pageSize: 25,
      searchTerm: 'amox',
      category: 'antibiotics',
      stockStatus: 'low stock',
      expiryStatus: 'warning',
      sortBy: 'stock',
      sortOrder: 'desc'
    }).subscribe();

    const req = httpMock.expectOne(
      request => request.url.endsWith('/api/pharmacy/inventory')
    );

    expect(req.request.params.get(MED_Q.pageNumber)).toBe('2');
    expect(req.request.params.get(MED_Q.pageSize)).toBe('25');
    expect(req.request.params.get(MED_Q.searchTerm)).toBe('amox');
    expect(req.request.params.get('category')).toBe('antibiotics');
    expect(req.request.params.get('stockStatus')).toBe('low stock');
    expect(req.request.params.get('expiryStatus')).toBe('warning');
    expect(req.request.params.get(MED_Q.sortBy)).toBe('stockQuantity');
    expect(req.request.params.get(MED_Q.sortOrder)).toBe('desc');
    req.flush(emptyInventoryResponse);
  });

  it('sends medications sort, filters, and paging together in one query', () => {
    service.getMedicationsWithFilters({
      pageNumber: 3,
      pageSize: 20,
      searchTerm: 'aspirin',
      category: 'painkiller',
      stockStatus: 'normal stock',
      sortBy: 'price',
      sortOrder: 'asc'
    }).subscribe();

    const req = httpMock.expectOne(
      request => request.url.endsWith('/api/pharmacy/medications')
    );

    expect(req.request.params.get(MED_Q.pageNumber)).toBe('3');
    expect(req.request.params.get(MED_Q.pageSize)).toBe('20');
    expect(req.request.params.get(MED_Q.searchTerm)).toBe('aspirin');
    expect(req.request.params.get(MED_Q.category)).toBe('painkiller');
    expect(req.request.params.get(MED_Q.stockStatus)).toBe('normal stock');
    expect(req.request.params.get(MED_Q.sortBy)).toBe('price');
    expect(req.request.params.get(MED_Q.sortOrder)).toBe('asc');
    req.flush({
      items: [],
      totalCount: 0,
      currentPage: 3,
      pageSize: 20,
      totalPages: 0
    });
  });

  it('sends prescriptions sort, filters, and paging together in one query', () => {
    service.getPrescriptionsWithFilters({
      pageNumber: 2,
      pageSize: 15,
      searchTerm: 'john',
      prescriptionStatus: 'Pending',
      sortBy: 'prescribedDate',
      sortOrder: 'desc'
    }).subscribe();

    const req = httpMock.expectOne(
      request => request.url.endsWith('/api/pharmacy/prescriptions')
    );

    expect(req.request.params.get('pageNumber')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('15');
    expect(req.request.params.get(MED_Q.searchTerm)).toBe('john');
    expect(req.request.params.get('status')).toBe('Pending');
    expect(req.request.params.get('sortBy')).toBe('prescribedDate');
    expect(req.request.params.get('sortOrder')).toBe('desc');
    req.flush({
      items: [],
      totalCount: 0,
      currentPage: 2,
      pageSize: 15,
      totalPages: 0
    });
  });
});
