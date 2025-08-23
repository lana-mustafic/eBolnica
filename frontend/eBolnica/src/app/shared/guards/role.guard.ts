import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  
  const router = inject(Router);
  const authService = inject(AuthService);
  
  const userType = authService.getUserType();

  if(!userType){
    router.navigateByUrl('/login');
    return false;
  }

  const expectedRoles = route.data?.['roles'] as Array<string>;

  if(expectedRoles && expectedRoles.includes(userType)){
    return true;
  }else{
    router.navigateByUrl('/unauthorized');
    return false;
  }

};
