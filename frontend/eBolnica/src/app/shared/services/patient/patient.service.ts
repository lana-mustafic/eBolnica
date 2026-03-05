import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PatientDataDto } from '../../../models/patient-data.dto';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private apiUrl = environment.apiUrl + '/patient';
  private http = inject(HttpClient);

  getPatientData(): Observable<PatientDataDto> {
    return this.http.get<PatientDataDto>(this.apiUrl + '/patient-data');
  }
}

