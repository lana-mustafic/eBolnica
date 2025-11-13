import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { AbstractControl, Form, FormBuilder, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { AuthService } from '../../shared/services/auth.service';
import { RouterModule } from '@angular/router';
import { I18nService, Language } from '../../shared/services/i18n.service';

@Component({
  selector: 'patient-registration',
  standalone: true,
  imports: [ReactiveFormsModule,CommonModule,RouterModule],
  templateUrl: './patient-registration.component.html',
  styleUrl: './patient-registration.component.css'
})
export class RegistrationComponent implements OnInit {
  form: FormGroup;
  isSubmitted:boolean = false;
  errorMessage:string | null = null;
  submitSuccess:string | null = null;
  
  i18nService = inject(I18nService);
  cdr = inject(ChangeDetectorRef);
  currentLanguage: Language = 'en';
  translationsLoaded = false;

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
      confirmPassword: ['']
    }, {validators:this.passwordMatchValidator});
  }

  ngOnInit(): void {
    // Set initial language
    this.currentLanguage = this.i18nService.getCurrentLanguageValue();
    this.translationsLoaded = this.i18nService.isTranslationsLoaded();
    
    // Subscribe to language changes
    this.i18nService.getCurrentLanguage().subscribe(lang => {
      this.currentLanguage = lang;
      this.cdr.detectChanges();
    });
    
    // Subscribe to translations loaded
    this.i18nService.getTranslationsLoaded().subscribe(loaded => {
      this.translationsLoaded = loaded;
      this.cdr.detectChanges();
    });
  }

  changeLanguage(lang: Language): void {
    this.i18nService.setLanguage(lang);
  }

  t(key: string): string {
    return this.i18nService.translate(key);
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
          this.submitSuccess = this.t('patientRegistration.success.registrationSuccessful');
          this.form.reset();
        },
        error: (err) => {
          console.error('Registration failed:', err);
          if(err.status===400){
            this.errorMessage = this.t('patientRegistration.errors.emailInUse');
          }
        }
      });
}
}
}
