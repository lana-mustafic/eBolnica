import { Component, inject } from '@angular/core';
import { AuthService } from '../../../shared/services/auth.service';
import { FormsModule } from "@angular/forms";
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { OnInit, OnDestroy } from '@angular/core';
import { interval, Subscription } from 'rxjs';
import { DoctorService } from '../../../shared/services/doctor/doctor.service';

@Component({
  selector: 'app-doctor-dashboard',
  imports: [FormsModule,RouterModule, CommonModule],
  standalone: true,
  templateUrl: './doctor-dashboard.component.html',
  styleUrl: './doctor-dashboard.component.css'
})
export class DoctorDashboardComponent {
  public authService = inject(AuthService);
  public doctorService = inject(DoctorService);

  stats: any = null;
  isLoading = true;
  lastUpdated: Date = new Date;
  private refreshSubscription?: Subscription;

  ngOnInit(): void{
    this.loadStats();

    this.refreshSubscription = interval(30000).subscribe(()=>{
      this.loadStats(true);
    })
  }

  ngOnDestroy(): void{
    this.refreshSubscription?.unsubscribe();
  }

  loadStats(silent = false): void{
    if(!silent) this.isLoading=true;

    this.doctorService.getStats().subscribe({
      next: (data) => {
        this.stats = data;
        this.lastUpdated = new Date();
        this.isLoading = false;
      },
      error: (err) =>{
        console.error('Error loading stats', err);
        this.isLoading = false;
      }
    });
  }

  manualRefresh(): void{
    this.loadStats();
  }

}
