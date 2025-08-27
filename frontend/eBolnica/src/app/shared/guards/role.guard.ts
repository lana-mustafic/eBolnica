import { CanActivateFn } from '@angular/router';
import { Router } from '@angular/router';
import { inject, Inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {

  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredRole = route.data['role']

  if(!authService.roleCheck(requiredRole)){
    return router.createUrlTree(['/user-login']);
  }

  return true;  
};
