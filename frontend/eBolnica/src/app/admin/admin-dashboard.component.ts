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
  totalCount = 0;
  page = 1;
  pageSize = 10;
  userType: string | null = null;
  
  private adminService = inject(AdminService);
  public authService = inject(AuthService);

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void{
    this.adminService.getAllUsers(this.page, this.pageSize, this.userType?? undefined).subscribe(res=>{
      console.log("Backend response:", res);
      this.users=res.users;
      this.totalCount=res.totalCount;
    }
    );
  }

  onPageChange(newPage: number): void {
    this.page = newPage;
    this.loadUsers();
  }

  onFilterChange(type: string): void {
    this.userType = type || null;
    this.page = 1;
    this.loadUsers();
  }

  get totalPages(): number {
  return Math.ceil(this.totalCount / this.pageSize);
}

  changeStatus(user:any, status:string){
    this.adminService.updateRegistrationStatus(user.id, status).subscribe({
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
