import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Column display name mapping for user-friendly names
 */
const COLUMN_DISPLAY_NAMES: { [key: string]: string } = {
  // Medications
  'name': 'Name',
  'category': 'Category',
  'price': 'Price',
  'stockQuantity': 'Stock Quantity',
  'stock': 'Stock Quantity',
  'status': 'Status',
  'createdAt': 'Date Created',
  'updatedAt': 'Last Updated',
  
  // Prescriptions
  'patientName': 'Patient Name',
  'medicationName': 'Medication',
  'prescribedDate': 'Prescription Date',
  'prescriptionDate': 'Prescription Date',
  'totalAmount': 'Total Amount',
  'totalPrice': 'Total Price',
  'prescriptionStatus': 'Status',
  
  // Inventory
  'batchNumber': 'Batch Number',
  'expiryDate': 'Expiry Date',
  'expiry': 'Expiry Date',
  'supplier': 'Supplier',
  'manufacturer': 'Manufacturer',
  'stockStatus': 'Stock Status',
  'quantity': 'Quantity'
};

@Component({
  selector: 'app-sort-status',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sort-status.component.html',
  styleUrl: './sort-status.component.css'
})
export class SortStatusComponent implements OnChanges {
  @Input() sortColumn: string = '';
  @Input() sortOrder: 'asc' | 'desc' = 'desc';
  @Input() isDefaultSort: boolean = false;
  @Input() componentType: 'medications' | 'prescriptions' | 'inventory' = 'medications';
  @Input() showResetButton: boolean = true;
  
  @Output() reset = new EventEmitter<void>();

  sortAnnouncement: string = '';
  sortChanged: boolean = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['sortColumn'] || changes['sortOrder']) {
      this.updateSortAnnouncement();
      this.triggerSortChangedAnimation();
    }
  }

  /**
   * Get user-friendly display name for column
   */
  getColumnDisplayName(column: string): string {
    if (!column) return '';
    
    // Check direct mapping first
    if (COLUMN_DISPLAY_NAMES[column]) {
      return COLUMN_DISPLAY_NAMES[column];
    }
    
    // Convert camelCase to Words (e.g., "stockQuantity" -> "Stock Quantity")
    return column
      .split(/(?=[A-Z])/)
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
  }

  /**
   * Get sort direction text
   */
  getSortDirectionText(): string {
    return this.sortOrder === 'asc' ? 'Ascending' : 'Descending';
  }

  /**
   * Get sort icon character
   */
  getSortIcon(): string {
    return this.sortOrder === 'asc' ? '↑' : '↓';
  }

  /**
   * Handle reset button click
   */
  onReset(): void {
    this.reset.emit();
  }

  /**
   * Update screen reader announcement
   */
  private updateSortAnnouncement(): void {
    if (!this.sortColumn) {
      this.sortAnnouncement = '';
      return;
    }

    const columnName = this.getColumnDisplayName(this.sortColumn);
    const direction = this.getSortDirectionText().toLowerCase();
    
    this.sortAnnouncement = `Table sorted by ${columnName} in ${direction} order`;
  }

  /**
   * Trigger sort changed animation
   */
  private triggerSortChangedAnimation(): void {
    this.sortChanged = true;
    setTimeout(() => {
      this.sortChanged = false;
    }, 500);
  }

  /**
   * Scroll to sort header (if needed)
   */
  scrollToSortHeader(): void {
    // This can be implemented if headers have IDs
    // For now, just focus on the sort status element
    const element = document.querySelector('.sort-status');
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
  }
}
