import { inject, Injectable } from '@angular/core';
import { DoctorDto } from '../../../models/doctor.dto';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { updateDoctorDto } from '../../../models/update-doctor.dto';
import { DoctorDataDto } from '../../../models/doctor-data.dto';
import { DoctorAssignedPatientDto } from '../../../models/doctor-patients.dto';
import { UpdatePatientDto } from '../../../models/update-patient.dto';
import { DoctorListDto } from '../../../models/doctor-list.dto';
import { DashboardStats } from '../../../models/analytics.dto';
import { environment } from '../../../../environments/environment';
import { PatientFilterParams } from '../../../models/patient-filters.dto';
import { PagedResponse } from '../../../models/paged-response.dto';

@Injectable({
  providedIn: 'root'
})
export class DoctorService {

  private apiUrl = environment.apiUrl + '/doctor';

  private http = inject(HttpClient);

  getDoctorData(): Observable<DoctorDto>{
    return this.http.get<DoctorDataDto>(this.apiUrl+'/doctor-data');
  }

  editDoctorData(updatedDoctor: updateDoctorDto):Observable<updateDoctorDto>{
    return this.http.put<updateDoctorDto>(this.apiUrl+'/edit-doctor',updatedDoctor);
  }

  getAssignedPatients(filters: PatientFilterParams = {}): Observable<PagedResponse<DoctorAssignedPatientDto>>{
    let params = new HttpParams();

    if(filters.firstName) params = params.set('firstName', filters.firstName);
    if (filters.lastName)   params = params.set('lastName', filters.lastName);
    if (filters.gender)     params = params.set('gender', filters.gender);
    if (filters.bloodType)  params = params.set('bloodType', filters.bloodType);
    if (filters.birthYear)  params = params.set('birthYear', filters.birthYear.toString());

    params = params.set('page', (filters.page ?? 1).toString());
    params = params.set('pageSize', (filters.pageSize ?? 10).toString());

    return this.http.get<PagedResponse<DoctorAssignedPatientDto>>(this.apiUrl+'/list-patients', { params });
  }

  updatePatient(patientId: number, patient: UpdatePatientDto): Observable<DoctorAssignedPatientDto> {
    return this.http.put<DoctorAssignedPatientDto>(this.apiUrl+`/update-patient/${patientId}`, patient);
  }
  
  getAllDoctors(): Observable<DoctorListDto[]>{
    return this.http.get<DoctorListDto[]>(this.apiUrl+'/GetAllDoctors');
  }

  getStats(): Observable<DashboardStats>{
    return this.http.get<DashboardStats>(this.apiUrl+'/doctor-stats');
  }

}
