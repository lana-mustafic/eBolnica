import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { BaseListComponent } from '../../../core/components/base-classes/base-list-component';
import { AdminApiService } from '../../../api-services/admin/admin-api.service';
import { UserOverviewDto } from '../../../api-services/admin/admin-api.models';
import { DialogButton } from '../../shared/models/dialog-config.model';
import { DialogHelperService } from '../../shared/services/dialog-helper.service';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-admin-users',
  standalone: false,
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss',
})
export class AdminUsersComponent extends BaseListComponent<UserOverviewDto> implements OnInit {
  private adminApi = inject(AdminApiService);
  private fb = inject(FormBuilder);
  private dialog = inject(DialogHelperService);
  private toaster = inject(ToasterService);

  totalCount = 0;
  page = 1;
  pageSize = 10;
  userTypeFilter: string | null = null;
  sortBy = 'firstName';
  sortDirection: 'asc' | 'desc' = 'asc';

  showDialog = false;
  isEditMode = false;
  selectedUserId: number | null = null;
  approvedDoctors: UserOverviewDto[] = [];

  displayedColumns = ['firstName', 'lastName', 'email', 'userType', 'status', 'actions'];

  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    userType: ['Patient', Validators.required],
    doctorId: [null as number | null],
    licenseNumber: [''],
  });

  ngOnInit(): void {
    this.initList();
    this.loadApprovedDoctors();
  }

  protected loadData(): void {
    this.startLoading();
    this.adminApi
      .listUsers({
        page: this.page,
        pageSize: this.pageSize,
        userType: this.userTypeFilter,
        sortBy: this.sortBy,
        sortDirection: this.sortDirection,
      })
      .subscribe({
        next: (res) => {
          this.items = res.users;
          this.totalCount = res.totalCount;
          this.stopLoading();
        },
        error: () => this.stopLoading('Greška pri učitavanju korisnika.'),
      });
  }

  loadApprovedDoctors(): void {
    this.adminApi
      .listUsers({ userType: 'Doctor', pageSize: 100, sortBy: 'lastName', sortDirection: 'asc' })
      .subscribe({
        next: (res) => {
          this.approvedDoctors = res.users.filter((d) => d.registrationStatus === 'Approved');
        },
      });
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  onFilterChange(value: string): void {
    this.userTypeFilter = value || null;
    this.page = 1;
    this.loadData();
  }

  sort(column: string): void {
    if (this.sortBy === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDirection = 'asc';
    }
    this.page = 1;
    this.loadData();
  }

  onPageChange(newPage: number): void {
    if (newPage < 1 || newPage > this.totalPages) return;
    this.page = newPage;
    this.loadData();
  }

  openCreateDialog(): void {
    this.isEditMode = false;
    this.selectedUserId = null;
    this.form.reset({ userType: 'Patient', doctorId: null });
    this.form.get('password')?.setValidators([Validators.required, Validators.minLength(6)]);
    this.form.get('password')?.updateValueAndValidity();
    this.showDialog = true;
  }

  openEditDialog(user: UserOverviewDto): void {
    this.isEditMode = true;
    this.selectedUserId = user.appUserId;
    this.form.get('password')?.clearValidators();
    this.form.get('password')?.updateValueAndValidity();
    this.form.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      userType: user.userType,
      password: '',
    });
    this.showDialog = true;
  }

  closeDialog(): void {
    this.showDialog = false;
  }

  submitForm(): void {
    if (this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    this.startLoading();
    const raw = this.form.getRawValue();

    if (this.isEditMode && this.selectedUserId) {
      this.adminApi
        .updateUser(this.selectedUserId, {
          firstName: raw.firstName ?? '',
          lastName: raw.lastName ?? '',
          email: raw.email ?? '',
        })
        .subscribe({
          next: () => {
            this.toaster.success('Korisnik ažuriran.');
            this.closeDialog();
            this.loadData();
            this.stopLoading();
          },
          error: (err) =>
            this.stopLoading(err?.error?.message ?? 'Ažuriranje nije uspjelo.'),
        });
      return;
    }

    this.adminApi
      .createUser({
        firstName: raw.firstName ?? '',
        lastName: raw.lastName ?? '',
        email: raw.email ?? '',
        password: raw.password ?? '',
        userType: raw.userType ?? 'Patient',
        doctorId: raw.userType === 'Patient' ? raw.doctorId : null,
        licenseNumber: raw.licenseNumber || null,
      })
      .subscribe({
        next: () => {
          this.toaster.success('Korisnik kreiran.');
          this.closeDialog();
          this.loadData();
          this.loadApprovedDoctors();
          this.stopLoading();
        },
        error: (err) => this.stopLoading(err?.error?.message ?? 'Kreiranje nije uspjelo.'),
      });
  }

  changeStatus(user: UserOverviewDto, status: string): void {
    const request =
      user.userType === 'Doctor'
        ? this.adminApi.updateDoctorRegistrationStatus(user.appUserId, { registrationStatus: status })
        : user.userType === 'Patient'
          ? this.adminApi.updatePatientRegistrationStatus(user.appUserId, { registrationStatus: status })
          : null;

    if (!request) return;

    request.subscribe({
      next: () => {
        user.registrationStatus = status;
        this.toaster.success('Status ažuriran.');
        if (status === 'Approved' && user.userType === 'Doctor') {
          this.loadApprovedDoctors();
        }
      },
      error: (err) => this.toaster.error(err?.error?.message ?? 'Status nije ažuriran.'),
    });
  }

  confirmDelete(user: UserOverviewDto): void {
    this.dialog
      .confirmDelete(`${user.firstName} ${user.lastName}`)
      .subscribe((result) => {
        if (result?.button === DialogButton.DELETE || result?.button === DialogButton.YES) {
          this.adminApi.deleteUser(user.appUserId).subscribe({
            next: () => {
              this.toaster.success('Korisnik obrisan.');
              this.loadData();
            },
            error: (err) => this.toaster.error(err?.error?.message ?? 'Brisanje nije uspjelo.'),
          });
        }
      });
  }
}
