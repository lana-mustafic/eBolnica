import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { MedicalRecord } from '../../../models/medical-record.dto';
import { Observable } from 'rxjs';
import { newMedicalReport } from '../../../models/medical-record.dto';

@Injectable({
  providedIn: 'root'
})
export class MedicalRecordService {

  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5004/api/patient/medical-record';

  getMedicalRecord(id: number){
    return this.http.get<MedicalRecord>(`/api/patient/medical-record/${id}/medical-records`);
  }
  
  newMedicalReport(data: newMedicalReport)
  {
    return this.http.post<newMedicalReport>(this.apiUrl+'/new-medical-report', data);
  }

  generatePdf(medicalRecordId: number, dateFrom: string, dateTo: string) {
  return this.http.get(
    `${this.apiUrl}/pdf/${medicalRecordId}?dateFrom=${dateFrom}&dateTo=${dateTo}`,
    { responseType: 'blob' }
  );
  }
}
