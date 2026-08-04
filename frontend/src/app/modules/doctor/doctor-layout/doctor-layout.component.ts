import { Component, inject } from '@angular/core';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

@Component({
  selector: 'app-doctor-layout',
  standalone: false,
  templateUrl: './doctor-layout.component.html',
  styleUrl: './doctor-layout.component.scss',
})
export class DoctorLayoutComponent {
  auth = inject(AuthFacadeService);
}
