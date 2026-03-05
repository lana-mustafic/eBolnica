import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, Form, FormBuilder, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { AuthService } from '../../shared/services/auth.service';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-doctor-registration',
  standalone: true,
  imports: [ReactiveFormsModule,CommonModule,RouterModule],
  templateUrl: './doctor-registration.component.html',
  styleUrl: './doctor-registration.component.css'
})
export class DoctorRegistrationComponent {
  form: FormGroup;
  submitSuccess:string | null=null
  errorMessage:string | null=null

  passwordMatchValidator:ValidatorFn = (control:AbstractControl):null =>{
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if(password && confirmPassword && password.value!=confirmPassword.value)
      confirmPassword?.setErrors({passwordMismatch:true})
    else
      confirmPassword?.setErrors(null);

    return null;
  }

  constructor(private formBuilder:FormBuilder, private authService:AuthService){
    this.form = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.pattern(/^[A-Z][a-zA-Z]*$/), Validators.minLength(3)]],
      lastName: ['', [Validators.required, Validators.pattern(/^[A-Z][a-zA-Z]*$/), Validators.minLength(3)]],
      licenseNumber:['', Validators.required],
      email:['', [Validators.required, Validators.email]],
      password:['', [Validators.required,
                      Validators.minLength(6),
                      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$/)]],
      confirmPassword:[''],
      dateOfBirth: ['', Validators.required],
      gender: ['', Validators.required]
    },{validators:this.passwordMatchValidator});
  }

    hidePassword: boolean = true;

    togglePasswordVisibility() {
    this.hidePassword = !this.hidePassword;
  }

  onSubmit(){
    this.submitSuccess=null;
    if(this.form.valid){
      this.errorMessage=null;
      this.authService.createDoctor(this.form.value).subscribe({
        next:(response)=>{
          console.log('Success', response);
          this.submitSuccess="Registration successful (Approval pending)";
          this.form.reset();
        },
        error:(err)=>{
          console.error('Error', err);
          if(err.status===400){
            this.errorMessage='E-mail already in use';
          }
        }
      });
    }
  }
}
