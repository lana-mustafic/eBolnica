import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { MedicationDto } from '../../../models/medication.dto';
import { MedicationCreateDto } from '../../../models/medication-create.dto';
import { PharmacistDataDto } from '../../../models/pharmacist-data.dto';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { PrescriptionCreateDto } from '../../../models/prescription-create.dto';
import { PrescriptionDispenseDto } from '../../../models/prescription-dispense.dto';
import { PagedResponse } from '../../../models/paged-response.dto';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';

/**
 * Filter parameters for medication queries
 */
export interface MedicationFilterParams {
  /** Filter by category (exact match, case-insensitive) */
  category?: string;
  /** Search term for name, generic name, or manufacturer (case-insensitive) */
  search?: string;
  /** Stock status filter: 'low stock', 'out of stock', or 'normal stock' */
  stockStatus?: string;
  /** Filter by prescription requirement */
  requiresPrescription?: boolean;
  /** Filter by active status (default: true - show active only) */
  isActive?: boolean;
  /** Page number (1-based, default: 1) */
  page?: number;
  /** Items per page (default: 10, range: 5-100) */
  pageSize?: number;
}

/**
 * Filter parameters for prescription queries
 */
export interface PrescriptionFilterParams {
  /** Filter by status (exact match) */
  status?: string;
  /** Search term for patient name, doctor name, medication name, or prescription code (case-insensitive) */
  search?: string;
  /** Page number (1-based, default: 1) */
  page?: number;
  /** Items per page (default: 10, range: 5-100) */
  pageSize?: number;
}

/**
 * Filter parameters for inventory queries
 */
