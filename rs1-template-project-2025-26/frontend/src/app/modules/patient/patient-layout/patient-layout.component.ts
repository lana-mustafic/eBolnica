import { Component, inject } from '@angular/core';

import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';



@Component({

  selector: 'app-patient-layout',

  standalone: false,

  templateUrl: './patient-layout.component.html',

  styleUrl: './patient-layout.component.scss',

})

export class PatientLayoutComponent {

  auth = inject(AuthFacadeService);

}


