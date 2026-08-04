import { Component, inject, OnInit } from '@angular/core';
import { DoctorApiService } from '../../../api-services/doctor/doctor-api.service';
import { DoctorStatsDto } from '../../../api-services/doctor/doctor-api.models';

@Component({
  selector: 'app-doctor-dashboard',
  standalone: false,
  templateUrl: './doctor-dashboard.component.html',
  styleUrl: './doctor-dashboard.component.scss',
})
export class DoctorDashboardComponent implements OnInit {
  private doctorApi = inject(DoctorApiService);

  stats: DoctorStatsDto | null = null;
  isLoading = true;
  lastUpdated = new Date();

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.isLoading = true;
    this.doctorApi.getStats().subscribe({
      next: (data) => {
        this.stats = data;
        this.lastUpdated = new Date();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }
}
