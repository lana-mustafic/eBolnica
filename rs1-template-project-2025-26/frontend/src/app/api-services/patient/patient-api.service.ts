import { HttpClient } from '@angular/common/http';

import { inject, Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

import { PatientProfileDto } from './patient-api.models';



@Injectable({

  providedIn: 'root',

})

export class PatientApiService {

  private readonly baseUrl = `${environment.apiUrl}/api/patient`;

  private http = inject(HttpClient);



  getProfile(): Observable<PatientProfileDto> {

    return this.http.get<PatientProfileDto>(`${this.baseUrl}/patient-data`);

  }

}


