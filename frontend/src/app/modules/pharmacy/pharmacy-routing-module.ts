import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
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

const routes: Routes = [
  {
    path: '',
    component: PharmacyLayoutComponent,
    children: [
      { path: 'dashboard', component: PharmacyDashboardComponent },
      { path: 'medications', component: PharmacyMedicationsComponent },
      { path: 'medications/wizard', component: MedicationWizardComponent },
      { path: 'medications/new', component: MedicationFormComponent },
      { path: 'medications/:id/edit', component: MedicationFormComponent },
      { path: 'medications/:id', component: MedicationDetailComponent },
      { path: 'inventory', component: PharmacyInventoryComponent },
      { path: 'prescriptions', component: PharmacyPrescriptionsComponent },
      { path: 'prescriptions/new', component: PrescriptionFormComponent },
      { path: 'prescriptions/:id', component: PrescriptionDetailComponent },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PharmacyRoutingModule {}
