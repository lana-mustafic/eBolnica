import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateUserCommand,
  ListUsersRequest,
  ListUsersResponse,
  MessageResponse,
  UpdateRegistrationStatusCommand,
  UpdateUserCommand,
} from './admin-api.models';

@Injectable({
  providedIn: 'root',
})
export class AdminApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/admin`;
  private http = inject(HttpClient);

  listUsers(request: ListUsersRequest = {}): Observable<ListUsersResponse> {
    let params = new HttpParams()
      .set('page', String(request.page ?? 1))
      .set('pageSize', String(request.pageSize ?? 10))
      .set('sortBy', request.sortBy ?? 'firstName')
      .set('sortDirection', request.sortDirection ?? 'asc');

    if (request.userType) {
      params = params.set('userType', request.userType);
    }

    return this.http.get<ListUsersResponse>(`${this.baseUrl}/list-users`, { params });
  }

  updateDoctorRegistrationStatus(
    appUserId: number,
    body: UpdateRegistrationStatusCommand
  ): Observable<MessageResponse> {
    return this.http.put<MessageResponse>(
      `${this.baseUrl}/update-registration-status/${appUserId}`,
      body
    );
  }

  updatePatientRegistrationStatus(
    appUserId: number,
    body: UpdateRegistrationStatusCommand
  ): Observable<MessageResponse> {
    return this.http.put<MessageResponse>(
      `${this.baseUrl}/update-patient-registration-status/${appUserId}`,
      body
    );
  }

  createUser(body: CreateUserCommand): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.baseUrl}/create-user`, body);
  }

  updateUser(appUserId: number, body: UpdateUserCommand): Observable<MessageResponse> {
    return this.http.put<MessageResponse>(`${this.baseUrl}/update-user/${appUserId}`, body);
  }

  deleteUser(appUserId: number): Observable<MessageResponse> {
    return this.http.delete<MessageResponse>(`${this.baseUrl}/delete-user/${appUserId}`);
  }
}
