import { Component, inject, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { DoctorApiService } from '../../../api-services/doctor/doctor-api.service';
import { DoctorProfileDto } from '../../../api-services/doctor/doctor-api.models';
import { DoctorProfileEditDialogComponent } from './doctor-profile-edit-dialog/doctor-profile-edit-dialog.component';

@Component({
  selector: 'app-doctor-profile',
  standalone: false,
  templateUrl: './doctor-profile.component.html',
  styleUrl: './doctor-profile.component.scss',
})
export class DoctorProfileComponent implements OnInit {
  private doctorApi = inject(DoctorApiService);
  private dialog = inject(MatDialog);

  doctor: DoctorProfileDto | null = null;
  isLoading = true;

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.doctorApi.getProfile().subscribe({
      next: (data) => {
        this.doctor = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  openEditDialog(): void {
    if (!this.doctor) return;

    const ref = this.dialog.open(DoctorProfileEditDialogComponent, {
      width: '600px',
      data: this.doctor,
    });

    ref.afterClosed().subscribe((updated) => {
      if (updated) this.loadProfile();
    });
  }
}
