import { CanActivateFn } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { inject } from '@angular/core';

export const noauthGuard: CanActivateFn = (route, state) => {

  const router = inject(Router);
  const authService = inject(AuthService);

  if(authService.isLoggedIn()){
  
    const role = authService.getUserType();

    let redirectPath='/';
    if(role==='Admin') redirectPath = '/admin-dashboard';
    else if(role==='Doctor') redirectPath='/doctor-dashboard';
    else if(role==='Patient') redirectPath='/';
    
    return router.createUrlTree([redirectPath]);
  }
  return true;
};
