import { NgModule } from '@angular/core';
import { DoctorRoutingModule } from './doctor-routing-module';
import { DoctorLayoutComponent } from './doctor-layout/doctor-layout.component';
import { DoctorDashboardComponent } from './dashboard/doctor-dashboard.component';
import { DoctorProfileComponent } from './profile/doctor-profile.component';
import { DoctorProfileEditDialogComponent } from './profile/doctor-profile-edit-dialog/doctor-profile-edit-dialog.component';
import { DoctorPatientsComponent } from './patients/doctor-patients.component';
import { MedicalRecordComponent } from './medical-record/medical-record.component';
import { SharedModule } from '../shared/shared-module';

@NgModule({
  declarations: [
    DoctorLayoutComponent,
    DoctorDashboardComponent,
    DoctorProfileComponent,
    DoctorProfileEditDialogComponent,
    DoctorPatientsComponent,
    MedicalRecordComponent,
  ],
  imports: [DoctorRoutingModule, SharedModule],
})
export class DoctorModule {}
