import { Injectable } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})

export class AuthInterceptorService {

  constructor() { }
}

export const authInterceptor: HttpInterceptorFn = (req,next) =>{
  const token = localStorage.getItem('jwtToken');
  if(token){
    req = req.clone({
      headers: req.headers.set('Authorization','Bearer ${token}')
  });
}
  return next(req);
}
