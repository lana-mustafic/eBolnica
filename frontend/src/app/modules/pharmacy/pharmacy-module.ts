import { NgModule } from '@angular/core';
import { PharmacyRoutingModule } from './pharmacy-routing-module';
import { PharmacyLayoutComponent } from './pharmacy-layout/pharmacy-layout.component';
import { PharmacyDashboardComponent } from './dashboard/pharmacy-dashboard.component';
import { PharmacyRevenueChartComponent } from './dashboard/charts/pharmacy-revenue-chart.component';
import { PharmacyCategoriesChartComponent } from './dashboard/charts/pharmacy-categories-chart.component';
import { PharmacyStockChartComponent } from './dashboard/charts/pharmacy-stock-chart.component';
import { PharmacyAnalyticsChartsComponent } from './dashboard/charts/pharmacy-analytics-charts.component';
import { PharmacyMedicationsComponent } from './medications/pharmacy-medications.component';
import { MedicationFormComponent } from './medications/medication-form/medication-form.component';
import { MedicationDetailComponent } from './medications/medication-detail/medication-detail.component';
import { MedicationWizardComponent } from './medications/medication-wizard/medication-wizard.component';
import { MedicationImageLightboxComponent } from './medications/medication-image-lightbox/medication-image-lightbox.component';
import { PharmacyInventoryComponent } from './inventory/pharmacy-inventory.component';
import { PharmacyPrescriptionsComponent } from './prescriptions/pharmacy-prescriptions.component';
import { PrescriptionDetailComponent } from './prescriptions/prescription-detail/prescription-detail.component';
import { PrescriptionFormComponent } from './prescriptions/prescription-form/prescription-form.component';
import { PharmacyIconComponent } from './shared/pharmacy-icon/pharmacy-icon.component';
import { PharmacyKpiCardComponent } from './shared/pharmacy-kpi-card/pharmacy-kpi-card.component';
import { SharedModule } from '../shared/shared-module';

@NgModule({
  declarations: [
    PharmacyLayoutComponent,
    PharmacyDashboardComponent,
    PharmacyRevenueChartComponent,
    PharmacyCategoriesChartComponent,
    PharmacyStockChartComponent,
    PharmacyAnalyticsChartsComponent,
    PharmacyMedicationsComponent,
    MedicationFormComponent,
    MedicationDetailComponent,
    MedicationWizardComponent,
    MedicationImageLightboxComponent,
    PharmacyInventoryComponent,
    PharmacyPrescriptionsComponent,
    PrescriptionDetailComponent,
    PrescriptionFormComponent,
    PharmacyIconComponent,
    PharmacyKpiCardComponent,
  ],
  imports: [PharmacyRoutingModule, SharedModule],
})
export class PharmacyModule {}
