import { Component, inject } from '@angular/core';
import { DoctorService } from '../../../shared/services/doctor/doctor.service';
import { DoctorAssignedPatientDto } from '../../../models/doctor-patients.dto';
import { AuthService } from '../../../shared/services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-doctor-patients',
  imports: [CommonModule, FormsModule],
  standalone: true,
  templateUrl: './doctor-patients.component.html',
  styleUrl: './doctor-patients.component.css'
})
export class DoctorPatientsComponent {

  doctorService = inject(DoctorService);
  authService = inject(AuthService);
  assignedPatients: DoctorAssignedPatientDto[] = [];

   filters = {
    firstName: '',
    lastName: '',
    gender: '',
    bloodType: '',
    birthYear: ''
  };  

  ngOnInit(){
    this.doctorService.getAssignedPatients().subscribe({
      next: (data) => {
        this.assignedPatients = data;
    },
      error: (err) =>{
          console.error('Error loading doctor data', err);
        }

  })}

  filteredPatients() {
  return this.assignedPatients.filter(p => {
    let year: string | null = null;
    if (p.dateOfBirth) {
      const d = new Date(p.dateOfBirth);
      if (!isNaN(d.getTime())) year = d.getFullYear().toString();
    }

    const yearMatch = !this.filters.birthYear || (year && year === this.filters.birthYear.toString());

    return (!this.filters.firstName || p.firstName.toLowerCase().includes(this.filters.firstName.toLowerCase()))
      && (!this.filters.lastName || p.lastName.toLowerCase().includes(this.filters.lastName.toLowerCase()))
      && (!this.filters.gender || p.gender === this.filters.gender)
      && (!this.filters.bloodType || p.bloodType === this.filters.bloodType)
      && yearMatch;
  });
}

}

