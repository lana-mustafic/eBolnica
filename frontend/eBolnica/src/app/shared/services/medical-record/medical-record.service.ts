import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { MedicalRecordDto } from '../../../models/medical-record.dto';

@Injectable({
  providedIn: 'root'
})
export class MedicalRecordService {

  private http = inject(HttpClient);

  getMedicalRecord(id: number){
    return this.http.get<MedicalRecordDto>(`/api/patient/medical-record/${id}/medical-records`);
  }
  
}
