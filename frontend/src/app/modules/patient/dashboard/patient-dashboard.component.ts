import { Component, inject, OnInit } from '@angular/core';

import { PatientApiService } from '../../../api-services/patient/patient-api.service';

import { PatientProfileDto } from '../../../api-services/patient/patient-api.models';

import { ToasterService } from '../../../core/services/toaster.service';



@Component({

  selector: 'app-patient-dashboard',

  standalone: false,

  templateUrl: './patient-dashboard.component.html',

  styleUrl: './patient-dashboard.component.scss',

})

export class PatientDashboardComponent implements OnInit {

  private patientApi = inject(PatientApiService);

  private toaster = inject(ToasterService);



  profile: PatientProfileDto | null = null;

  isLoading = true;



  ngOnInit(): void {

    this.loadProfile();

  }



  loadProfile(): void {

    this.isLoading = true;

    this.patientApi.getProfile().subscribe({

      next: (data) => {

        this.profile = data;

        this.isLoading = false;

      },

      error: () => {

        this.isLoading = false;

        this.toaster.error('Greška pri učitavanju profila.');

      },

    });

  }



  get doctorName(): string {

    if (!this.profile?.doctorFirstName && !this.profile?.doctorLastName) {

      return 'Nije dodijeljen';

    }

    return `${this.profile.doctorFirstName ?? ''} ${this.profile.doctorLastName ?? ''}`.trim();

  }

}


