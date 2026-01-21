/**
 * Integration Example: Adding Revenue Bar Chart to Pharmacy Dashboard
 * 
 * This file shows how to integrate the RevenueBarChartComponent into
 * the existing PharmacyDashboardComponent.
 */

// ============================================
// Step 1: Update pharmacy-dashboard.component.ts
// ============================================

/*
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../shared/services/auth.service';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { forkJoin, finalize } from 'rxjs';
import { MedicationDto } from '../../../models/medication.dto';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { RevenueBarChartComponent } from '../../../features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';
import { MonthlyRevenueData } from '../../../models/analytics.dto';

@Component({
  selector: 'app-pharmacy-dashboard',
  standalone: true,
  imports: [
    FormsModule, 
    RouterModule, 
    CommonModule,
    RevenueBarChartComponent  // Add this import
  ],
  templateUrl: './pharmacy-dashboard.component.html',
  styleUrl: './pharmacy-dashboard.component.css'
})
export class PharmacyDashboardComponent implements OnInit {
  public authService = inject(AuthService);
  private pharmacyService = inject(PharmacyService);
  private router = inject(Router);

  // Existing properties...
  medications: MedicationDto[] = [];
  prescriptions: PrescriptionDto[] = [];
  totalMedications: number = 0;
  pendingPrescriptions: number = 0;
  lowStockAlerts: number = 0;
  expiringSoon: number = 0;
  recentPrescriptions: PrescriptionDto[] = [];
  isLoading: boolean = true;
  errorMessage: string | null = null;

  // Add analytics properties
  revenuePeriod: 'last6months' | 'last12months' = 'last12months';
  revenueData: MonthlyRevenueData[] = [];

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.errorMessage = null;

    forkJoin({
      medications: this.pharmacyService.getAllMedications({
        isActive: true,
        page: 1,
        pageSize: 1000
      }),
      prescriptions: this.pharmacyService.getPrescriptions()
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (data) => {
        this.medications = data.medications.items || [];
        this.prescriptions = data.prescriptions.items || [];
        this.calculateMetrics();
        this.recentPrescriptions = this.getRecentPrescriptions();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load dashboard data. Please try again later.';
        console.error('Error loading dashboard data:', error);
      }
    });
  }

  // Add handler for revenue data loaded event
  onRevenueDataLoaded(data: MonthlyRevenueData[]): void {
    this.revenueData = data;
    console.log('Revenue data loaded:', data.length, 'months');
  }

  // Add handler for revenue chart errors
  onRevenueError(error: Error): void {
    console.error('Revenue chart error:', error);
    // Optionally show notification to user
  }

  // Add handler for bar click
  onRevenueBarClick(event: { month: string; revenue: number }): void {
    console.log(`Clicked on ${event.month}: $${event.revenue}`);
    // Optionally navigate to detail page or show modal
  }

  // Existing methods...
  calculateMetrics(): void {
    // ... existing implementation
  }

  getRecentPrescriptions(): PrescriptionDto[] {
    // ... existing implementation
  }

  // ... rest of existing methods
}
*/

// ============================================
// Step 2: Update pharmacy-dashboard.component.html
// ============================================

/*
<!-- Add this section after the metrics cards, before recent prescriptions -->

<!-- Analytics Section -->
<section class="analytics-section" *ngIf="!isLoading && !errorMessage">
  <h2 class="section-title">Analytics</h2>
  
  <!-- Period Selector -->
  <div class="period-selector">
    <button 
      class="period-btn"
      [class.active]="revenuePeriod === 'last6months'"
      (click)="revenuePeriod = 'last6months'">
      Last 6 Months
    </button>
    <button 
      class="period-btn"
      [class.active]="revenuePeriod === 'last12months'"
      (click)="revenuePeriod = 'last12months'">
      Last 12 Months
    </button>
  </div>

  <!-- Revenue Chart -->
  <div class="chart-container">
    <app-revenue-bar-chart 
      [title]="'Monthly Revenue Overview'"
      [period]="revenuePeriod"
      [height]="400"
      (dataLoaded)="onRevenueDataLoaded($event)"
      (errorOccurred)="onRevenueError($event)"
      (barClicked)="onRevenueBarClick($event)">
    </app-revenue-bar-chart>
  </div>
</section>
*/

// ============================================
// Step 3: Add CSS styles (pharmacy-dashboard.component.css)
// ============================================

/*
.analytics-section {
  margin-top: 2rem;
  padding: 1.5rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.section-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: #2d3748;
  margin-bottom: 1.5rem;
}

.period-selector {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #e2e8f0;
}

.period-btn {
  padding: 0.5rem 1rem;
  background: #f7fafc;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  color: #718096;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.period-btn:hover {
  background: #edf2f7;
  border-color: #cbd5e0;
}

.period-btn.active {
  background: #3b82f6;
  color: white;
  border-color: #3b82f6;
}

.chart-container {
  margin-top: 1rem;
}

@media (max-width: 768px) {
  .analytics-section {
    padding: 1rem;
  }

  .period-selector {
    flex-direction: column;
  }

  .period-btn {
    width: 100%;
  }
}
*/

// ============================================
// Alternative: Simple Integration (Minimal Changes)
// ============================================

/*
// Just add the component to your template:

<app-revenue-bar-chart 
  [title]="'Monthly Revenue'"
  [height]="400">
</app-revenue-bar-chart>

// And import it in your component:

import { RevenueBarChartComponent } from '../../../features/pharmacy/analytics/components/revenue-bar-chart/revenue-bar-chart.component';

@Component({
  imports: [
    // ... existing imports
    RevenueBarChartComponent
  ]
})
*/
