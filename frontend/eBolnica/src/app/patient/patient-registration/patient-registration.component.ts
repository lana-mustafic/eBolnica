import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { AbstractControl, Form, FormBuilder, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { AuthService } from '../../shared/services/auth.service';
import { RouterModule } from '@angular/router';
import { DoctorService } from '../../shared/services/doctor/doctor.service';
import { DoctorListDto } from '../../models/doctor-list.dto';


@Component({
  selector: 'patient-registration',
  standalone: true,
  imports: [ReactiveFormsModule,CommonModule,RouterModule],
  templateUrl: './patient-registration.component.html',
  styleUrl: './patient-registration.component.css'
})
export class RegistrationComponent implements OnInit{
  form: FormGroup;
  isSubmitted:boolean = false;
  errorMessage:string | null = null;
  submitSuccess:string | null = null;
 


  private doctorService = inject(DoctorService);

   doctors: DoctorListDto[] =[];

  ngOnInit(): void{
    this.loadDoctors();
  }

  loadDoctors(): void{
    this.doctorService.getAllDoctors().subscribe({
      next: (data) => this.doctors = data,
      error: (err) => alert('Unable to load doctor list')
    });
  }

  passwordMatchValidator:ValidatorFn = (control:AbstractControl):null =>{
    const password = control.get('password')
    const confirmPassword = control.get('confirmPassword')

    if(password && confirmPassword && password.value!=confirmPassword.value)
      confirmPassword?.setErrors({passwordMismatch:true})
      else
      confirmPassword?.setErrors(null);

    return null;
  }
  

  constructor(private formBuilder: FormBuilder, private authService:AuthService) {
    this.form = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.pattern(/^[A-Z][a-zA-Z]*$/), Validators.minLength(3)]],
      lastName: ['', [Validators.required, Validators.pattern(/^[A-Z][a-zA-Z]*$/), Validators.minLength(3)]],
      email: ['', [Validators.required,Validators.email]],
      password: ['', [Validators.required,
                      Validators.minLength(6),
                      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$/)]],
      confirmPassword: [''],
      doctorId: [''], 
    }, {validators:this.passwordMatchValidator});
  }

  hidePassword: boolean = true;
  
  togglePasswordVisibility() {
  this.hidePassword = !this.hidePassword;
}

  onSubmit(){
  this.submitSuccess = null;
  if (this.form.valid) {
      this.errorMessage = null;
      this.authService.createPatient(this.form.value).subscribe({
        next: (response) => {
          console.log('Success:', response);
          this.submitSuccess= "Registration successful";
          this.form.reset();
        },
        error: (err) => {
          console.error('Registration failed:', err);
          if(err.status===400){
            this.errorMessage='E-mail already in use';
          }
        }
      });
}
}
}
