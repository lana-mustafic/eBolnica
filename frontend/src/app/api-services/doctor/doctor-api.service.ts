import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
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
}
