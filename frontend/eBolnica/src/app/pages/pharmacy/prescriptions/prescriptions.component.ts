import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize } from 'rxjs';

@Component({
  selector: 'app-prescriptions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './prescriptions.component.html',
  styleUrl: './prescriptions.component.css'
})
export class PrescriptionsComponent implements OnInit {
  private pharmacyService = inject(PharmacyService);

  prescriptions: PrescriptionDto[] = [];
  filteredPrescriptions: PrescriptionDto[] = [];
  isLoading: boolean = false;
  errorMessage: string | null = null;

  // Status filter
  selectedStatus: string = 'Pending';
  statusFilters = [
    { value: 'All', label: 'All Prescriptions', count: 0 },
    { value: 'Pending', label: 'Pending', count: 0 },
    { value: 'Dispensed', label: 'Dispensed', count: 0 },
    { value: 'Cancelled', label: 'Cancelled', count: 0 }
  ];

  // Search
  searchTerm: string = '';
  private searchSubject = new Subject<string>();

  // Sort
  sortBy: string = 'date';
  sortOrder: 'asc' | 'desc' = 'desc';

  ngOnInit(): void {
    this.loadPrescriptions();
    
    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.applyFilters();
    });
  }

  loadPrescriptions(): void {
    this.isLoading = true;
    this.errorMessage = null;

    // Load all prescriptions, we'll filter client-side
    this.pharmacyService.getPrescriptions().pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (prescriptions) => {
        this.prescriptions = prescriptions;
        this.updateFilterCounts();
        this.applyFilters();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load prescriptions. Please try again later.';
        console.error('Error loading prescriptions:', error);
      }
    });
  }

  updateFilterCounts(): void {
    this.statusFilters.forEach(filter => {
      if (filter.value === 'All') {
        filter.count = this.prescriptions.length;
      } else {
        filter.count = this.prescriptions.filter(p => p.status === filter.value).length;
      }
    });
  }

  onStatusFilterChange(status: string): void {
    this.selectedStatus = status;
    this.applyFilters();
  }

  onSearchChange(searchTerm: string): void {
    this.searchSubject.next(searchTerm);
  }

  onSortChange(sortBy: string): void {
    if (this.sortBy === sortBy) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = sortBy;
      this.sortOrder = 'desc';
    }
    this.applyFilters();
  }

  applyFilters(): void {
    let filtered = [...this.prescriptions];

    // Status filter
    if (this.selectedStatus && this.selectedStatus !== 'All') {
      filtered = filtered.filter(p => p.status === this.selectedStatus);
    }

    // Search filter
    if (this.searchTerm.trim()) {
      const search = this.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(p => 
        p.prescriptionNumber.toLowerCase().includes(search) ||
        (p.patient && `${p.patient.firstName} ${p.patient.lastName}`.toLowerCase().includes(search))
      );
    }

    // Sort
    filtered.sort((a, b) => {
      let comparison = 0;
      
      switch (this.sortBy) {
        case 'date':
          comparison = new Date(a.prescribedDate).getTime() - new Date(b.prescribedDate).getTime();
          break;
        case 'status':
          comparison = a.status.localeCompare(b.status);
          break;
        case 'amount':
          comparison = a.totalAmount - b.totalAmount;
          break;
        case 'number':
          comparison = a.prescriptionNumber.localeCompare(b.prescriptionNumber);
          break;
        default:
          comparison = 0;
      }
      
      return this.sortOrder === 'asc' ? comparison : -comparison;
    });

    this.filteredPrescriptions = filtered;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'status-pending';
      case 'Dispensed':
        return 'status-dispensed';
      case 'Cancelled':
        return 'status-cancelled';
      default:
        return 'status-default';
    }
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

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
