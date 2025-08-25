import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { RegistrationComponent } from './patient/patient-registration/patient-registration.component';
import { DoctorRegistrationComponent } from './doctor/doctor-registration/doctor-registration.component';
import { LoginComponent } from './login/login.component';
import { AdminDashboardComponent } from './admin/admin-dashboard.component';
import { authGuard } from './shared/guards/auth.guard';

export const routes: Routes = [
    {path: '', component:HomeComponent, pathMatch: 'full', title:'Home'},
    {path: 'register-patient', component: RegistrationComponent, title:'Patient registration'},
    {path: 'register-doctor', component:DoctorRegistrationComponent, title:'Doctor registration'},
    {path: 'user-login', component:LoginComponent, title:'LogIn'},
    {path: 'admin-dashboard', component:AdminDashboardComponent, canActivate:[authGuard ], title:'Admin Dashboard'},
    { path: '', redirectTo: '/user-login', pathMatch: 'full' }
];
