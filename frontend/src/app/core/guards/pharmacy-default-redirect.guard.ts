import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthFacadeService } from '../services/auth/auth-facade.service';

export const pharmacyDefaultRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthFacadeService);
  const router = inject(Router);

  return router.createUrlTree([
    auth.isPharmacist() ? '/pharmacy/dashboard' : '/pharmacy/medications',
  ]);
};
