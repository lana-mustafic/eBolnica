import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../shared/services/auth.service';

@Component({
  selector: 'app-pharmacy-shell',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './pharmacy-shell.component.html',
  styleUrl: './pharmacy-shell.component.css',
})
export class PharmacyShellComponent {
  readonly authService = inject(AuthService);
}
