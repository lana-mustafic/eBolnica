import { Component, inject } from '@angular/core';
import { DoctorService } from '../../../shared/services/doctor/doctor.service';
import { DoctorDto } from '../../../models/doctor.dto';
import { RouterModule } from '@angular/router'; 
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { DoctorProfileEditComponent } from './doctor-profile-edit/doctor-profile-edit.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-doctor-profile',
  imports: [RouterModule, MatDialogModule,CommonModule],
  standalone:true,
  templateUrl: './doctor-profile.component.html',
  styleUrl: './doctor-profile.component.css'
})
export class DoctorProfileComponent {

  private doctorService = inject(DoctorService);
  private dialog = inject(MatDialog);

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

  openEditForm(){
    const dialogRef = this.dialog.open(DoctorProfileEditComponent,{
      width:'800px',
      data: this.doctor 
    }); 

    dialogRef.afterClosed().subscribe ((updated) =>{
      if(updated){
        this.doctorService.getDoctorData().subscribe({
          next:(data)=>{
            this.doctor = data;
          },
          error:(err)=>{
            console.error('Error loading doctor data', err);
          }
        })
      }
    })
  }
}

