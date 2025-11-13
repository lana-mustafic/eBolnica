import { inject, Injectable } from '@angular/core';
import { DoctorDto } from '../../../models/doctor.dto';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { updateDoctorDto } from '../../../models/update-doctor.dto';
import { DoctorDataDto } from '../../../models/doctor-data.dto';
import { DoctorAssignedPatientDto } from '../../../models/doctor-patients.dto';
import { CreatePatientDto } from '../../../models/create-patient.dto';
import { UpdatePatientDto } from '../../../models/update-patient.dto';
import { PatientSearchDto } from '../../../models/patient-search.dto';
import { AssignPatientDto } from '../../../models/assign-patient.dto';

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

  createPatient(patient: CreatePatientDto): Observable<DoctorAssignedPatientDto> {
    return this.http.post<DoctorAssignedPatientDto>(this.apiUrl+'/create-patient', patient);
  }

  updatePatient(patientId: number, patient: UpdatePatientDto): Observable<DoctorAssignedPatientDto> {
    return this.http.put<DoctorAssignedPatientDto>(this.apiUrl+`/update-patient/${patientId}`, patient);
  }

  deletePatient(patientId: number): Observable<any> {
    return this.http.delete(this.apiUrl+`/delete-patient/${patientId}`);
  }

  searchPatients(searchTerm?: string): Observable<PatientSearchDto[]> {
    let params = new HttpParams();
    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }
    return this.http.get<PatientSearchDto[]>(this.apiUrl+'/search-patients', { params });
  }

  assignPatient(assignData: AssignPatientDto): Observable<DoctorAssignedPatientDto> {
    return this.http.post<DoctorAssignedPatientDto>(this.apiUrl+'/assign-patient', assignData);
  }
}
