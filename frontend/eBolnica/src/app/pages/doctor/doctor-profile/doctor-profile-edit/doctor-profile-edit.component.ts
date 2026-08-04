import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DoctorService } from '../../../../shared/services/doctor/doctor.service';
import { Validators } from '@angular/forms';


@Component({
  selector: 'app-doctor-profile-edit',
  imports: [ReactiveFormsModule, CommonModule],
  standalone:true,
  templateUrl: './doctor-profile-edit.component.html',
  styleUrl: './doctor-profile-edit.component.css'
})
export class DoctorProfileEditComponent {

  private fb = inject(FormBuilder);
  doctorData = inject(MAT_DIALOG_DATA);
  doctorService = inject(DoctorService);
  dialogRef = inject(MatDialogRef<DoctorProfileEditComponent>)
  form!:FormGroup;

  ngOnInit(){
    this.form=this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(3), Validators.minLength(3)]],
      lastName: ['', [Validators.required, Validators.minLength(3), Validators.minLength(3)]],
      phoneNumber: [''],
      specialization: ['', [Validators.required, Validators.pattern(/^[A-Z][a-zA-Z]*$/), Validators.minLength(3)]],
      address: ['', Validators.required]
    });
    if(this.doctorData){
      this.form.patchValue(this.doctorData);
    }
  }

  onSubmit(){
    if(this.form.valid){
      this.doctorService.editDoctorData(this.form.value).subscribe({
        next: (res) =>{
          this.dialogRef.close(true);
        },
        error: (err) =>{
          console.error("Error updating doctor", err);
        }
      })
    }
  }

  cancel(){
    this.dialogRef.close(false);
  }
}
