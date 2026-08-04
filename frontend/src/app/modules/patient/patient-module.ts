import { NgModule } from '@angular/core';

import { PatientRoutingModule } from './patient-routing-module';

import { PatientLayoutComponent } from './patient-layout/patient-layout.component';

import { PatientDashboardComponent } from './dashboard/patient-dashboard.component';

import { SharedModule } from '../shared/shared-module';



@NgModule({

  declarations: [PatientLayoutComponent, PatientDashboardComponent],

  imports: [PatientRoutingModule, SharedModule],

})

export class PatientModule {}


