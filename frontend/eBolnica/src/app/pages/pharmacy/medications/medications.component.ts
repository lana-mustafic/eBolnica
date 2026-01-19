import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { MedicationDto } from '../../../models/medication.dto';
import { Subject, debounceTime, distinctUntilChanged, finalize } from 'rxjs';

@Component({
  selector: 'app-medications',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './medications.component.html',
  styleUrl: './medications.component.css'
})
export class MedicationsComponent implements OnInit {
  private pharmacyService = inject(PharmacyService);

  medications: MedicationDto[] = [];
  filteredMedications: MedicationDto[] = [];
  isLoading: boolean = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  // Search
  searchTerm: string = '';
  private searchSubject = new Subject<string>();

  // Filters
  selectedCategory: string = '';
  selectedStockStatus: string = '';
  selectedRequiresPrescription: string = '';
  selectedActiveStatus: string = '';

  // Available categories (populated from medications)
  categories: string[] = [];

  ngOnInit(): void {
    this.loadMedications();
    
    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.searchTerm = searchTerm;
      this.applyFilters();
    });
  }

  loadMedications(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.pharmacyService.getAllMedications().pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (medications) => {
        this.medications = medications;
        this.extractCategories();
        this.applyFilters();
      },
      error: (error) => {
        this.errorMessage = 'Failed to load medications. Please try again later.';
        console.error('Error loading medications:', error);
      }
    });
  }

  extractCategories(): void {
    const categorySet = new Set<string>();
    this.medications.forEach(med => {
      if (med.category) {
        categorySet.add(med.category);
      }
    });
    this.categories = Array.from(categorySet).sort();
  }

  onSearchChange(searchTerm: string): void {
    this.searchSubject.next(searchTerm);
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategory = '';
    this.selectedStockStatus = '';
    this.selectedRequiresPrescription = '';
    this.selectedActiveStatus = '';
    this.applyFilters();
  }

  applyFilters(): void {
    let filtered = [...this.medications];

    // Search filter
    if (this.searchTerm.trim()) {
      const search = this.searchTerm.toLowerCase().trim();
      filtered = filtered.filter(med => 
        med.name.toLowerCase().includes(search) ||
        (med.genericName && med.genericName.toLowerCase().includes(search)) ||
        (med.manufacturer && med.manufacturer.toLowerCase().includes(search))
      );
    }

    // Category filter
    if (this.selectedCategory) {
      filtered = filtered.filter(med => med.category === this.selectedCategory);
    }

    // Stock status filter
    if (this.selectedStockStatus) {
      switch (this.selectedStockStatus) {
        case 'Low Stock':
          filtered = filtered.filter(med => 
            med.isActive && med.stockQuantity < med.minimumStockLevel
          );
          break;
        case 'Out of Stock':
          filtered = filtered.filter(med => med.stockQuantity === 0);
          break;
        case 'Normal Stock':
          filtered = filtered.filter(med => 
            med.isActive && med.stockQuantity >= med.minimumStockLevel
          );
          break;
      }
    }

    // Requires prescription filter
    if (this.selectedRequiresPrescription) {
      const requires = this.selectedRequiresPrescription === 'Yes';
      filtered = filtered.filter(med => med.requiresPrescription === requires);
    }

    // Active status filter
    if (this.selectedActiveStatus) {
      const isActive = this.selectedActiveStatus === 'Active';
      filtered = filtered.filter(med => med.isActive === isActive);
    }

    this.filteredMedications = filtered;
  }

  deleteMedication(medication: MedicationDto): void {
    if (confirm(`Are you sure you want to delete "${medication.name}"? This action cannot be undone.`)) {
      this.isLoading = true;
      this.errorMessage = null;
      this.successMessage = null;

      this.pharmacyService.deleteMedication(medication.id).subscribe({
        next: () => {
          this.isLoading = false;
          this.successMessage = `Medication "${medication.name}" deleted successfully.`;
          this.loadMedications();
          // Clear success message after 3 seconds
          setTimeout(() => this.successMessage = null, 3000);
        },
        error: (error) => {
          this.isLoading = false;
          if (error.error?.message) {
            this.errorMessage = error.error.message;
          } else {
            this.errorMessage = 'Failed to delete medication. Please try again.';
          }
          console.error('Error deleting medication:', error);
        }
      });
    }
  }

  getStockStatus(medication: MedicationDto): { label: string; class: string } {
    if (!medication.isActive) {
      return { label: 'Inactive', class: 'status-inactive' };
    }
    
    if (medication.expiryDate) {
      const expiry = new Date(medication.expiryDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (expiry < today) {
        return { label: 'Expired', class: 'status-expired' };
      }
    }

    if (medication.stockQuantity === 0) {
      return { label: 'Out of Stock', class: 'status-out-of-stock' };
    }

    if (medication.stockQuantity < medication.minimumStockLevel) {
      return { label: 'Low Stock', class: 'status-low-stock' };
    }

    return { label: 'Active', class: 'status-active' };
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return '-';
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
