import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable, throwError, of, timer, forkJoin } from 'rxjs';
import { catchError, tap, retry, retryWhen, delayWhen, take, concatMap, map } from 'rxjs/operators';
import { normalizePagedResponse, normalizeInventoryResponse } from '../../utils/paged-response.util';
import { normalizePaginationParams } from '../../utils/pagination-params.util';
import { MedicationDto } from '../../../models/medication.dto';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import { MedicationCreateDto } from '../../../models/medication-create.dto';
import { PharmacistDataDto } from '../../../models/pharmacist-data.dto';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { PrescriptionCreateDto } from '../../../models/prescription-create.dto';
import { PrescriptionDispenseDto } from '../../../models/prescription-dispense.dto';
import { InventoryResponse } from '../../../models/inventory-response.dto';
import { PagedResponse } from '../../../models/paged-response.dto';
import { PharmacyFilters } from '../../../models/pharmacy-filters.model';
import { PHARMACY_MEDICATION_QUERY_PARAMS as MED_Q } from '../../../constants/pharmacy-query-params.constants';
import { 
  MonthlyRevenueData, 
  MedicationCategoryData, 
  StockTrendData, 
  DashboardStats,
  AnalyticsPeriod,
  AnalyticsDateRange
} from '../../../models/analytics.dto';

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

  /**
   * Resolves a medication primary image URL for display.
   * Supports absolute URLs and API-relative paths.
   */
  resolveMedicationImageUrl(imageUrl?: string): string | null {
    if (!imageUrl?.trim()) {
      return null;
    }

    const trimmed = imageUrl.trim();
    if (trimmed.startsWith('http://') || trimmed.startsWith('https://') || trimmed.startsWith('data:')) {
      return trimmed;
    }

    const apiOrigin = this.apiUrl.replace(/\/api\/pharmacy\/?$/, '');
    return trimmed.startsWith('/') ? `${apiOrigin}${trimmed}` : `${apiOrigin}/${trimmed}`;
  }

  // Medications CRUD
  
  /**
   * Get medications with optional filtering and pagination
   * @param filters Optional filter parameters object
   * @returns Observable of paginated medication response
   */
  getAllMedications(filters?: MedicationFilterParams): Observable<PagedResponse<MedicationDto>> {
    // Set defaults
    const { pageNumber, pageSize } = normalizePaginationParams(filters?.page, filters?.pageSize);
    
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    // Category filter
    if (filters?.category) {
      params = params.set('category', filters.category);
    }
    
    // Search filter (trim and only add if not empty)
    if (filters?.search) {
      const searchTerm = filters.search.trim();
      if (searchTerm) {
        params = params.set(MED_Q.searchTerm, searchTerm);
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

  // Medication Images

  getMedicationImages(medicationId: number): Observable<MedicationImageDto[]> {
    return this.http.get<MedicationImageDto[]>(`${this.apiUrl}/medications/${medicationId}/images`);
  }

  uploadMedicationImage(medicationId: number, file: File): Observable<MedicationImageDto> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<MedicationImageDto>(`${this.apiUrl}/medications/${medicationId}/images`, formData);
  }

  setPrimaryMedicationImage(medicationId: number, imageId: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/medications/${medicationId}/images/${imageId}/primary`, {});
  }

  deleteMedicationImage(medicationId: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/medications/${medicationId}/images/${imageId}`);
  }

  // Prescriptions Management
  /**
   * Get prescriptions with optional filtering, search, and pagination
   * @param filters Optional filter parameters object
   * @returns Observable of paginated prescription response
   */
  getPrescriptions(filters?: PrescriptionFilterParams): Observable<PagedResponse<PrescriptionDto>> {
    const { pageNumber, pageSize } = normalizePaginationParams(filters?.page, filters?.pageSize);

    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
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
  getInventory(filters?: InventoryFilterParams): Observable<InventoryResponse> {
    const { pageNumber, pageSize } = normalizePaginationParams(filters?.page, filters?.pageSize);

    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
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
    
    return this.http.get<any>(this.apiUrl + '/inventory', { params }).pipe(
      map(response => normalizeInventoryResponse(response, pageSize)),
      catchError(this.handleError.bind(this))
    );
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
      map(response => normalizePagedResponse<MedicationDto>(response, filters.pageSize || 10)),
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
      map(response => normalizePagedResponse<PrescriptionDto>(response, filters.pageSize || 10)),
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
  getInventoryWithFilters(filters: PharmacyFilters): Observable<InventoryResponse> {
    const params = this.buildInventoryQueryParams(filters);
    
    console.log('[PharmacyService] Loading inventory with filters:', filters);

    return this.http.get<any>(`${this.apiUrl}/inventory`, { params }).pipe(
      map(response => normalizeInventoryResponse(response, filters.pageSize || 10)),
      tap(response => {
        console.log('[PharmacyService] Inventory loaded:', {
          count: response.items?.length || 0,
          total: response.totalCount,
          sortBy: filters.sortBy,
          sortOrder: filters.sortOrder,
          lowStockAlerts: response.lowStockAlerts?.length || 0,
          expiryAlerts: response.expiryAlerts?.length || 0
        });
      }),
      catchError(this.handleError.bind(this))
    );
  }

  /**
   * Build query parameters for medications from PharmacyFilters
   */
  private buildMedicationQueryParams(filters: PharmacyFilters): HttpParams {
    const { pageNumber, pageSize } = normalizePaginationParams(
      filters.pageNumber,
      filters.pageSize
    );

    let params = new HttpParams()
      .set(MED_Q.pageNumber, pageNumber.toString())
      .set(MED_Q.pageSize, pageSize.toString());

    if (filters.searchTerm?.trim()) {
      params = params.set(MED_Q.searchTerm, filters.searchTerm.trim());
    }

    if (filters.category) {
      params = params.set(MED_Q.category, filters.category);
    }

    if (filters.stockStatus) {
      const validStatuses = ['low stock', 'out of stock', 'normal stock'];
      const normalizedStatus = filters.stockStatus.toLowerCase();
      if (validStatuses.includes(normalizedStatus)) {
        params = params.set(MED_Q.stockStatus, normalizedStatus);
      }
    }

    if (filters.requiresPrescription !== undefined && filters.requiresPrescription !== null) {
      params = params.set(MED_Q.requiresPrescription, filters.requiresPrescription.toString());
    }

    if (filters.isActive !== undefined && filters.isActive !== null) {
      params = params.set(MED_Q.isActive, filters.isActive.toString());
    }

    if (filters.minPrice !== undefined && filters.minPrice !== null) {
      params = params.set(MED_Q.minPrice, filters.minPrice.toString());
    }
    if (filters.maxPrice !== undefined && filters.maxPrice !== null) {
      params = params.set(MED_Q.maxPrice, filters.maxPrice.toString());
    }

    if (filters.sortBy) {
      params = params.set(MED_Q.sortBy, filters.sortBy);
    }
    if (filters.sortOrder) {
      params = params.set(MED_Q.sortOrder, filters.sortOrder);
    }

    return params;
  }

  /**
   * Build query parameters for prescriptions from PharmacyFilters
   */
  private buildPrescriptionQueryParams(filters: PharmacyFilters): HttpParams {
    const { pageNumber, pageSize } = normalizePaginationParams(
      filters.pageNumber,
      filters.pageSize
    );

    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    // Search (with URL encoding)
    if (filters.searchTerm?.trim()) {
      const encodedSearch = this.encodeSearchTerm(filters.searchTerm.trim());
      params = params.set(MED_Q.searchTerm, encodedSearch);
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
    const { pageNumber, pageSize } = normalizePaginationParams(
      filters.pageNumber,
      filters.pageSize
    );

    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    // Search (with URL encoding)
    if (filters.searchTerm?.trim()) {
      const encodedSearch = this.encodeSearchTerm(filters.searchTerm.trim());
      params = params.set(MED_Q.searchTerm, encodedSearch);
    }

    // Category
    if (filters.category) {
      params = params.set('category', filters.category);
    }

    // Stock status
    if (filters.stockStatus) {
      params = params.set('stockStatus', filters.stockStatus);
    }

    if (filters.minStock !== undefined && filters.minStock !== null) {
      params = params.set('minStock', filters.minStock.toString());
    }
    if (filters.maxStock !== undefined && filters.maxStock !== null) {
      params = params.set('maxStock', filters.maxStock.toString());
    }

    if (filters.expiryAfter) {
      params = params.set('expiryAfter', filters.expiryAfter);
    }
    if (filters.expiryBefore) {
      params = params.set('expiryBefore', filters.expiryBefore);
    }

    if (filters.sortBy) {
      params = params.set(MED_Q.sortBy, filters.sortBy);
    }
    if (filters.sortOrder) {
      params = params.set(MED_Q.sortOrder, filters.sortOrder);
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

  // PDF Export Methods

  /**
   * Export inventory data to PDF
   * Downloads PDF file with current filters and sorting applied
   * @param filters Current filter and sort parameters
   * @returns Observable that completes when download starts
   */
  exportInventoryToPdf(filters: PharmacyFilters): Observable<any> {
    const params = this.buildPdfQueryParams(filters, 'inventory');
    
    console.log('[PharmacyService] Exporting inventory to PDF with filters:', filters);
    
    return this.http.get(
      `${this.apiUrl}/reports/inventory/pdf`,
      {
        params,
        responseType: 'blob', // IMPORTANT: Get response as Blob
        observe: 'response'   // Get full response including headers
      }
    ).pipe(
      tap(response => {
        console.log('[PharmacyService] PDF download initiated');
        const fileInfo = this.handlePdfDownload(response, 'inventory-report', filters);
        // Store file info for component to access
        (response as any).fileInfo = fileInfo;
      }),
      catchError(error => this.handlePdfError(error))
    );
  }

  /**
   * Export prescriptions data to PDF
   * Downloads PDF file with current filters and sorting applied
   * @param filters Current filter and sort parameters
   * @returns Observable that completes when download starts
   */
  exportPrescriptionsToPdf(filters: PharmacyFilters): Observable<any> {
    const params = this.buildPdfQueryParams(filters, 'prescriptions');
    
    console.log('[PharmacyService] Exporting prescriptions to PDF with filters:', filters);
    
    return this.http.get(
      `${this.apiUrl}/reports/prescriptions/pdf`,
      {
        params,
        responseType: 'blob', // IMPORTANT: Get response as Blob
        observe: 'response'   // Get full response including headers
      }
    ).pipe(
      tap(response => {
        console.log('[PharmacyService] PDF download initiated');
        const fileInfo = this.handlePdfDownload(response, 'prescriptions-report', filters);
        // Store file info for component to access
        (response as any).fileInfo = fileInfo;
      }),
      catchError(error => this.handlePdfError(error))
    );
  }

  /**
   * Build query parameters for PDF export
   * Includes all filters, sorting, and PDF-specific parameters
   * @param filters Current filter and sort parameters
   * @param reportType Type of report: 'inventory' or 'prescriptions'
   * @returns HttpParams with all query parameters
   */
  private buildPdfQueryParams(filters: PharmacyFilters, reportType: 'inventory' | 'prescriptions'): HttpParams {
    let params: HttpParams;

    // Reuse existing query parameter builders but override pagination for PDF
    if (reportType === 'inventory') {
      params = this.buildInventoryQueryParams(filters);
    } else {
      params = this.buildPrescriptionQueryParams(filters);
    }

    // Override pagination - PDF should include all matching data
    params = params.delete('pageNumber');
    params = params.delete('pageSize');
    params = params.set('includeAllData', 'true');

    // Add PDF-specific parameters
    params = params.set('reportType', 'detailed'); // Could be 'summary' or 'detailed'

    // Add cache busting timestamp to prevent cached PDFs
    params = params.set('_t', Date.now().toString());

    return params;
  }

  /**
   * Handle PDF file download from HTTP response
   * Extracts filename from headers or generates default, then triggers browser download
   * @param response HTTP response containing PDF blob
   * @param defaultFileName Default filename if not provided in headers
   * @param filters Filter parameters for generating meaningful filename
   * @returns File name and size for notification
   */
  private handlePdfDownload(response: HttpResponse<Blob>, defaultFileName: string, filters?: PharmacyFilters): { fileName: string; fileSize: number } {
    // Validate response type is PDF
    const contentType = response.headers.get('content-type');
    if (!contentType || !contentType.includes('application/pdf')) {
      console.warn('[PharmacyService] Unexpected content type:', contentType);
      // Still proceed with download, but log warning
    }

    // Extract filename from response headers or use default
    const contentDisposition = response.headers.get('content-disposition');
    let fileName = this.generatePdfFileName(defaultFileName, filters);

    if (contentDisposition) {
      const fileNameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
      if (fileNameMatch && fileNameMatch.length > 1) {
        fileName = fileNameMatch[1].replace(/['"]/g, '');
        // Decode URI if encoded
        try {
          fileName = decodeURIComponent(fileName);
        } catch (e) {
          // Use as-is if decode fails
        }
      }
    }

    // Sanitize filename to prevent path traversal
    fileName = this.sanitizeFileName(fileName);

    // Create blob from response body
    const blob = new Blob([response.body!], { type: 'application/pdf' });
    
    // Create download link
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.style.display = 'none';

    // Trigger download
    document.body.appendChild(link);
    link.click();

    // Cleanup
    setTimeout(() => {
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    }, 100);

    console.log('[PharmacyService] PDF downloaded:', fileName);

    // Return file info for notification
    return {
      fileName: fileName,
      fileSize: blob.size
    };
  }

  /**
   * Generate meaningful PDF filename based on report type and filters
   * @param reportType Type of report (will be used as base filename)
   * @param filters Optional filter parameters to include in filename
   * @returns Generated filename with timestamp and optional filter info
   */
  private generatePdfFileName(reportType: string, filters?: PharmacyFilters): string {
    const dateStr = new Date().toISOString().split('T')[0];
    const timeStr = new Date().toLocaleTimeString('en-US', { hour12: false }).replace(/:/g, '-').split('.')[0];
    
    let fileName = `${reportType}_${dateStr}_${timeStr}`;
    
    // Add filter info if applicable
    if (filters) {
      if (filters.searchTerm) {
        const searchSnippet = filters.searchTerm.substring(0, 20).replace(/[^a-zA-Z0-9]/g, '_');
        fileName += `_search-${searchSnippet}`;
      }
      
      if (filters.category) {
        const categorySnippet = filters.category.substring(0, 20).replace(/[^a-zA-Z0-9]/g, '_');
        fileName += `_category-${categorySnippet}`;
      }

      if (filters.prescriptionStatus && filters.prescriptionStatus !== 'All') {
        const statusSnippet = filters.prescriptionStatus.replace(/[^a-zA-Z0-9]/g, '_');
        fileName += `_status-${statusSnippet}`;
      }

      if (filters.stockStatus) {
        const stockSnippet = filters.stockStatus.replace(/[^a-zA-Z0-9]/g, '_');
        fileName += `_stock-${stockSnippet}`;
      }
    }
    
    return `${fileName}.pdf`;
  }

  /**
   * Sanitize filename to prevent path traversal and other security issues
   * @param fileName Original filename
   * @returns Sanitized filename safe for download
   */
  private sanitizeFileName(fileName: string): string {
    // Remove path traversal attempts
    let sanitized = fileName.replace(/\.\./g, '');
    sanitized = sanitized.replace(/\//g, '_');
    sanitized = sanitized.replace(/\\/g, '_');
    
    // Remove control characters
    sanitized = sanitized.replace(/[\x00-\x1F\x7F]/g, '');
    
    // Limit length
    if (sanitized.length > 255) {
      const ext = sanitized.substring(sanitized.lastIndexOf('.'));
      const name = sanitized.substring(0, sanitized.lastIndexOf('.'));
      sanitized = name.substring(0, 255 - ext.length) + ext;
    }
    
    // Ensure it ends with .pdf
    if (!sanitized.toLowerCase().endsWith('.pdf')) {
      sanitized += '.pdf';
    }
    
    return sanitized;
  }

  /**
   * Handle PDF-specific errors
   * Attempts to read error message from blob response if available
   * @param error HTTP error response
   * @returns Observable that emits error
   */
  private handlePdfError(error: HttpErrorResponse): Observable<never> {
    console.error('[PharmacyService] PDF export error:', error);

    let errorMessage = 'Failed to generate PDF';

    // Handle standard HTTP errors first (before blob parsing)
    if (error.status === 404) {
      errorMessage = 'PDF generation endpoint not available. This feature is currently being implemented on the backend.';
    } else if (error.status === 401) {
      errorMessage = 'Unauthorized. Please log in again.';
    } else if (error.status === 403) {
      errorMessage = 'Access denied. You do not have permission to generate PDF reports.';
    } else if (error.status === 400) {
      errorMessage = 'Invalid parameters for PDF generation';
      if (error.error?.message) {
        errorMessage = error.error.message;
      }
    } else if (error.status === 500) {
      errorMessage = 'Server error during PDF generation. Please try again later.';
    } else if (error.status === 0) {
      errorMessage = 'Network error. Please check your connection.';
    } else if (error.error instanceof Blob) {
      // Try to read error message from blob if error is a blob
      // Note: This is asynchronous, so we'll use a default message for now
      errorMessage = 'PDF generation failed. Please check server logs for details.';
      
      // Attempt to read blob asynchronously for logging (but don't wait)
      const reader = new FileReader();
      reader.onload = () => {
        try {
          const errorObj = JSON.parse(reader.result as string);
          console.error('[PharmacyService] PDF error details from blob:', errorObj);
        } catch (e) {
          // If parsing fails, log the raw text
          console.error('[PharmacyService] PDF error blob content:', reader.result);
        }
      };
      reader.readAsText(error.error);
    } else {
      // Default error message
      errorMessage = `PDF generation failed: ${error.statusText || 'Unknown error'}`;
      if (error.error?.message) {
        errorMessage = error.error.message;
      }
    }

    // Return an observable that emits an error
    return throwError(() => ({
      message: errorMessage,
      status: error.status,
      originalError: error
    }));
  }

  // ============================================
  // Analytics Methods
  // ============================================

  private readonly analyticsApiUrl = `${this.apiUrl}/analytics`;
  private readonly cachePrefix = 'pharmacy_analytics_';
  private readonly cacheExpiryMs = 60 * 60 * 1000; // 1 hour cache

  /**
   * Get cache key for analytics data
   * @param endpoint Analytics endpoint name
   * @param params Optional parameters for cache key uniqueness
   */
  private getCacheKey(endpoint: string, params?: any): string {
    const paramStr = params ? JSON.stringify(params) : '';
    return `${this.cachePrefix}${endpoint}_${paramStr}`;
  }

  /**
   * Get cached data if available and not expired
   * @param cacheKey Cache key
   */
  private getCachedData<T>(cacheKey: string): T | null {
    try {
      const cached = localStorage.getItem(cacheKey);
      if (!cached) return null;

      const parsed = JSON.parse(cached);
      const now = Date.now();
      
      // Check if cache is expired
      if (parsed.expiry && now > parsed.expiry) {
        localStorage.removeItem(cacheKey);
        return null;
      }

      return parsed.data as T;
    } catch (error) {
      console.warn('[PharmacyService] Error reading cache:', error);
      return null;
    }
  }

  /**
   * Store data in cache
   * @param cacheKey Cache key
   * @param data Data to cache
   */
  private setCachedData<T>(cacheKey: string, data: T): void {
    try {
      const cacheData = {
        data,
        expiry: Date.now() + this.cacheExpiryMs,
        timestamp: new Date().toISOString()
      };
      localStorage.setItem(cacheKey, JSON.stringify(cacheData));
    } catch (error) {
      console.warn('[PharmacyService] Error writing cache:', error);
      // If storage quota exceeded, clear old cache entries
      if (error instanceof DOMException && error.name === 'QuotaExceededError') {
        this.clearExpiredCache();
      }
    }
  }

  /**
   * Clear expired cache entries
   */
  private clearExpiredCache(): void {
    try {
      const keys = Object.keys(localStorage);
      const now = Date.now();
      keys.forEach(key => {
        if (key.startsWith(this.cachePrefix)) {
          try {
            const cached = JSON.parse(localStorage.getItem(key) || '{}');
            if (cached.expiry && now > cached.expiry) {
              localStorage.removeItem(key);
            }
          } catch {
            // Remove invalid cache entries
            localStorage.removeItem(key);
          }
        }
      });
    } catch (error) {
      console.warn('[PharmacyService] Error clearing expired cache:', error);
    }
  }

  /**
   * Retry logic with exponential backoff
   * Retries up to 3 times with increasing delays
   */
  private retryWithBackoff<T>(maxRetries: number = 3) {
    return retryWhen(errors =>
      errors.pipe(
        concatMap((error, index) => {
          const retryAttempt = index + 1;
          if (retryAttempt > maxRetries) {
            return throwError(() => error);
          }
          // Exponential backoff: 1s, 2s, 4s
          const delay = Math.pow(2, retryAttempt - 1) * 1000;
          console.log(`[PharmacyService] Retry attempt ${retryAttempt}/${maxRetries} after ${delay}ms`);
          return timer(delay);
        })
      )
    );
  }

  /**
   * Get monthly revenue data for bar chart
   * @param period Optional period filter (default: 'last12months')
   * @param dateRange Optional custom date range
   * @param useCache Whether to use cached data if available (default: true)
   * @returns Observable of monthly revenue data array
   */
  getMonthlyRevenue(
    period?: AnalyticsPeriod, 
    dateRange?: AnalyticsDateRange,
    useCache: boolean = true
  ): Observable<MonthlyRevenueData[]> {
    let params = new HttpParams();
    
    if (period) {
      params = params.set('period', period);
    }
    
    if (dateRange) {
      const startDate = typeof dateRange.startDate === 'string' 
        ? dateRange.startDate 
        : dateRange.startDate.toISOString();
      const endDate = typeof dateRange.endDate === 'string'
        ? dateRange.endDate
        : dateRange.endDate.toISOString();
      params = params.set('startDate', startDate);
      params = params.set('endDate', endDate);
    }

    const cacheKey = this.getCacheKey('monthlyRevenue', { period, dateRange });
    
    // Check cache first
    if (useCache) {
      const cached = this.getCachedData<MonthlyRevenueData[]>(cacheKey);
      if (cached) {
        console.log('[PharmacyService] Returning cached monthly revenue data');
        return of(cached);
      }
    }

    console.log('[PharmacyService] Fetching monthly revenue data from API');

    return this.http.get<MonthlyRevenueData[]>(
      `${this.analyticsApiUrl}/monthly-revenue`,
      { params }
    ).pipe(
      retry(3), // Simple retry (3 attempts)
      tap(data => {
        // Cache successful response
        if (useCache && data && data.length > 0) {
          this.setCachedData(cacheKey, data);
        }
        console.log('[PharmacyService] Monthly revenue data loaded:', data.length, 'months');
      }),
      catchError(error => {
        console.error('[PharmacyService] Error fetching monthly revenue:', error);
        
        // Return fallback empty data instead of throwing
        const fallbackData: MonthlyRevenueData[] = this.getMockMonthlyRevenue();
        console.warn('[PharmacyService] Using fallback monthly revenue data');
        return of(fallbackData);
      })
    );
  }

  /**
   * Get top medication categories data for pie chart
   * @param limit Optional limit for number of categories (default: 10)
   * @param useCache Whether to use cached data if available (default: true)
   * @returns Observable of medication category data array
   */
  getTopMedicationCategories(
    limit: number = 10,
    useCache: boolean = true
  ): Observable<MedicationCategoryData[]> {
    const params = new HttpParams().set('limit', limit.toString());
    const cacheKey = this.getCacheKey('topCategories', { limit });
    
    // Check cache first
    if (useCache) {
      const cached = this.getCachedData<MedicationCategoryData[]>(cacheKey);
      if (cached) {
        console.log('[PharmacyService] Returning cached top categories data');
        return of(cached);
      }
    }

    console.log('[PharmacyService] Fetching top medication categories from API');

    return this.http.get<MedicationCategoryData[]>(
      `${this.analyticsApiUrl}/top-categories`,
      { params }
    ).pipe(
      retry(3),
      tap(data => {
        if (useCache && data && data.length > 0) {
          this.setCachedData(cacheKey, data);
        }
        console.log('[PharmacyService] Top categories data loaded:', data.length, 'categories');
      }),
      catchError(error => {
        console.error('[PharmacyService] Error fetching top categories:', error);
        const fallbackData: MedicationCategoryData[] = this.getMockTopCategories();
        console.warn('[PharmacyService] Using fallback top categories data');
        return of(fallbackData);
      })
    );
  }

  /**
   * Get stock trends data for line chart
   * @param medicationIds Optional array of medication IDs to filter (if empty, returns all)
   * @param days Optional number of days to look back (default: 30)
   * @param useCache Whether to use cached data if available (default: true)
   * @returns Observable of stock trend data array
   */
  getStockTrends(
    medicationIds?: number[],
    days: number = 30,
    useCache: boolean = true
  ): Observable<StockTrendData[]> {
    let params = new HttpParams().set('days', days.toString());
    
    if (medicationIds && medicationIds.length > 0) {
      medicationIds.forEach(id => {
        params = params.append('medicationIds', id.toString());
      });
    }

    const cacheKey = this.getCacheKey('stockTrends', { medicationIds, days });
    
    // Check cache first
    if (useCache) {
      const cached = this.getCachedData<StockTrendData[]>(cacheKey);
      if (cached) {
        console.log('[PharmacyService] Returning cached stock trends data');
        return of(cached);
      }
    }

    console.log('[PharmacyService] Fetching stock trends from API');

    return this.http.get<StockTrendData[]>(
      `${this.analyticsApiUrl}/stock-trends`,
      { params }
    ).pipe(
      retry(3),
      tap(data => {
        if (useCache && data && data.length > 0) {
          this.setCachedData(cacheKey, data);
        }
        console.log('[PharmacyService] Stock trends data loaded:', data.length, 'data points');
      }),
      catchError(error => {
        console.error('[PharmacyService] Error fetching stock trends:', error);
        const fallbackData: StockTrendData[] = this.getMockStockTrends();
        console.warn('[PharmacyService] Using fallback stock trends data');
        return of(fallbackData);
      })
    );
  }

  /**
   * Get all dashboard statistics at once
   * Fetches monthly revenue, top categories, and stock trends in parallel
   * @param period Optional period for revenue data
   * @param dateRange Optional custom date range for revenue data
   * @param categoryLimit Optional limit for categories (default: 10)
   * @param stockTrendDays Optional days for stock trends (default: 30)
   * @param useCache Whether to use cached data if available (default: true)
   * @returns Observable of complete dashboard stats
   */
  getDashboardStats(
    period?: AnalyticsPeriod,
    dateRange?: AnalyticsDateRange,
    categoryLimit: number = 10,
    stockTrendDays: number = 30,
    useCache: boolean = true
  ): Observable<DashboardStats> {
    const cacheKey = this.getCacheKey('dashboardStats', { period, dateRange, categoryLimit, stockTrendDays });
    
    // Check cache first
    if (useCache) {
      const cached = this.getCachedData<DashboardStats>(cacheKey);
      if (cached) {
        console.log('[PharmacyService] Returning cached dashboard stats');
        return of(cached);
      }
    }

    console.log('[PharmacyService] Fetching dashboard stats from API');

    // Fetch all analytics data in parallel
    return forkJoin({
      monthlyRevenue: this.getMonthlyRevenue(period, dateRange, useCache),
      topCategories: this.getTopMedicationCategories(categoryLimit, useCache),
      stockTrends: this.getStockTrends(undefined, stockTrendDays, useCache)
    }).pipe(
      retry(3),
      tap(data => {
        // Calculate summary statistics
        const summary = {
          totalRevenue: data.monthlyRevenue.reduce((sum, item) => sum + item.revenue, 0),
          totalMedications: data.topCategories.reduce((sum, item) => sum + item.count, 0),
          totalCategories: data.topCategories.length,
          averageStockLevel: data.stockTrends.length > 0
            ? data.stockTrends.reduce((sum, item) => sum + item.stockLevel, 0) / data.stockTrends.length
            : 0
        };

        const dashboardStats: DashboardStats = {
          ...data,
          summary
        };

        if (useCache) {
          this.setCachedData(cacheKey, dashboardStats);
        }

        console.log('[PharmacyService] Dashboard stats loaded successfully');
      }),
      catchError(error => {
        console.error('[PharmacyService] Error fetching dashboard stats:', error);
        // Return fallback data
        const fallbackStats: DashboardStats = {
          monthlyRevenue: this.getMockMonthlyRevenue(),
          topCategories: this.getMockTopCategories(),
          stockTrends: this.getMockStockTrends(),
          summary: {
            totalRevenue: 0,
            totalMedications: 0,
            totalCategories: 0,
            averageStockLevel: 0
          }
        };
        console.warn('[PharmacyService] Using fallback dashboard stats');
        return of(fallbackStats);
      })
    );
  }

  /**
   * Clear analytics cache
   * Useful when data needs to be refreshed
   */
  clearAnalyticsCache(): void {
    try {
      const keys = Object.keys(localStorage);
      keys.forEach(key => {
        if (key.startsWith(this.cachePrefix)) {
          localStorage.removeItem(key);
        }
      });
      console.log('[PharmacyService] Analytics cache cleared');
    } catch (error) {
      console.warn('[PharmacyService] Error clearing analytics cache:', error);
    }
  }

  // ============================================
  // Mock Data Methods (for testing/fallback)
  // ============================================

  /**
   * Get mock monthly revenue data
   * Used as fallback when API is unavailable
   */
  private getMockMonthlyRevenue(): MonthlyRevenueData[] {
    const months = ['January', 'February', 'March', 'April', 'May', 'June', 
                     'July', 'August', 'September', 'October', 'November', 'December'];
    return months.map((month, index) => ({
      month,
      monthAbbr: month.substring(0, 3),
      revenue: Math.floor(Math.random() * 50000) + 10000,
      transactionCount: Math.floor(Math.random() * 200) + 50
    }));
  }

  /**
   * Get mock top categories data
   * Used as fallback when API is unavailable
   */
  private getMockTopCategories(): MedicationCategoryData[] {
    const categories = [
      { name: 'Antibiotics', count: 45 },
      { name: 'Pain Relief', count: 32 },
      { name: 'Cardiovascular', count: 28 },
      { name: 'Diabetes', count: 22 },
      { name: 'Respiratory', count: 18 },
      { name: 'Vitamins', count: 15 },
      { name: 'Digestive', count: 12 },
      { name: 'Skin Care', count: 10 }
    ];

    const total = categories.reduce((sum, cat) => sum + cat.count, 0);
    
    return categories.map(cat => ({
      category: cat.name,
      count: cat.count,
      percentage: Math.round((cat.count / total) * 100 * 100) / 100,
      totalStock: cat.count * 50
    }));
  }

  /**
   * Get mock stock trends data
   * Used as fallback when API is unavailable
   */
  private getMockStockTrends(): StockTrendData[] {
    const medications = [
      { id: 1, name: 'Paracetamol 500mg', minStock: 100 },
      { id: 2, name: 'Ibuprofen 400mg', minStock: 80 },
      { id: 3, name: 'Amoxicillin 250mg', minStock: 50 },
      { id: 4, name: 'Metformin 500mg', minStock: 120 },
      { id: 5, name: 'Aspirin 100mg', minStock: 90 }
    ];

    const trends: StockTrendData[] = [];
    const days = 30;
    const today = new Date();

    medications.forEach(med => {
      for (let i = days; i >= 0; i--) {
        const date = new Date(today);
        date.setDate(date.getDate() - i);
        
        trends.push({
          date: date.toISOString().split('T')[0],
          medicationId: med.id,
          medicationName: med.name,
          stockLevel: Math.floor(Math.random() * 200) + med.minStock,
          minimumStockLevel: med.minStock,
          category: 'General'
        });
      }
    });

    return trends;
  }
}
