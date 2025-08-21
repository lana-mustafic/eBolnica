import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { RegistrationComponent } from './patient/patient-registration/patient-registration.component';
import { DoctorRegistrationComponent } from './doctor/doctor-registration/doctor-registration.component';
import { LoginComponent } from './login/login.component';

export const routes: Routes = [
    {path: '', component:HomeComponent, pathMatch: 'full', title:'Home'},
    {path: 'register-patient', component: RegistrationComponent, title:'Patient registration'},
    {path: 'register-doctor', component:DoctorRegistrationComponent, title:'Doctor registration'},
    {path: 'user-login', component:LoginComponent, title:'LogIn'}
];
