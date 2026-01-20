import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpErrorResponse, HttpResponse } from '@angular/common/http';
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
}
