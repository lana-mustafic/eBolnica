import { Component, inject, OnInit } from '@angular/core';
import { AuthService } from '../../../shared/services/auth.service';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { FormsModule } from "@angular/forms";
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { forkJoin, finalize } from 'rxjs';
import { MedicationDto } from '../../../models/medication.dto';
import { PrescriptionDto } from '../../../models/prescription.dto';

@Component({
  selector: 'app-pharmacy-dashboard',
  imports: [FormsModule, RouterModule, CommonModule],
  standalone: true,
  templateUrl: './pharmacy-dashboard.component.html',
  styleUrl: './pharmacy-dashboard.component.css'
})
export class PharmacyDashboardComponent implements OnInit {
  public authService = inject(AuthService);
  private pharmacyService = inject(PharmacyService);
  private router = inject(Router);

  // Data
  medications: MedicationDto[] = [];
  prescriptions: PrescriptionDto[] = [];
  
  // Metrics
  totalMedications: number = 0;
  pendingPrescriptions: number = 0;
  lowStockAlerts: number = 0;
  expiringSoon: number = 0;
  
  // Recent prescriptions (pending)
  recentPrescriptions: PrescriptionDto[] = [];
  
  // Loading and error states
  isLoading: boolean = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.errorMessage = null;

    forkJoin({
      medications: this.pharmacyService.getAllMedications(),
      prescriptions: this.pharmacyService.getPrescriptions()
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (data) => {
        this.medications = data.medications;
        this.prescriptions = data.prescriptions;
        this.calculateMetrics();
        this.recentPrescriptions = this.getRecentPrescriptions();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load dashboard data. Please try again later.';
        console.error('Error loading dashboard data:', error);
      }
    });
  }

  calculateMetrics(): void {
    // Total active medications
    this.totalMedications = this.medications.filter(m => m.isActive).length;

    // Pending prescriptions
    this.pendingPrescriptions = this.prescriptions.filter(p => p.status === 'Pending').length;

    // Low stock alerts
    this.lowStockAlerts = this.medications.filter(m => 
      m.isActive && m.stockQuantity < m.minimumStockLevel
    ).length;

    // Expiring soon (within next 30 days)
    const today = new Date();
    const in30Days = new Date();
    in30Days.setDate(today.getDate() + 30);

    this.expiringSoon = this.medications.filter(m => {
      if (!m.expiryDate || !m.isActive) return false;
      const expiry = new Date(m.expiryDate);
      return expiry >= today && expiry <= in30Days;
    }).length;
  }

  getRecentPrescriptions(): PrescriptionDto[] {
    return this.prescriptions
      .filter(p => p.status === 'Pending')
      .sort((a, b) => {
        const dateA = new Date(a.prescribedDate).getTime();
        const dateB = new Date(b.prescribedDate).getTime();
        return dateB - dateA; // Most recent first
      })
      .slice(0, 5); // Get top 5 most recent
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