export interface InventoryFilterParams {
  /** Filter by category (exact match, case-insensitive) */
  category?: string;
  /** Search term for medication name, batch number, or supplier (case-insensitive) */
  search?: string;
  /** Page number (1-based, default: 1) */
  page?: number;
  /** Items per page (default: 10, range: 5-100) */
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class PharmacyService {

  private apiUrl = 'http://localhost:5004/api/pharmacy';
  private http = inject(HttpClient);

  // Medications CRUD
  
  /**
   * Get medications with optional filtering and pagination
   * @param filters Optional filter parameters object
   * @returns Observable of paginated medication response
   */
  getAllMedications(filters?: MedicationFilterParams): Observable<PagedResponse<MedicationDto>> {
    // Set defaults
    const page = filters?.page || 1;
    const pageSize = Math.max(5, Math.min(100, filters?.pageSize || 10)); // Clamp between 5-100
    
    // Build query parameters
    // Backend accepts both 'page' (backward compatibility) and 'pageNumber'
    let params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', pageSize.toString());
    
    // Category filter
    if (filters?.category) {
      params = params.set('category', filters.category);
    }
    
    // Search filter (trim and only add if not empty)
    if (filters?.search) {
      const searchTerm = filters.search.trim();
      if (searchTerm) {
        params = params.set('search', searchTerm);
      }
    }
    
    // Stock status filter
    if (filters?.stockStatus) {
      // Validate stock status values
      const validStatuses = ['low stock', 'out of stock', 'normal stock'];
      const normalizedStatus = filters.stockStatus.toLowerCase();
      if (validStatuses.includes(normalizedStatus)) {
        params = params.set('stockStatus', normalizedStatus);
      }
    }
    
    // Requires prescription filter (only add if explicitly provided)
    if (filters?.requiresPrescription !== undefined && filters?.requiresPrescription !== null) {
      params = params.set('requiresPrescription', filters.requiresPrescription.toString());
    }
    
    // Active status filter (only add if explicitly provided, backend defaults to true if not provided)
    if (filters?.isActive !== undefined && filters?.isActive !== null) {
      params = params.set('isActive', filters.isActive.toString());
    }
    
    return this.http.get<PagedResponse<MedicationDto>>(`${this.apiUrl}/medications`, { params });
  }

  getMedicationById(id: number): Observable<MedicationDto> {
    return this.http.get<MedicationDto>(this.apiUrl + `/medications/${id}`);
  }

  createMedication(medication: MedicationCreateDto): Observable<MedicationDto> {
    return this.http.post<MedicationDto>(this.apiUrl + '/medications', medication);
  }

  updateMedication(id: number, medication: MedicationCreateDto): Observable<MedicationDto> {
    return this.http.put<MedicationDto>(this.apiUrl + `/medications/${id}`, medication);
  }

  deleteMedication(id: number): Observable<any> {
    return this.http.delete(this.apiUrl + `/medications/${id}`);
  }

  // Prescriptions Management
  /**
   * Get prescriptions with optional filtering, search, and pagination
   * @param filters Optional filter parameters object
   * @returns Observable of paginated prescription response
   */
  getPrescriptions(filters?: PrescriptionFilterParams): Observable<PagedResponse<PrescriptionDto>> {
    const page = filters?.page || 1;
    const pageSize = Math.max(5, Math.min(100, filters?.pageSize || 10));
    
    let params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (filters?.status) {
      params = params.set('status', filters.status);
    }
    
    // Search filter (trim and only add if not empty)
    if (filters?.search) {
      const searchTerm = filters.search.trim();
      if (searchTerm) {
        params = params.set('search', searchTerm);
      }
    }
    
    return this.http.get<PagedResponse<PrescriptionDto>>(`${this.apiUrl}/prescriptions`, { params });
  }

  getPrescriptionById(id: number): Observable<PrescriptionDto> {
    return this.http.get<PrescriptionDto>(this.apiUrl + `/prescriptions/${id}`);
  }

  createPrescription(prescription: PrescriptionCreateDto): Observable<PrescriptionDto> {
    return this.http.post<PrescriptionDto>(this.apiUrl + '/prescriptions', prescription);
  }

  dispensePrescription(id: number, data: PrescriptionDispenseDto): Observable<PrescriptionDto> {
    return this.http.post<PrescriptionDto>(this.apiUrl + `/prescriptions/${id}/dispense`, data);
  }

  // Inventory & Pharmacist Data
  /**
   * Get inventory with optional filtering, search, and pagination
   * @param filters Optional filter parameters object
   * @returns Observable of paginated inventory response with alerts
   */
  getInventory(filters?: InventoryFilterParams): Observable<PagedResponse<MedicationDto> & { LowStockAlerts: MedicationDto[]; ExpiryAlerts: MedicationDto[] }> {
    const page = filters?.page || 1;
    const pageSize = Math.max(5, Math.min(100, filters?.pageSize || 10));
    
    let params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (filters?.category) {
      params = params.set('category', filters.category);
    }
    
    // Search filter (trim and only add if not empty)
    if (filters?.search) {
      const searchTerm = filters.search.trim();
      if (searchTerm) {
        params = params.set('search', searchTerm);
      }
    }
    
    return this.http.get<any>(this.apiUrl + '/inventory', { params });
  }

  getPharmacistData(): Observable<PharmacistDataDto> {
    return this.http.get<PharmacistDataDto>(this.apiUrl + '/pharmacist-data');
  }

  // Unified Filter Methods

  /**
   * Get medications using unified PharmacyFilters
   * Maps PharmacyFilters to API query parameters
   * Includes error handling and logging
   */
  getMedicationsWithFilters(filters: PharmacyFilters): Observable<PagedResponse<MedicationDto>> {
    const params = this.buildMedicationQueryParams(filters);
    
    // Log API call for debugging
    console.log('[PharmacyService] Loading medications with filters:', filters);
    console.log('[PharmacyService] Query params:', params.toString());
    
    return this.http.get<PagedResponse<MedicationDto>>(`${this.apiUrl}/medications`, { params }).pipe(
      tap(response => {
        console.log('[PharmacyService] Medications loaded:', {
          count: response.items?.length || 0,
          total: response.totalCount,
          page: response.currentPage
        });
      }),
      catchError(this.handleError.bind(this))
    );
  }

  /**
   * Get prescriptions using unified PharmacyFilters
   * Includes error handling and logging
   */
  getPrescriptionsWithFilters(filters: PharmacyFilters): Observable<PagedResponse<PrescriptionDto>> {
    const params = this.buildPrescriptionQueryParams(filters);
    
    console.log('[PharmacyService] Loading prescriptions with filters:', filters);
    
    return this.http.get<PagedResponse<PrescriptionDto>>(`${this.apiUrl}/prescriptions`, { params }).pipe(
      tap(response => {
        console.log('[PharmacyService] Prescriptions loaded:', {
          count: response.items?.length || 0,
          total: response.totalCount,
          page: response.currentPage
        });
      }),
      catchError(this.handleError.bind(this))
    );
  }

  /**
   * Get inventory using unified PharmacyFilters
   * Includes error handling and logging
   */
  getInventoryWithFilters(filters: PharmacyFilters): Observable<PagedResponse<MedicationDto> & { LowStockAlerts: MedicationDto[]; ExpiryAlerts: MedicationDto[] }> {
    const params = this.buildInventoryQueryParams(filters);
    
    console.log('[PharmacyService] Loading inventory with filters:', filters);
    
    return this.http.get<any>(`${this.apiUrl}/inventory`, { params }).pipe(
      tap(response => {
        console.log('[PharmacyService] Inventory loaded:', {
          count: response.items?.length || 0,
          total: response.totalCount,
          lowStockAlerts: response.LowStockAlerts?.length || 0,
          expiryAlerts: response.ExpiryAlerts?.length || 0
        });
      }),
      catchError(this.handleError.bind(this))
    );
  }

  /**
   * Build query parameters for medications from PharmacyFilters
   */
  private buildMedicationQueryParams(filters: PharmacyFilters): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', (filters.pageNumber || 1).toString())
      .set('pageSize', Math.max(5, Math.min(100, filters.pageSize || 10)).toString());

    // Search
    if (filters.searchTerm?.trim()) {
      params = params.set('search', filters.searchTerm.trim());
    }

    // Category
    if (filters.category) {
      params = params.set('category', filters.category);
    }

    // Stock status
    if (filters.stockStatus) {
      const validStatuses = ['low stock', 'out of stock', 'normal stock'];
      const normalizedStatus = filters.stockStatus.toLowerCase();
      if (validStatuses.includes(normalizedStatus)) {
        params = params.set('stockStatus', normalizedStatus);
      }
    }

    // Requires prescription
    if (filters.requiresPrescription !== undefined && filters.requiresPrescription !== null) {
      params = params.set('requiresPrescription', filters.requiresPrescription.toString());
    }

    // Active status
    if (filters.isActive !== undefined && filters.isActive !== null) {
      params = params.set('isActive', filters.isActive.toString());
    }

    // Price range
    if (filters.minPrice !== undefined && filters.minPrice !== null) {
      params = params.set('minPrice', filters.minPrice.toString());
    }
    if (filters.maxPrice !== undefined && filters.maxPrice !== null) {
      params = params.set('maxPrice', filters.maxPrice.toString());
    }

    // Sorting
    if (filters.sortBy) {
      params = params.set('sortBy', filters.sortBy);
    }
    if (filters.sortOrder) {
      params = params.set('sortOrder', filters.sortOrder);
    }

    return params;
  }

