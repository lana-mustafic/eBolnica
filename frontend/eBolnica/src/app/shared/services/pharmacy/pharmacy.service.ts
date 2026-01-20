import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MedicationDto } from '../../../models/medication.dto';
import { MedicationCreateDto } from '../../../models/medication-create.dto';
import { PharmacistDataDto } from '../../../models/pharmacist-data.dto';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { PrescriptionCreateDto } from '../../../models/prescription-create.dto';
import { PrescriptionDispenseDto } from '../../../models/prescription-dispense.dto';
import { PagedResponse } from '../../../models/paged-response.dto';

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
    let params = new HttpParams()
      .set('page', page.toString())
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
}
