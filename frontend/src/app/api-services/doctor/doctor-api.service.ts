import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreatePrescriptionRequest,
  ListPrescriptionsRequest,
  ListPrescriptionsResponse,
  MedicationAutocompleteSuggestion,
  PrescriptionDto,
} from '../pharmacy/pharmacy-api.models';
import {
  DoctorProfileDto,
  DoctorStatsDto,
  ListDoctorPatientsRequest,
  ListDoctorPatientsResponse,
  UpdateDoctorProfileCommand,
} from './doctor-api.models';

@Injectable({
  providedIn: 'root',
})
export class DoctorApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/doctor`;
  private http = inject(HttpClient);

  getProfile(): Observable<DoctorProfileDto> {
    return this.http.get<DoctorProfileDto>(`${this.baseUrl}/doctor-data`);
  }

  updateProfile(body: UpdateDoctorProfileCommand): Observable<UpdateDoctorProfileCommand> {
    return this.http.put<UpdateDoctorProfileCommand>(`${this.baseUrl}/edit-doctor`, body);
  }

  listPatients(request: ListDoctorPatientsRequest = {}): Observable<ListDoctorPatientsResponse> {
    let params = new HttpParams()
      .set('page', String(request.page ?? 1))
      .set('pageSize', String(request.pageSize ?? 10));

    if (request.firstName) params = params.set('firstName', request.firstName);
    if (request.lastName) params = params.set('lastName', request.lastName);
    if (request.gender) params = params.set('gender', request.gender);
    if (request.bloodType) params = params.set('bloodType', request.bloodType);
    if (request.birthYear) params = params.set('birthYear', String(request.birthYear));

    return this.http.get<ListDoctorPatientsResponse>(`${this.baseUrl}/list-patients`, { params });
  }

  getStats(): Observable<DoctorStatsDto> {
    return this.http.get<DoctorStatsDto>(`${this.baseUrl}/doctor-stats`);
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

  getPrescription(id: number): Observable<PrescriptionDto> {
    return this.http.get<PrescriptionDto>(`${this.baseUrl}/prescriptions/${id}`);
  }

  createPrescription(body: CreatePrescriptionRequest): Observable<PrescriptionDto> {
    return this.http.post<PrescriptionDto>(`${this.baseUrl}/prescriptions`, body);
  }

  getMedicationAutocomplete(query: string, limit = 10): Observable<MedicationAutocompleteSuggestion[]> {
    const params = new HttpParams().set('q', query).set('limit', String(limit));
    return this.http.get<MedicationAutocompleteSuggestion[]>(`${this.baseUrl}/medications/autocomplete`, {
      params,
    });
  }
}
