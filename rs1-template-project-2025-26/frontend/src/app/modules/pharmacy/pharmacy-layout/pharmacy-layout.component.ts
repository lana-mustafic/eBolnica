import { Component, inject } from '@angular/core';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';

@Component({
  selector: 'app-pharmacy-layout',
  standalone: false,
  templateUrl: './pharmacy-layout.component.html',
  styleUrl: './pharmacy-layout.component.scss',
})
export class PharmacyLayoutComponent {
  auth = inject(AuthFacadeService);
}
