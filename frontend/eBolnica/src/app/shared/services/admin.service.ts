import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AdminService {

  constructor(private http:HttpClient) {}

  baseUrl = 'http://localhost:5004/api/admin'

  getAllUsers(): Observable<any[]>{
    return this.http.get<any[]>(`${this.baseUrl}/list-users`)
  }

  updateRegistrationStatus(appUserId: string, status:string):Observable<any>{
    return this.http.put(`${this.baseUrl}/update-registration-status/${appUserId}`, {registrationStatus:status});
  }
}
