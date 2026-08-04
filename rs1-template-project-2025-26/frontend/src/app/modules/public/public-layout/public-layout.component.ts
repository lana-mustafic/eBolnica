import { Component, inject } from '@angular/core';
import { CurrentUserService } from '../../../core/services/auth/current-user.service';

@Component({
  selector: 'app-public-layout',
  standalone: false,
  templateUrl: './public-layout.component.html',
  styleUrl: './public-layout.component.scss',
})
export class PublicLayoutComponent {
  private currentUserService = inject(CurrentUserService);

  currentUser = this.currentUserService.currentUser;
  isAuthenticated = this.currentUserService.isAuthenticated;

  dashboardRoute(): string {
    return this.currentUserService.getDefaultRoute();
  }
}
