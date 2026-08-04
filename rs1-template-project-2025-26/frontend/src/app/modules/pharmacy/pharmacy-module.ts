import { NgModule } from '@angular/core';
import { PharmacyRoutingModule } from './pharmacy-routing-module';
import { PharmacyLayoutComponent } from './pharmacy-layout/pharmacy-layout.component';
import { PharmacyDashboardComponent } from './dashboard/pharmacy-dashboard.component';
import { PharmacyMedicationsComponent } from './medications/pharmacy-medications.component';
import { MedicationFormComponent } from './medications/medication-form/medication-form.component';
import { MedicationWizardComponent } from './medications/medication-wizard/medication-wizard.component';
import { PharmacyInventoryComponent } from './inventory/pharmacy-inventory.component';
import { PharmacyPrescriptionsComponent } from './prescriptions/pharmacy-prescriptions.component';
import { PrescriptionDetailComponent } from './prescriptions/prescription-detail/prescription-detail.component';
import { SharedModule } from '../shared/shared-module';

@NgModule({
  declarations: [
    PharmacyLayoutComponent,
    PharmacyDashboardComponent,
    PharmacyMedicationsComponent,
    MedicationFormComponent,
    MedicationWizardComponent,
    PharmacyInventoryComponent,
    PharmacyPrescriptionsComponent,
    PrescriptionDetailComponent,
  ],
  imports: [PharmacyRoutingModule, SharedModule],
})
export class PharmacyModule {}
