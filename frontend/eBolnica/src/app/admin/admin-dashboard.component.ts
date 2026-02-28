import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../shared/services/admin.service';
import { AuthService } from '../shared/services/auth.service';
import { UserOverview } from '../models/user-overview.dto';

@Component({
  selector: 'app-admin-dashboard',
  imports: [CommonModule,FormsModule],
  standalone: true,
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit{
  users:  UserOverview[] = [];
  status = ['Pending','Approved','Rejected'];  

  totalCount = 0;
  page = 1;
  pageSize = 10;

  userType: string | null = null;

  currentSortBy: string = 'firstName';
  currentSortDirection: 'asc' | 'desc' = 'asc';

  private adminService = inject(AdminService);
  public authService = inject(AuthService);

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void{
    this.adminService.getAllUsers(this.page, this.pageSize, this.userType?? undefined, this.currentSortBy, this.currentSortDirection).subscribe(res=>{
      console.log("Backend response:", res);
      this.users=res.users;
      this.totalCount=res.totalCount;
    }
    );
  }

  sortBy(column: string): void{
    if(this.currentSortBy === column){
      // Toggle direction if column already sorted
       this.currentSortDirection = this.currentSortDirection === 'asc' ? 'desc' : 'asc';
    }
    else{
      this.currentSortBy = column;
      this.currentSortDirection = 'asc';
    }

    this.page = 1;
    
    this.loadUsers();
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
    this.adminService.updateRegistrationStatus(user.appUserId, status).subscribe({
      next: (res) =>{
        user.registrationStatus = status;     
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
