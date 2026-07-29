import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../../../shared/services/auth.service';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { FormsModule } from "@angular/forms";
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { forkJoin, finalize } from 'rxjs';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { RevenueBarChartComponent } from '../../../features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';
import { CategoriesPieChartComponent } from '../../../features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';
import { StockTrendsLineChartComponent } from '../../../features/pharmacy/analytics/components/stock-trends-line-chart/stock-trends-line-chart.component';

/**
 * Pharmacy dashboard overview.
 *
 * Metrics audit (hardcoded/mock sources, pagination risks, chart fallbacks):
 * @see ./PHARMACY_DASHBOARD_METRICS_AUDIT.md
 */
@Component({
  selector: 'app-pharmacy-dashboard',
  imports: [
    FormsModule, 
    RouterModule, 
    CommonModule,
    RevenueBarChartComponent,
    CategoriesPieChartComponent,
    StockTrendsLineChartComponent
  ],
  standalone: true,
  templateUrl: './pharmacy-dashboard.component.html',
  styleUrl: './pharmacy-dashboard.component.css'
})
export class PharmacyDashboardComponent implements OnInit {
  public authService = inject(AuthService);
  private pharmacyService = inject(PharmacyService);
  private router = inject(Router);

  // Metrics (from GET /api/pharmacy/analytics/dashboard-stats)
  totalMedications: number = 0;
  pendingPrescriptions: number = 0;
  lowStockAlerts: number = 0;
  expiringSoon: number = 0;

  recentPrescriptions: PrescriptionDto[] = [];

  isLoading: boolean = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.errorMessage = null;

    forkJoin({
      summary: this.pharmacyService.getDashboardSummaryMetrics(),
      prescriptions: this.pharmacyService.getPrescriptions({
        status: 'Pending',
        page: 1,
        pageSize: 5
      })
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: ({ summary, prescriptions }) => {
        this.totalMedications = summary.totalMedications ?? 0;
        this.pendingPrescriptions = summary.pendingPrescriptions ?? 0;
        this.lowStockAlerts = summary.lowStockAlerts ?? 0;
        this.expiringSoon = summary.expiringSoon ?? 0;
        this.recentPrescriptions = prescriptions.items ?? [];
      },
      error: (error) => {
        this.errorMessage = 'Failed to load dashboard data. Please try again later.';
        console.error('Error loading dashboard data:', error);
      }
    });
  }

  navigateToPrescription(prescriptionId: number): void {
    this.router.navigate(['/pharmacy/prescriptions', prescriptionId]);
  }

  navigateToRoute(route: string): void {
    this.router.navigate([route]);
  }

  getPatientName(prescription: PrescriptionDto): string {
    if (prescription.patient) {
      return `${prescription.patient.firstName} ${prescription.patient.lastName}`;
    }
    return `Patient #${prescription.patientId}`;
  }

  getDoctorName(prescription: PrescriptionDto): string {
    if (prescription.doctor) {
      return `Dr. ${prescription.doctor.firstName} ${prescription.doctor.lastName}`;
    }
    return `Doctor #${prescription.doctorId}`;
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    });
  }
}
