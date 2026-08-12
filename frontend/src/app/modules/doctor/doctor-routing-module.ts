import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DoctorLayoutComponent } from './doctor-layout/doctor-layout.component';
import { DoctorDashboardComponent } from './dashboard/doctor-dashboard.component';
import { DoctorProfileComponent } from './profile/doctor-profile.component';
import { DoctorPatientsComponent } from './patients/doctor-patients.component';
import { MedicalRecordComponent } from './medical-record/medical-record.component';
import { DoctorPrescriptionsComponent } from './prescriptions/doctor-prescriptions.component';
import { DoctorPrescriptionFormComponent } from './prescriptions/doctor-prescription-form.component';
import { DoctorPrescriptionDetailComponent } from './prescriptions/doctor-prescription-detail.component';

const routes: Routes = [
  {
    path: '',
    component: DoctorLayoutComponent,
    children: [
      { path: 'dashboard', component: DoctorDashboardComponent },
      { path: 'profile', component: DoctorProfileComponent },
      { path: 'patients', component: DoctorPatientsComponent },
      { path: 'medical-record/:patientId', component: MedicalRecordComponent },
      { path: 'prescriptions', component: DoctorPrescriptionsComponent },
      { path: 'prescriptions/new', component: DoctorPrescriptionFormComponent },
      { path: 'prescriptions/:id', component: DoctorPrescriptionDetailComponent },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class DoctorRoutingModule {}
