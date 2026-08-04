import { NgModule } from '@angular/core';

import { AuthRoutingModule } from './auth-routing-module';
import { AuthLayoutComponent } from './auth-layout/auth-layout.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { LogoutComponent } from './logout/logout.component';
import { PatientRegistrationComponent } from './patient-registration/patient-registration.component';
import { DoctorRegistrationComponent } from './doctor-registration/doctor-registration.component';
import { SharedModule } from '../shared/shared-module';

@NgModule({
  declarations: [
    AuthLayoutComponent,
    LoginComponent,
    RegisterComponent,
    ForgotPasswordComponent,
    LogoutComponent,
    PatientRegistrationComponent,
    DoctorRegistrationComponent,
  ],
  imports: [AuthRoutingModule, SharedModule],
})
export class AuthModule {}
