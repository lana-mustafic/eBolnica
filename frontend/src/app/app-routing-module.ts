import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import {myAuthData, myAuthGuard} from './core/guards/my-auth-guard';

const routes: Routes = [
  {
    path: 'admin',
    canActivate: [myAuthGuard],
    data: myAuthData({ requireAuth: true, requireAdmin: true }),
    loadChildren: () =>
      import('./modules/admin/admin-module').then(m => m.AdminModule)
  },
  {
    path: 'auth',
    loadChildren: () =>
      import('./modules/auth/auth-module').then(m => m.AuthModule)
  },
  {
    path: 'doctor',
    canActivate: [myAuthGuard],
    data: myAuthData({ requireAuth: true, requireDoctor: true }),
    loadChildren: () =>
      import('./modules/doctor/doctor-module').then(m => m.DoctorModule)
  },
  {
    path: 'patient',
    canActivate: [myAuthGuard],
    data: myAuthData({ requireAuth: true, requirePatient: true }),
    loadChildren: () =>
      import('./modules/patient/patient-module').then(m => m.PatientModule)
  },
  {
    path: 'pharmacy',
    canActivate: [myAuthGuard],
    data: myAuthData({ requireAuth: true, requirePharmacyStaff: true }),
    loadChildren: () =>
      import('./modules/pharmacy/pharmacy-module').then(m => m.PharmacyModule)
  },
  {
    path: '',
    loadChildren: () =>
      import('./modules/public/public-module').then(m => m.PublicModule)
  },
  // fallback 404
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
