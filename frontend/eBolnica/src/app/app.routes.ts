import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { RegistrationComponent } from './patient/patient-registration/patient-registration.component';
import { DoctorRegistrationComponent } from './doctor/doctor-registration/doctor-registration.component';
import { LoginComponent } from './login/login.component';
import { AdminDashboardComponent } from './admin/admin-dashboard.component';
import { authGuard } from './shared/guards/auth.guard';
import { DoctorDashboardComponent } from './pages/doctor/doctor-dashboard/doctor-dashboard.component';
import { roleGuard } from './shared/guards/role.guard';
import { noauthGuard } from './shared/guards/noauth.guard';
import { DoctorProfileComponent } from './pages/doctor/doctor-profile/doctor-profile.component';
import { DoctorProfileEditComponent } from './pages/doctor/doctor-profile/doctor-profile-edit/doctor-profile-edit.component';
import { DoctorPatientsComponent } from './pages/doctor/doctor-patients/doctor-patients.component';
import { PatientDashboardComponent } from './pages/patient/patient-dashboard/patient-dashboard.component';
import { MedicalRecordComponent } from './pages/doctor/doctor-patients/medical-record/medical-record.component';

export const routes: Routes = [
    {path: '', component:HomeComponent,canActivate:[noauthGuard],pathMatch: 'full', title:'Home'},
    {path: 'register-patient', component: RegistrationComponent,canActivate:[noauthGuard], title:'Patient registration'},
    {path: 'register-doctor', component:DoctorRegistrationComponent,canActivate:[noauthGuard], title:'Doctor registration'},
    {path: 'user-login', component:LoginComponent,canActivate:[noauthGuard], title:'LogIn'},
    {path: 'admin-dashboard', component:AdminDashboardComponent, canActivate:[authGuard, roleGuard], data:{role:'Admin'},title:'Admin Dashboard'},
    {path: 'doctor-dashboard', component:DoctorDashboardComponent, canActivate:[authGuard, roleGuard ], data: { role: 'Doctor'}, title:'Doctor Dashboard'},
    {path: 'doctor-profile', component:DoctorProfileComponent, canActivate:[authGuard, roleGuard ], data: { role: 'Doctor'}, title:'Doctor Profile'},
    {path: 'doctor-profile-edit', component:DoctorProfileEditComponent, canActivate:[authGuard, roleGuard], data: {role: 'Doctor'}, title:"Edit doctor profile"},
    {path: 'doctor-patients', component:DoctorPatientsComponent, canActivate:[authGuard, roleGuard], data: {role: 'Doctor'}, title:"List assigned patients"},
    {path: 'patient-dashboard', component:PatientDashboardComponent, canActivate:[authGuard, roleGuard], data: {role: 'Patient'}, title:'Patient Dashboard'}
    {path: 'medical-record/:patientId', component:MedicalRecordComponent, canActivate:[authGuard, roleGuard], data: {role: 'Doctor'}, title:"Open medical record"}
];
    