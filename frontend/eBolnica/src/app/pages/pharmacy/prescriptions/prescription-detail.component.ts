import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { PrescriptionDto } from '../../../models/prescription.dto';
import { PrescriptionItemDto } from '../../../models/prescription-item.dto';
import { MedicationDto } from '../../../models/medication.dto';
import { PrescriptionDispenseDto } from '../../../models/prescription-dispense.dto';
import { PharmacistDataDto } from '../../../models/pharmacist-data.dto';
import { finalize, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';

interface StockIssue {
  medicationName: string;
  required: number;
  available: number;
  isCritical: boolean;
}

interface ValidationResult {
  isValid: boolean;
  issues: StockIssue[];
  message: string;
}

@Component({
  selector: 'app-prescription-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './prescription-detail.component.html',
  styleUrl: './prescription-detail.component.css'
})
export class PrescriptionDetailComponent implements OnInit {
  private pharmacyService = inject(PharmacyService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  prescription: PrescriptionDto | null = null;
  medications: MedicationDto[] = [];
  pharmacistData: PharmacistDataDto | null = null;
  
  isLoading: boolean = false;
  isDispensing: boolean = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  
  prescriptionId: number | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.prescriptionId = +id;
      this.loadPrescriptionData();
    }
  }

  loadPrescriptionData(): void {
    if (!this.prescriptionId) return;

    this.isLoading = true;
    this.errorMessage = null;

    forkJoin({
      prescription: this.pharmacyService.getPrescriptionById(this.prescriptionId),
      pharmacistData: this.pharmacyService.getPharmacistData()
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (data) => {
        this.prescription = data.prescription;
        this.pharmacistData = data.pharmacistData;
        this.loadMedicationStockForPrescription();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load prescription details. Please try again.';
        console.error('Error loading prescription:', error);
      }
    });
  }

  validateStockForDispense(): ValidationResult {
    if (!this.prescription) {
      return {
        isValid: false,
        issues: [],
        message: 'Prescription not found'
      };
    }

    const issues: StockIssue[] = [];

    this.prescription.prescriptionItems.forEach(item => {
      const medication = this.medications.find(m => m.id === item.medicationId);
      if (medication) {
        if (medication.stockQuantity < item.quantity) {
          issues.push({
            medicationName: medication.name,
            required: item.quantity,
            available: medication.stockQuantity,
            isCritical: medication.stockQuantity === 0
          });
        }
      } else {
        // Medication not found in inventory
        issues.push({
          medicationName: item.medicationName,
          required: item.quantity,
          available: 0,
          isCritical: true
        });
      }
    });

    return {
      isValid: issues.length === 0,
      issues,
      message: issues.length > 0
        ? `${issues.length} medication(s) have insufficient stock`
        : 'All medications have sufficient stock'
    };
  }

  getStockStatus(medicationId: number, requiredQuantity: number): { label: string; class: string; icon: string } {
    const medication = this.medications.find(m => m.id === medicationId);
    
    if (!medication) {
      return { label: 'Not Found', class: 'stock-not-found', icon: '❌' };
    }

    if (medication.stockQuantity === 0) {
      return { label: 'Out of Stock', class: 'stock-out', icon: '🔴' };
    }

    if (medication.stockQuantity < requiredQuantity) {
      return { label: 'Low Stock', class: 'stock-low', icon: '🟠' };
    }

    if (medication.stockQuantity < medication.minimumStockLevel) {
      return { label: 'Below Minimum', class: 'stock-warning', icon: '🟡' };
    }

    return { label: 'In Stock', class: 'stock-ok', icon: '🟢' };
  }

  onDispenseClick(): void {
    if (!this.prescription || this.prescription.status !== 'Pending') {
      return;
    }

    // Validate stock first
    const validation = this.validateStockForDispense();
    
    if (!validation.isValid) {
      // Show stock issues
      let message = 'Cannot dispense prescription due to insufficient stock:\n\n';
      validation.issues.forEach(issue => {
        message += `• ${issue.medicationName}: Required ${issue.required}, Available ${issue.available}\n`;
      });
      alert(message);
      return;
    }

    // Confirm dispense
    if (!confirm(`Are you sure you want to dispense prescription ${this.prescription.prescriptionNumber}?`)) {
      return;
    }

    // Perform dispense
    this.dispensePrescription();
  }

  dispensePrescription(): void {
    if (!this.prescription || !this.pharmacistData) {
      this.errorMessage = 'Missing prescription or pharmacist data.';
      return;
    }

    this.isDispensing = true;
    this.errorMessage = null;
    this.successMessage = null;

    const dispenseData: PrescriptionDispenseDto = {
      pharmacistId: this.pharmacistData.id,
      dispensedDate: new Date().toISOString()
    };

    this.pharmacyService.dispensePrescription(this.prescription.id, dispenseData).pipe(
      finalize(() => this.isDispensing = false)
    ).subscribe({
      next: (updatedPrescription) => {
        this.prescription = updatedPrescription;
        this.successMessage = `Prescription ${updatedPrescription.prescriptionNumber} dispensed successfully.`;
        
        // Update medications stock (optimistic update)
        this.updateMedicationsStock(updatedPrescription);
        
        // Clear success message after 3 seconds
        setTimeout(() => {
          this.successMessage = null;
        }, 3000);
      },
      error: (error) => {
        if (error.error?.message) {
          this.errorMessage = error.error.message;
        } else {
          this.errorMessage = 'Failed to dispense prescription. Please try again.';
        }
        console.error('Error dispensing prescription:', error);
      }
    });
  }

  updateMedicationsStock(prescription: PrescriptionDto): void {
    prescription.prescriptionItems.forEach(item => {
      const medication = this.medications.find(m => m.id === item.medicationId);
      if (medication) {
        medication.stockQuantity = Math.max(0, medication.stockQuantity - item.quantity);
      }
    });
  }

  getPatientName(): string {
    if (this.prescription?.patient) {
      return `${this.prescription.patient.firstName} ${this.prescription.patient.lastName}`;
    }
    return `Patient #${this.prescription?.patientId || 'N/A'}`;
  }

  getDoctorName(): string {
    if (this.prescription?.doctor) {
      return `Dr. ${this.prescription.doctor.firstName} ${this.prescription.doctor.lastName}`;
    }
    return `Doctor #${this.prescription?.doctorId || 'N/A'}`;
  }

  getDoctorSpecialization(): string {
    return this.prescription?.doctor?.specialization || 'N/A';
  }

  getPharmacistName(): string {
    if (this.prescription?.pharmacist) {
      return `${this.prescription.pharmacist.firstName} ${this.prescription.pharmacist.lastName}`;
    }
    return 'N/A';
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  calculateSubtotal(): number {
    if (!this.prescription) return 0;
    return this.prescription.prescriptionItems.reduce((sum, item) => sum + item.totalPrice, 0);
  }

  calculateTotal(): number {
    return this.prescription?.totalAmount || this.calculateSubtotal();
  }

  onBack(): void {
    this.router.navigate(['/pharmacy/prescriptions']);
  }

  private loadMedicationStockForPrescription(): void {
    if (!this.prescription?.prescriptionItems?.length) {
      this.medications = [];
      return;
    }

    const uniqueIds = [...new Set(this.prescription.prescriptionItems.map(item => item.medicationId))];
    forkJoin(
      uniqueIds.map(id => this.pharmacyService.getMedicationById(id))
    ).pipe(
      map(meds => meds.filter((med): med is MedicationDto => !!med))
    ).subscribe({
      next: meds => {
        this.medications = meds;
      },
      error: () => {
        this.medications = [];
      }
    });
  }
}
