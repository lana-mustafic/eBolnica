import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../shared/services/auth.service';

@Component({
  selector: 'app-pharmacy-shell',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './pharmacy-shell.component.html',
  styleUrl: './pharmacy-shell.component.css'
})
export class PharmacyShellComponent {
  authService = inject(AuthService);

  @Input() showNav = true;
}
