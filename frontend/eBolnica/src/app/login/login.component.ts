import { Component } from '@angular/core';
import { AuthService } from '../shared/services/auth.service';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})


export class LoginComponent {

    loginData = {
      email: '',
      password: ''
    };
    errorMessage: string | null = null;

    public router=inject(Router);
    public authService=inject(AuthService);

    

    onLogin(){
        this.authService.login(this.loginData.email,this.loginData.password).subscribe({
          next: (res) => {
            console.log('User logged', res);
            const token = res.Token || res.token;
            localStorage.setItem('jwtToken', token); 
            
            const role = this.authService.getUserType();
            console.log('User role:', role);

            if(role==='Admin'){
              this.router.navigateByUrl('/admin-dashboard');
            }else if(role==='Doctor'){
              this.router.navigateByUrl('/doctor-dashboard');
            }else if(role==='Patient'){
              this.router.navigateByUrl('/patient-dashboard');
            }else if(role==='Pharmacist'){
              this.router.navigateByUrl('/pharmacy-dashboard');
            } else {
              console.error('Unknown role:', role);
              this.errorMessage = 'Unknown user role';
            }
          },
          error: (err) =>{
            console.log('Login failed', err)
            if(err.status === 401){
              this.errorMessage ='Incorrect email or password';
            } else if(err.status === 403){
              this.errorMessage='Account waiting for approval';
            } else if(err.status===500){
              this.errorMessage='Server error. Please try again.';
            } else {
              this.errorMessage='Login failed. Please try again.';
            }
          }
        })
      }
      
}

