import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../../../shared/services/auth.service';
import { PatientService } from '../../../shared/services/patient/patient.service';
import { PatientDataDto } from '../../../models/patient-data.dto';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-patient-dashboard',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './patient-dashboard.component.html',
  styleUrl: './patient-dashboard.component.css'
})
export class PatientDashboardComponent implements OnInit {
  public authService = inject(AuthService);
  private patientService = inject(PatientService);
  
  patient?: PatientDataDto;

  ngOnInit(): void {
    this.patientService.getPatientData().subscribe({
      next: (data) => {
        this.patient = data;
      },
      error: (err) => {
        console.error('Error loading patient data', err);
      }
    });
  }
}

