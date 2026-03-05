import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminService } from '../shared/services/admin.service';
import { AuthService } from '../shared/services/auth.service';
import { UserOverview } from '../models/user-overview.dto';
import { ConfirmModalComponent } from '../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-admin-dashboard',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ConfirmModalComponent],
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

  showModal = false;
  isEditMode = false;
  selectedUserId: string | null = null;
  errorMessage = '';
  successMessage = '';

  showConfirmModal = false;
  confirmMessage = '';
  pendingDeleteUserId: string | null = null;

  userForm!: FormGroup;

  private fb = inject(FormBuilder);
  private adminService = inject(AdminService);
  public authService = inject(AuthService);

  ngOnInit(): void {
    this.initForm();
    this.loadUsers();
  }

  initForm(): void{
    this.userForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName:  ['', [Validators.required, Validators.minLength(2)]],
      email:     ['', [Validators.required, Validators.email]],
      password:  ['', [Validators.required, Validators.minLength(6)]],
      userType:  ['Patient', Validators.required]
    });
  }

  get firstName() { return this.userForm.get('firstName'); }
  get lastName()  { return this.userForm.get('lastName'); }
  get email()     { return this.userForm.get('email'); }
  get password()  { return this.userForm.get('password'); }
  get userTypeCtrl() { return this.userForm.get('userType'); }

  loadUsers(): void{
    this.adminService.getAllUsers(this.page, this.pageSize, this.userType?? undefined, this.currentSortBy, this.currentSortDirection).subscribe(res=>{
      console.log("Backend response:", res);
      this.users=res.users;
      this.totalCount=res.totalCount;
    }
    );
  }

  openCreateModal(): void{
    this.isEditMode = false;
    this.selectedUserId = null;
    this.errorMessage = '';
    this.userForm.reset({userType:'Patient'});
    this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    this.userForm.get('password')?.updateValueAndValidity();
    this.showModal = true; 
  }

  openEditModal(user: UserOverview): void{
    this.isEditMode = true;
    this.selectedUserId = user.appUserId;
    this.errorMessage='';

    this.userForm.get('password')?.clearValidators();
    this.userForm.get('password')?.updateValueAndValidity();
    this.userForm.patchValue({
      firstName:user.firstName,
      lastName:user.lastName,
      email:user.email,
      userType:user.userType,
      password:''
    });

    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.userForm.reset();
  }

  submitForm(): void{
    if(this.userForm.invalid){
      this.userForm.markAllAsTouched();
      return;
    }

    if(this.isEditMode && this.selectedUserId){
      const {firstName, lastName, email }= this.userForm.value;
      this.adminService.updateUser(this.selectedUserId, {firstName,lastName,email}).subscribe({
        next: () =>{
          this.successMessage = 'User updated successfully!';
          this.closeModal();
          this.loadUsers();
        },
        error: (err) => this.errorMessage = err.error?.message || 'Update failed'
      });
    } else {
      this.adminService.createUser(this.userForm.value).subscribe({
        next: () => {
          this.successMessage = 'User created successfully!';
          this.closeModal();
          this.loadUsers();
        },
        error: (err) => this.errorMessage = err.error?.message ||'Creation failed'
      });
    }
  }

  deleteUser(user: UserOverview): void {
  this.pendingDeleteUserId = user.appUserId;
  this.confirmMessage = `Are you sure you want to delete ${user.firstName} ${user.lastName}?`;
  this.showConfirmModal = true;
  }

  onDeleteConfirmed(): void {
  if (!this.pendingDeleteUserId) return;

  this.adminService.deleteUser(this.pendingDeleteUserId).subscribe({
    next: () => {
      this.successMessage = 'User deleted.';
      this.loadUsers();
    },
    error: (err) => this.errorMessage = err.error?.message || 'Delete failed.'
  });

  this.showConfirmModal = false;
  this.pendingDeleteUserId = null;
  }

  onDeleteCancelled(): void {
    this.showConfirmModal = false;
    this.pendingDeleteUserId = null;
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