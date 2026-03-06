import { Component, inject } from '@angular/core';
import { AuthService } from '../../../shared/services/auth.service';
import { FormsModule } from "@angular/forms";
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { interval, Subscription } from 'rxjs';
import { DoctorService } from '../../../shared/services/doctor/doctor.service';
import { Chart } from 'chart.js/auto';

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

  monthlyChart?: Chart;
  bloodTypeChart?: Chart;

  ngOnInit(): void{
    this.loadStats();

    this.refreshSubscription = interval(30000).subscribe(()=>{
      this.loadStats(true);
    })
  }

  ngOnDestroy(): void{
    this.refreshSubscription?.unsubscribe();

    if(this.monthlyChart) this.monthlyChart.destroy();
    if(this.bloodTypeChart) this.bloodTypeChart.destroy();
  }

  loadStats(silent = false): void{
    if(!silent) this.isLoading=true;

    this.doctorService.getStats().subscribe({
      next: (data) => {
        this.stats = data;
        this.lastUpdated = new Date();
        this.isLoading = false;

          if (this.stats && 
          this.stats.monthlyReportTrend && 
          this.stats.bloodTypeDistribution) {
        setTimeout(() => {
          this.createMonthlyTrendChart();
          this.createBloodTypeChart();
        }, 100);
      }
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

  createMonthlyTrendChart(): void{
    if(this.monthlyChart){
      this.monthlyChart.destroy();
    }

    const ctx = document.getElementById('monthlyTrendChart') as HTMLCanvasElement;

    this.monthlyChart = new Chart(ctx, {
      type: 'line',
      data:{
        labels: this.stats.monthlyReportTrend.map((m:any) => m.month),
        datasets: [{
          label: 'Medical Reports',
          data: this.stats.monthlyReportTrend.map((m:any) => m.count),
          borderColor: '#1976d2',
          backgroundColor: 'rgba(25, 118, 210, 0.1)',
          tension: 0.4,
          fill: true
        }]
      },
      options:{
        responsive: true,
        plugins: {
          title:{
            display: true,
            text: 'Monthly Report Trend (Last 6 Months)'
          },
          legend:{
            display:false
          }
        },
        scales:{
          y:{
            beginAtZero: true,
            ticks:{stepSize:1}
          }
        }
      }
    });
  }

   createBloodTypeChart(): void{
      if(this.bloodTypeChart){
        this.bloodTypeChart?.destroy();
      }

      const ctx = document.getElementById('bloodTypeChart') as HTMLCanvasElement;

      this.bloodTypeChart = new Chart(ctx,{
        type: 'doughnut',
        data: {
          labels: this.stats.bloodTypeDistribution.map((b:any) => b.bloodType),
          datasets: [{
            data: this.stats.bloodTypeDistribution.map((b:any) => b.count),
            backgroundColor: [
            '#f44336',
            '#e91e63',
            '#9c27b0',
            '#673ab7',
            '#3f51b5',
            '#2196f3',
            '#03a9f4',
            '#00bcd4'
            ]
          }]
        },
        options: {
          responsive: true,
          plugins:{
            title:{
              display:true,
              text: 'Blood Type Distribution'
            }
          }
        }
      });
    }

}
