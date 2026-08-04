import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DoctorLayoutComponent } from './doctor-layout/doctor-layout.component';
import { DoctorDashboardComponent } from './dashboard/doctor-dashboard.component';
import { DoctorProfileComponent } from './profile/doctor-profile.component';
import { DoctorPatientsComponent } from './patients/doctor-patients.component';
import { MedicalRecordComponent } from './medical-record/medical-record.component';

const routes: Routes = [
  {
    path: '',
    component: DoctorLayoutComponent,
    children: [
      { path: 'dashboard', component: DoctorDashboardComponent },
      { path: 'profile', component: DoctorProfileComponent },
      { path: 'patients', component: DoctorPatientsComponent },
      { path: 'medical-record/:patientId', component: MedicalRecordComponent },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class DoctorRoutingModule {}
