import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AdminService {

  constructor(private http:HttpClient) {}

  baseUrl = 'http://localhost:5004/api/admin'

  getAllUsers(page:number, pageSize:number, userType?: string, sortBy: string = 'firstName', sortDirection: string = 'asc'): Observable<any>{
    let params = new HttpParams()
          .set('page', page.toString())
          .set('pageSize', pageSize.toString())
          .set('sortBy', sortBy)
          .set('sortDirection', sortDirection);

    if(userType){ 
      params = params.set('userType', userType);
    }

    return this.http.get<any>(`${this.baseUrl}/list-users`, { params })
  }

  updateRegistrationStatus(appUserId: string, status:string):Observable<any>{
    return this.http.put(`${this.baseUrl}/update-registration-status/${appUserId}`, {registrationStatus:status});
  }

  createUser(data: any): Observable<any> {
  return this.http.post(`${this.baseUrl}/create-user`, data);
  }

  updateUser(appUserId: string, data: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/update-user/${appUserId}`, data);
  }

  deleteUser(appUserId: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/delete-user/${appUserId}`);
  }
}
