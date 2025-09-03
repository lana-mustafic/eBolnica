import { Component, inject } from '@angular/core';
import { DoctorService } from '../../../shared/services/doctor/doctor.service';
import { DoctorAssignedPatientDto } from '../../../models/doctor-patients.dto';
import { AuthService } from '../../../shared/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-doctor-patients',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './doctor-patients.component.html',
  styleUrl: './doctor-patients.component.css'
})
export class DoctorPatientsComponent {

  doctorService = inject(DoctorService);
  authService = inject(AuthService);
  assignedPatients: DoctorAssignedPatientDto[] = [];


  ngOnInit(){
    this.doctorService.getAssignedPatients().subscribe({
      next: (data) => {
        this.assignedPatients = data;
    },
      error: (err) =>{
          console.error('Error loading doctor data', err);
        }

  })}
}