  /**
   * Build query parameters for prescriptions from PharmacyFilters
   */
  private buildPrescriptionQueryParams(filters: PharmacyFilters): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', (filters.pageNumber || 1).toString())
      .set('pageSize', Math.max(5, Math.min(100, filters.pageSize || 10)).toString());

    // Search (with URL encoding)
    if (filters.searchTerm?.trim()) {
      const encodedSearch = this.encodeSearchTerm(filters.searchTerm.trim());
      params = params.set('search', encodedSearch);
    }

    // Status (prescriptionStatus maps to status)
    if (filters.prescriptionStatus && filters.prescriptionStatus !== 'All') {
      params = params.set('status', filters.prescriptionStatus);
    }

    // Sorting
    if (filters.sortBy) {
      params = params.set('sortBy', filters.sortBy);
    }
    if (filters.sortOrder) {
      params = params.set('sortOrder', filters.sortOrder);
    }

    return params;
  }

  /**
   * Build query parameters for inventory from PharmacyFilters
   */
  private buildInventoryQueryParams(filters: PharmacyFilters): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', (filters.pageNumber || 1).toString())
      .set('pageSize', Math.max(5, Math.min(100, filters.pageSize || 10)).toString());

    // Search (with URL encoding)
    if (filters.searchTerm?.trim()) {
      const encodedSearch = this.encodeSearchTerm(filters.searchTerm.trim());
      params = params.set('search', encodedSearch);
    }

    // Category
    if (filters.category) {
      params = params.set('category', filters.category);
    }

    // Stock status
    if (filters.stockStatus) {
      params = params.set('stockStatus', filters.stockStatus);
    }

    // Sorting
    if (filters.sortBy) {
      params = params.set('sortBy', filters.sortBy);
    }
    if (filters.sortOrder) {
      params = params.set('sortOrder', filters.sortOrder);
    }

    return params;
  }

  /**
   * Encode search term to handle special characters safely
   */
  private encodeSearchTerm(term: string): string {
    // HttpParams automatically encodes, but we can add additional validation
    // Remove potentially dangerous characters while preserving search functionality
    return term.replace(/[<>]/g, ''); // Remove angle brackets for safety
  }

  /**
   * Handle HTTP errors with proper error messages
   */
  private handleError(error: HttpErrorResponse): Observable<never> {
    console.error('[PharmacyService] API Error:', error);

    let errorMessage = 'An error occurred while loading data';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error (network, timeout, etc.)
      errorMessage = `Network error: ${error.error.message}`;
      console.error('[PharmacyService] Client-side error:', error.error);
    } else {
      // Server-side error
      const status = error.status;
      const statusText = error.statusText;
      
      switch (status) {
        case 400:
          errorMessage = 'Invalid request. Please check your filters and try again.';
          if (error.error?.message) {
            errorMessage = error.error.message;
          }
          break;
        case 401:
          errorMessage = 'Unauthorized. Please log in again.';
          break;
        case 403:
          errorMessage = 'Access denied. You do not have permission to access this resource.';
          break;
        case 404:
          errorMessage = 'Resource not found.';
          break;
        case 500:
          errorMessage = 'Server error. Please try again later.';
          break;
        case 0:
          // Network error or CORS issue
          errorMessage = 'Network error. Please check your connection and try again.';
          break;
        default:
          errorMessage = `Error ${status}: ${statusText || 'Unknown error'}`;
          if (error.error?.message) {
            errorMessage = error.error.message;
          }
      }
      
      console.error(`[PharmacyService] Server error (${status}):`, error.error);
    }

    // Return an observable that emits an error
    return throwError(() => ({
      message: errorMessage,
      status: error.status,
      originalError: error
    }));
  }
}
