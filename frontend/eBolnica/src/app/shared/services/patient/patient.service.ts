import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PatientDataDto } from '../../../models/patient-data.dto';

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private apiUrl = 'http://localhost:5004/api/patient';
  private http = inject(HttpClient);

  getPatientData(): Observable<PatientDataDto> {
    return this.http.get<PatientDataDto>(this.apiUrl + '/patient-data');
  }
}

