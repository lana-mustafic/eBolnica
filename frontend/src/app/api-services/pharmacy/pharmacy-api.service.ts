import { HttpClient, HttpEvent, HttpParams, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InventoryResponse,
  ListMedicationsRequest,
  ListMedicationsResponse,
  MedicationAutocompleteSuggestion,
  MedicationDto,
  MedicationImageDto,
  MedicationImportResult,
  MedicationNameAvailabilityDto,
  MedicationUpsertCommand,
  DispensePrescriptionRequest,
  ListPrescriptionsRequest,
  ListPrescriptionsResponse,
  PrescriptionDto,
  DashboardStatsResponseDto,
  RevenueDataDto,
  CategoriesDataDto,
  StockTrendsDataDto,
} from './pharmacy-api.models';

@Injectable({
  providedIn: 'root',
})
export class PharmacyApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/pharmacy`;
  private http = inject(HttpClient);

  private buildMedicationParams(request: ListMedicationsRequest): HttpParams {
    let params = new HttpParams()
      .set('pageNumber', String(request.pageNumber ?? 1))
      .set('pageSize', String(request.pageSize ?? 10));

    if (request.search) params = params.set('search', request.search);
    if (request.category) params = params.set('category', request.category);
    if (request.isActive !== undefined) params = params.set('isActive', String(request.isActive));
    if (request.includeInactive) params = params.set('includeInactive', 'true');
    if (request.stockStatus) params = params.set('stockStatus', request.stockStatus);
    if (request.requiresPrescription !== undefined) {
      params = params.set('requiresPrescription', String(request.requiresPrescription));
    }
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortOrder) params = params.set('sortOrder', request.sortOrder);
    return params;
  }

  listMedications(request: ListMedicationsRequest = {}): Observable<ListMedicationsResponse> {
    return this.http.get<ListMedicationsResponse>(`${this.baseUrl}/medications`, {
      params: this.buildMedicationParams(request),
    });
  }

  getAutocomplete(query: string, limit = 10): Observable<MedicationAutocompleteSuggestion[]> {
    const params = new HttpParams().set('q', query).set('limit', String(limit));
    return this.http.get<MedicationAutocompleteSuggestion[]>(`${this.baseUrl}/medications/autocomplete`, {
      params,
    });
  }

  exportMedicationsCsv(request: ListMedicationsRequest = {}): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/medications/export/csv`, {
      params: this.buildMedicationParams({ ...request, pageNumber: 1, pageSize: 100 }),
      responseType: 'blob',
      observe: 'response',
    });
  }

  downloadImportTemplate(): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/medications/import/template`, {
      responseType: 'blob',
      observe: 'response',
    });
  }

  importMedicationsCsv(file: File): Observable<MedicationImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<MedicationImportResult>(`${this.baseUrl}/medications/import/csv`, formData);
  }

  getInventory(request: ListMedicationsRequest = {}): Observable<InventoryResponse> {
    return this.http.get<InventoryResponse>(`${this.baseUrl}/inventory`, {
      params: this.buildMedicationParams(request),
    });
  }

  getMedicationById(id: number): Observable<MedicationDto> {
    return this.http.get<MedicationDto>(`${this.baseUrl}/medications/${id}`);
  }

  checkName(name: string, excludeId?: number): Observable<MedicationNameAvailabilityDto> {
    let params = new HttpParams().set('name', name);
    if (excludeId != null) params = params.set('excludeId', String(excludeId));
    return this.http.get<MedicationNameAvailabilityDto>(`${this.baseUrl}/medications/check-name`, { params });
  }

  createMedication(body: MedicationUpsertCommand): Observable<MedicationDto> {
    return this.http.post<MedicationDto>(`${this.baseUrl}/medications`, body);
  }

  updateMedication(id: number, body: MedicationUpsertCommand): Observable<MedicationDto> {
    return this.http.put<MedicationDto>(`${this.baseUrl}/medications/${id}`, body);
  }

  deleteMedication(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/medications/${id}`);
  }

  listImages(medicationId: number): Observable<MedicationImageDto[]> {
    return this.http.get<MedicationImageDto[]>(`${this.baseUrl}/medications/${medicationId}/images`);
  }

  uploadImage(medicationId: number, file: File): Observable<HttpEvent<MedicationImageDto>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<MedicationImageDto>(
      `${this.baseUrl}/medications/${medicationId}/images`,
      formData,
      { reportProgress: true, observe: 'events' }
    );
  }

  deleteImage(medicationId: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/medications/${medicationId}/images/${imageId}`);
  }

  setPrimaryImage(medicationId: number, imageId: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/medications/${medicationId}/images/${imageId}/primary`, {});
  }

  imageFullUrl(relativeUrl: string): string {
    if (relativeUrl.startsWith('http')) {
      return relativeUrl;
    }
    const path = relativeUrl.startsWith('/') ? relativeUrl : `/${relativeUrl}`;
    return `${environment.apiUrl}${path}`;
  }

  getMedicationImageBlob(medicationId: number, imageId: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/medications/${medicationId}/images/${imageId}/file`, {
      responseType: 'blob',
    });
  }

  listPrescriptions(request: ListPrescriptionsRequest = {}): Observable<ListPrescriptionsResponse> {
    let params = new HttpParams()
      .set('pageNumber', String(request.pageNumber ?? 1))
      .set('pageSize', String(request.pageSize ?? 10));

    if (request.status) params = params.set('status', request.status);
    if (request.search) params = params.set('search', request.search);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortOrder) params = params.set('sortOrder', request.sortOrder);

    return this.http.get<ListPrescriptionsResponse>(`${this.baseUrl}/prescriptions`, { params });
  }

  getPrescriptionById(id: number): Observable<PrescriptionDto> {
    return this.http.get<PrescriptionDto>(`${this.baseUrl}/prescriptions/${id}`);
  }

  dispensePrescription(id: number, body: DispensePrescriptionRequest = {}): Observable<PrescriptionDto> {
    return this.http.post<PrescriptionDto>(`${this.baseUrl}/prescriptions/${id}/dispense`, body);
  }

  getDashboardStats(): Observable<DashboardStatsResponseDto> {
    return this.http.get<DashboardStatsResponseDto>(`${this.baseUrl}/analytics/dashboard-stats`);
  }

  getMonthlyRevenue(months = 12): Observable<RevenueDataDto> {
    return this.http.get<RevenueDataDto>(`${this.baseUrl}/analytics/monthly-revenue`, {
      params: new HttpParams().set('months', String(months)),
    });
  }

  getTopCategories(limit = 8): Observable<CategoriesDataDto> {
    return this.http.get<CategoriesDataDto>(`${this.baseUrl}/analytics/top-categories`, {
      params: new HttpParams().set('limit', String(limit)),
    });
  }

  getStockTrends(): Observable<StockTrendsDataDto> {
    return this.http.get<StockTrendsDataDto>(`${this.baseUrl}/analytics/stock-trends`);
  }

  exportInventoryPdf(request: ListMedicationsRequest = {}): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.baseUrl}/reports/inventory/pdf`, {
      params: this.buildMedicationParams({ ...request, pageNumber: 1, pageSize: 100 }),
      responseType: 'blob',
      observe: 'response',
    });
  }

  exportPrescriptionsPdf(request: ListPrescriptionsRequest = {}): Observable<HttpResponse<Blob>> {
    let params = new HttpParams().set('pageNumber', '1').set('pageSize', '100');
    if (request.status) params = params.set('status', request.status);
    if (request.search) params = params.set('search', request.search);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortOrder) params = params.set('sortOrder', request.sortOrder);
    return this.http.get(`${this.baseUrl}/reports/prescriptions/pdf`, {
      params,
      responseType: 'blob',
      observe: 'response',
    });
  }

  downloadBlobResponse(response: HttpResponse<Blob>, fallbackName: string): void {
    const blob = response.body;
    if (!blob) return;
    const disposition = response.headers.get('Content-Disposition');
    const match = disposition?.match(/filename="?([^";]+)"?/);
    const fileName = match?.[1] ?? fallbackName;
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  }
}
