import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateMedicalReportCommand,
  MedicalRecordDto,
} from './medical-record-api.models';

@Injectable({
  providedIn: 'root',
})
export class MedicalRecordApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/patient/medical-record`;
  private http = inject(HttpClient);

  getByPatientId(patientId: number): Observable<MedicalRecordDto> {
    return this.http.get<MedicalRecordDto>(`${this.baseUrl}/${patientId}/medical-records`);
  }

  createReport(body: CreateMedicalReportCommand): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.baseUrl}/new-medical-report`, body);
  }
}
