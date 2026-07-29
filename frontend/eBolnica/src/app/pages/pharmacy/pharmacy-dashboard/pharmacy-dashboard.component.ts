import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { AuthService } from '../../../shared/services/auth.service';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { FormsModule } from "@angular/forms";
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { forkJoin, finalize, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { RevenueBarChartComponent } from '../../../features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';
import { CategoriesPieChartComponent } from '../../../features/pharmacy/analytics/components/categories-pie-chart/categories-pie-chart.component';
import { StockTrendsLineChartComponent } from '../../../features/pharmacy/analytics/components/stock-trends-line-chart/stock-trends-line-chart.component';

type AnalyticsSource = 'summary' | 'revenue' | 'categories' | 'stock';

const ANALYTICS_SOURCE_LABELS: Record<AnalyticsSource, string> = {
  summary: 'summary metrics',
  revenue: 'monthly revenue',
  categories: 'top categories',
  stock: 'stock levels'
};

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

  @ViewChild(RevenueBarChartComponent) revenueChart?: RevenueBarChartComponent;
  @ViewChild(CategoriesPieChartComponent) categoriesChart?: CategoriesPieChartComponent;
  @ViewChild(StockTrendsLineChartComponent) stockChart?: StockTrendsLineChartComponent;

  // Metrics (from GET /api/pharmacy/analytics/dashboard-stats)
  totalMedications: number = 0;
  pendingPrescriptions: number = 0;
  lowStockAlerts: number = 0;
  expiringSoon: number = 0;
  expiredMedications: number = 0;
  inventoryValue: number = 0;

  recentPrescriptions: PrescriptionDto[] = [];

  isLoading: boolean = true;
  /** Fatal error (e.g. prescriptions list); hides the dashboard body. */
  errorMessage: string | null = null;
  private analyticsFailures = new Set<AnalyticsSource>();

  get hasAnalyticsErrors(): boolean {
    return this.analyticsFailures.size > 0;
  }

  get analyticsErrorMessage(): string | null {
    if (!this.hasAnalyticsErrors) {
      return null;
    }

    const labels = Array.from(this.analyticsFailures)
      .map(source => ANALYTICS_SOURCE_LABELS[source])
      .join(', ');

    return `Analytics data could not be loaded (${labels}). Charts may be incomplete until you retry.`;
  }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.errorMessage = null;

    forkJoin({
      summary: this.pharmacyService.getDashboardSummaryMetrics().pipe(
        catchError(error => {
          this.registerAnalyticsFailure('summary', error);
          return of(null);
        })
      ),
      prescriptions: this.pharmacyService.getPrescriptions({
        status: 'Pending',
        page: 1,
        pageSize: 5
      })
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: ({ summary, prescriptions }) => {
        if (summary) {
          this.applySummaryMetrics(summary);
          this.clearAnalyticsFailure('summary');
        }
        this.recentPrescriptions = prescriptions.items ?? [];
      },
      error: (error) => {
        this.errorMessage = 'Failed to load dashboard data. Please try again later.';
        console.error('Error loading dashboard data:', error);
      }
    });
  }

  onAnalyticsError(source: AnalyticsSource, error: Error): void {
    this.registerAnalyticsFailure(source, error);
  }

  onAnalyticsLoaded(source: AnalyticsSource): void {
    this.clearAnalyticsFailure(source);
  }

  retryAnalytics(): void {
    this.pharmacyService.clearAnalyticsCache();
    this.loadSummaryMetrics();
    this.revenueChart?.refresh();
    this.categoriesChart?.refresh();
    this.stockChart?.refresh();
  }

  private loadSummaryMetrics(): void {
    this.pharmacyService.getDashboardSummaryMetrics(false).subscribe({
      next: (summary) => {
        this.applySummaryMetrics(summary);
        this.clearAnalyticsFailure('summary');
      },
      error: (error) => this.registerAnalyticsFailure('summary', error)
    });
  }

  private applySummaryMetrics(summary: {
    totalMedications?: number;
    pendingPrescriptions?: number;
    lowStockAlerts?: number;
    expiringSoon?: number;
    expiredMedications?: number;
    inventoryValue?: number;
  }): void {
    this.totalMedications = summary.totalMedications ?? 0;
    this.pendingPrescriptions = summary.pendingPrescriptions ?? 0;
    this.lowStockAlerts = summary.lowStockAlerts ?? 0;
    this.expiringSoon = summary.expiringSoon ?? 0;
    this.expiredMedications = summary.expiredMedications ?? 0;
    this.inventoryValue = summary.inventoryValue ?? 0;
  }

  private registerAnalyticsFailure(source: AnalyticsSource, error: unknown): void {
    this.analyticsFailures.add(source);
    console.error(`[PharmacyDashboard] Analytics error (${source}):`, error);
  }

  private clearAnalyticsFailure(source: AnalyticsSource): void {
    this.analyticsFailures.delete(source);
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
