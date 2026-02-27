import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req,next) =>{

  const router = inject(Router);
  const token = localStorage.getItem('jwtToken');

  if(token){
    req = req.clone({
      setHeaders: {Authorization: `Bearer ${token}`} 
  });
}
  return next(req).pipe(
    catchError((error) => {
      if(error.status === 401) {
        localStorage.removeItem('jwtToken');
        router.navigate(['/user-login']);
      }

      return throwError(() => error);
    })
  );
};
