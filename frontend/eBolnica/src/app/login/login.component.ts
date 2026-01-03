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
            console.log('User logged', res.token);
            localStorage.setItem('jwtToken',res.token); 
            
            const role = this.authService.getUserType();

            if(role==='Admin'){
              this.router.navigateByUrl('/admin-dashboard');
            }else if(role==='Doctor'){
              this.router.navigateByUrl('/doctor-dashboard');
            }else if(role==='Patient'){
              this.router.navigateByUrl('/patient-dashboard');
            }
          },
          error: (err) =>{
            console.log('Login failed', err)
            if(err.status === 401){
              this.errorMessage ='Incorrect email or password';
            } else if(err.status===500){
              this.errorMessage='Account waiting for approval';
            }
          }
        })
      }
      
}

