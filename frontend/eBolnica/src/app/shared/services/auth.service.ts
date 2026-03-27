import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

interface JwtTokenPayload {
  role: string;
  exp: number;
  firstName:string;
  lastName:string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private router = inject(Router);

  constructor(private http:HttpClient) {}

  baseUrl = environment.apiUrl;

  createPatient(formData:any){
    return this.http.post(this.baseUrl+'/accounts/patient-registration',formData);
  }
  createDoctor(formData:any){
    return this.http.post(this.baseUrl+'/accounts/doctor-registration',formData);
  }

  login(email:string,password:string):Observable<any>{
    return this.http.post(this.baseUrl+'/accounts/user-login',{email,password})
  }

  getToken():string|null{
    return localStorage.getItem('jwtToken');
  }

  getUserType():string|null{
    const token = this.getToken();
    if(!token)
      return null;

    try{
        const decoded = jwtDecode<JwtTokenPayload>(token);
        return decoded.role;
    }
    catch{
      return null;
    }
  }

  logout():void{
    localStorage.removeItem('jwtToken');
    this.router.navigate(['/']);
  }

  isLoggedIn():boolean{
    return !!this.getToken();
  }

  roleCheck(userRole: string): boolean {
  const expectedRole = this.getUserType();
  return expectedRole === userRole;
  }

  userLoggedInfo():string|null{
    const token = this.getToken();
    if(!token){
      return null;
    }
    const decoded = jwtDecode<JwtTokenPayload>(token);
     if (decoded.firstName && decoded.lastName) {
    return `${decoded.firstName} ${decoded.lastName}`;
  }
  return null;
  }

}
