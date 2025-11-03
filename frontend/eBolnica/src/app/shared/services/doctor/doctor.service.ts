import { inject, Injectable } from '@angular/core';
import { DoctorDto } from '../../../models/doctor.dto';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { updateDoctorDto } from '../../../models/update-doctor.dto';
import { DoctorDataDto } from '../../../models/doctor-data.dto';
import { DoctorAssignedPatientDto } from '../../../models/doctor-patients.dto';

@Injectable({
  providedIn: 'root'
})
export class DoctorService {

  private apiUrl = 'http://localhost:5004/api/doctor';

  private http = inject(HttpClient);

  getDoctorData(): Observable<DoctorDto>{
    return this.http.get<DoctorDataDto>(this.apiUrl+'/doctor-data');
  }

  editDoctorData(updatedDoctor: updateDoctorDto):Observable<updateDoctorDto>{
    return this.http.put<updateDoctorDto>(this.apiUrl+'/edit-doctor',updatedDoctor);
  }

  getAssignedPatients(){
    return this.http.get<DoctorAssignedPatientDto[]>(this.apiUrl+'/list-patients');
  }
}
