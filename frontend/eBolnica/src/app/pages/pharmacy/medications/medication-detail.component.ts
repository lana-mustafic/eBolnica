import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { MedicationDto } from '../../../models/medication.dto';
import { MedicationImageDto } from '../../../models/medication-image.dto';
import { MedicationAiSummaryDto } from '../../../models/medication-ai-summary.dto';
import { MedicationImageGalleryComponent } from './medication-image-gallery.component';
import { PharmacyShellComponent } from '../pharmacy-shell/pharmacy-shell.component';
import {
  getMedicationAiSummaryErrorMessage,
  resolveMedicationAiSummaryState,
  MedicationAiSummaryState
} from './medication-ai-summary.util';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-medication-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MedicationImageGalleryComponent, PharmacyShellComponent],
  templateUrl: './medication-detail.component.html',
  styleUrl: './medication-detail.component.css'
})
export class MedicationDetailComponent implements OnInit {
  private pharmacyService = inject(PharmacyService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  medication: MedicationDto | null = null;
  images: MedicationImageDto[] = [];
  isLoading = false;
  errorMessage: string | null = null;
  medicationId: number | null = null;

  isGeneratingAiSummary = false;
  aiSummaryError: string | null = null;
  aiSummary: MedicationAiSummaryDto | null = null;

  get aiSummaryState(): MedicationAiSummaryState {
    return resolveMedicationAiSummaryState(
      this.isGeneratingAiSummary,
      this.aiSummaryError,
      this.aiSummary
    );
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.medicationId = +id;
      this.loadMedication();
    }
  }

  loadMedication(): void {
    if (!this.medicationId) return;

    this.isLoading = true;
    this.errorMessage = null;
    this.resetAiSummaryState();

    this.pharmacyService.getMedicationById(this.medicationId).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (medication) => {
        this.medication = medication;
        this.images = medication.images ?? [];
      },
      error: () => {
        this.errorMessage = 'Failed to load medication details. Please try again.';
      }
    });
  }

  onImagesChange(images: MedicationImageDto[]): void {
    this.images = images;
    if (this.medication) {
      const primary = images.find(img => img.isPrimary) ?? images[0];
      this.medication = {
        ...this.medication,
        images,
        primaryImageUrl: primary ? (primary.thumbnailUrl ?? primary.imageUrl) : undefined
      };
    }
  }

  getStockStatus(): { label: string; class: string } {
    if (!this.medication) {
      return { label: 'Unknown', class: 'status-inactive' };
    }

    if (!this.medication.isActive) {
      return { label: 'Inactive', class: 'status-inactive' };
    }

    if (this.medication.expiryDate) {
      const expiry = new Date(this.medication.expiryDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (expiry < today) {
        return { label: 'Expired', class: 'status-expired' };
      }
    }

    if (this.medication.stockQuantity === 0) {
      return { label: 'Out of Stock', class: 'status-out-of-stock' };
    }

    if (this.medication.stockQuantity < this.medication.minimumStockLevel) {
      return { label: 'Low Stock', class: 'status-low-stock' };
    }

    return { label: 'Active', class: 'status-active' };
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  onBack(): void {
    this.router.navigate(['/pharmacy/medications']);
  }

  onEdit(): void {
    if (this.medicationId) {
      this.router.navigate(['/pharmacy/medications', this.medicationId, 'edit']);
    }
  }

  onGenerateAiSummary(): void {
    if (!this.medicationId || this.isGeneratingAiSummary) {
      return;
    }

    this.isGeneratingAiSummary = true;
    this.aiSummaryError = null;
    this.aiSummary = null;

    this.pharmacyService.generateMedicationAiSummary(this.medicationId).pipe(
      finalize(() => {
        this.isGeneratingAiSummary = false;
      })
    ).subscribe({
      next: (summary) => {
        this.aiSummary = summary;
      },
      error: (error) => {
        this.aiSummaryError = getMedicationAiSummaryErrorMessage(error);
      }
    });
  }

  private resetAiSummaryState(): void {
    this.isGeneratingAiSummary = false;
    this.aiSummaryError = null;
    this.aiSummary = null;
  }
}
