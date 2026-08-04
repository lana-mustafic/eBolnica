import { NgModule } from '@angular/core';

import { RouterModule, Routes } from '@angular/router';

import { PatientLayoutComponent } from './patient-layout/patient-layout.component';

import { PatientDashboardComponent } from './dashboard/patient-dashboard.component';



const routes: Routes = [

  {

    path: '',

    component: PatientLayoutComponent,

    children: [

      { path: 'dashboard', component: PatientDashboardComponent },

      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

    ],

  },

];



@NgModule({

  imports: [RouterModule.forChild(routes)],

  exports: [RouterModule],

})

export class PatientRoutingModule {}


