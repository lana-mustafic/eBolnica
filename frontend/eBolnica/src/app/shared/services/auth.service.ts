import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

interface JwtTokenPayload {
  UserType: string;
  exp: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor(private http:HttpClient) {}

  baseUrl = 'http://localhost:5004/api'

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
        console.log('Decoded JWT:', decoded);
        return decoded.UserType;
    }
    catch{
      return null;
    }
  }

  isLoggedIn():boolean{
    return !!this.getToken();
  }
}
