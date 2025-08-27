import { Component, inject } from '@angular/core';
import { DoctorService } from '../../../shared/services/doctor/doctor.service';
import { DoctorDto } from '../../../models/doctor.dto';

@Component({
  selector: 'app-doctor-profile',
  imports: [],
  standalone:true,
  templateUrl: './doctor-profile.component.html',
  styleUrl: './doctor-profile.component.css'
})
export class DoctorProfileComponent {

  private doctorService = inject(DoctorService);

  doctor?: DoctorDto;

  ngOnInit():void{
      this.doctorService.getDoctorData().subscribe({
        next: (data) =>{
          this.doctor = data;
        }, 
        error: (err) =>{
          console.error('Error loading doctor data', err);
        }
      })
  }
}
