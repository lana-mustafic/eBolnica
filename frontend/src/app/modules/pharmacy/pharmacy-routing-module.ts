import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { myAuthData, myAuthGuard } from '../../core/guards/my-auth-guard';
import { pharmacyDefaultRedirectGuard } from '../../core/guards/pharmacy-default-redirect.guard';
import { PharmacyLayoutComponent } from './pharmacy-layout/pharmacy-layout.component';
import { PharmacyDashboardComponent } from './dashboard/pharmacy-dashboard.component';
import { PharmacyMedicationsComponent } from './medications/pharmacy-medications.component';
import { MedicationFormComponent } from './medications/medication-form/medication-form.component';
import { MedicationDetailComponent } from './medications/medication-detail/medication-detail.component';
import { MedicationWizardComponent } from './medications/medication-wizard/medication-wizard.component';
import { PharmacyInventoryComponent } from './inventory/pharmacy-inventory.component';
import { PharmacyPrescriptionsComponent } from './prescriptions/pharmacy-prescriptions.component';
import { PrescriptionDetailComponent } from './prescriptions/prescription-detail/prescription-detail.component';
import { PrescriptionFormComponent } from './prescriptions/prescription-form/prescription-form.component';

const pharmacyStaffOnly = {
  canActivate: [myAuthGuard],
  data: myAuthData({ requireAuth: true, requirePharmacyStaff: true }),
};

const pharmacistOnly = {
  canActivate: [myAuthGuard],
  data: myAuthData({ requireAuth: true, requirePharmacist: true }),
};

const routes: Routes = [
  {
    path: '',
    component: PharmacyLayoutComponent,
    children: [
      { path: 'dashboard', component: PharmacyDashboardComponent, ...pharmacistOnly },
      { path: 'medications', component: PharmacyMedicationsComponent, ...pharmacyStaffOnly },
      { path: 'medications/wizard', component: MedicationWizardComponent, ...pharmacyStaffOnly },
      { path: 'medications/new', component: MedicationFormComponent, ...pharmacyStaffOnly },
      { path: 'medications/:id/edit', component: MedicationFormComponent, ...pharmacyStaffOnly },
      { path: 'medications/:id', component: MedicationDetailComponent, ...pharmacyStaffOnly },
      { path: 'inventory', component: PharmacyInventoryComponent, ...pharmacyStaffOnly },
      { path: 'prescriptions', component: PharmacyPrescriptionsComponent, ...pharmacistOnly },
      { path: 'prescriptions/new', component: PrescriptionFormComponent, ...pharmacistOnly },
      { path: 'prescriptions/:id', component: PrescriptionDetailComponent, ...pharmacistOnly },
      { path: '', pathMatch: 'full', canActivate: [pharmacyDefaultRedirectGuard] },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PharmacyRoutingModule {}
