import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor(private http:HttpClient) {}

  baseUrl = 'http://localhost:5004'

  createPatient(formData:any){
    return this.http.post(this.baseUrl+'/api/accounts/patient-registration',formData);
  }
  createDoctor(formData:any){
    return this.http.post(this.baseUrl+'/api/accounts/doctor-registration',formData);
  }
}
