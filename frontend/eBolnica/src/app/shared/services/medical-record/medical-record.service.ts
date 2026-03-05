import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { MedicalRecord } from '../../../models/medical-record.dto';
import { newMedicalReport } from '../../../models/medical-record.dto';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MedicalRecordService {

  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl + '/patient/medical-record';

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
