import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../shared/services/admin.service';
import { AuthService } from '../shared/services/auth.service';

@Component({
  selector: 'app-admin-dashboard',
  imports: [CommonModule,FormsModule],
  standalone: true,
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit{
  users: any[]=[];
  status = ['Pending','Approved','Rejected'];  

  private adminService = inject(AdminService);
  public authService = inject(AuthService);

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(){
    this.adminService.getAllUsers().subscribe(data=>{
      this.users = data;
    });
  }

  changeStatus(user:any, status:string){
    this.adminService.updateRegistrationStatus(user.appUserId, status).subscribe({
      next: (res) =>{
        user.doctorInfo.registrationStatus = status;
        console.log(res.message);
      },
      error: (err) =>{
        console.error(err.message);
      }
    })
  }

  onLogout(): void{
      this.authService.logout();
  }
}
