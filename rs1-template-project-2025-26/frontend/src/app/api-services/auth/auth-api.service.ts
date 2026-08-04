import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  LoginCommand,
  LoginCommandDto,
  LogoutCommand,
  RefreshTokenCommand,
  RefreshTokenCommandDto,
  RegisterDoctorCommand,
  RegisterPatientCommand,
  RegisterResponseDto,
} from './auth-api.model';

@Injectable({
  providedIn: 'root',
})
export class AuthApiService {
  private readonly authUrl = `${environment.apiUrl}/api/auth`;
  private readonly accountsUrl = `${environment.apiUrl}/api/accounts`;
  private http = inject(HttpClient);

  login(payload: LoginCommand): Observable<LoginCommandDto> {
    return this.http.post<LoginCommandDto>(`${this.authUrl}/login`, payload);
  }

  refresh(payload: RefreshTokenCommand): Observable<RefreshTokenCommandDto> {
    return this.http.post<RefreshTokenCommandDto>(`${this.authUrl}/refresh`, payload);
  }

  logout(payload: LogoutCommand): Observable<void> {
    return this.http.post<void>(`${this.authUrl}/logout`, payload);
  }

  registerPatient(payload: RegisterPatientCommand): Observable<RegisterResponseDto> {
    return this.http.post<RegisterResponseDto>(`${this.accountsUrl}/patient-registration`, payload);
  }

  registerDoctor(payload: RegisterDoctorCommand): Observable<RegisterResponseDto> {
    return this.http.post<RegisterResponseDto>(`${this.accountsUrl}/doctor-registration`, payload);
  }
}
