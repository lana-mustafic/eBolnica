import { Component, inject } from '@angular/core';
import { AuthService } from '../../../shared/services/auth.service';
import { FormsModule } from "@angular/forms";

@Component({
  selector: 'app-doctor-dashboard',
  imports: [FormsModule],
  standalone: true,
  templateUrl: './doctor-dashboard.component.html',
  styleUrl: './doctor-dashboard.component.css'
})
export class DoctorDashboardComponent {
  public authService = inject(AuthService);
}
