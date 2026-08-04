import { Component, inject, OnInit } from '@angular/core';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto } from '../../../api-services/pharmacy/pharmacy-api.models';

@Component({
  selector: 'app-pharmacy-inventory',
  standalone: false,
  templateUrl: './pharmacy-inventory.component.html',
  styleUrl: './pharmacy-inventory.component.scss',
})
export class PharmacyInventoryComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);

  items: MedicationDto[] = [];
  lowStockAlerts: MedicationDto[] = [];
  expiryAlerts: MedicationDto[] = [];
  isLoading = true;
  totalCount = 0;
  currentPage = 1;
  totalPages = 0;

  sortBy = 'name';
  sortOrder: 'asc' | 'desc' = 'asc';

  displayedColumns = ['name', 'stock', 'expiry'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.pharmacyApi
      .getInventory({
        pageNumber: this.currentPage,
        pageSize: 10,
        sortBy: this.sortBy,
        sortOrder: this.sortOrder,
      })
      .subscribe({
      next: (res) => {
        this.items = res.items;
        this.lowStockAlerts = res.lowStockAlerts;
        this.expiryAlerts = res.expiryAlerts;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.currentPage = res.currentPage;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.load();
  }

  onSort(column: string): void {
    if (this.sortBy === column) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortOrder = 'asc';
    }
    this.currentPage = 1;
    this.load();
  }

  sortIndicator(column: string): string {
    if (this.sortBy !== column) return '';
    return this.sortOrder === 'asc' ? ' ▲' : ' ▼';
  }
}
