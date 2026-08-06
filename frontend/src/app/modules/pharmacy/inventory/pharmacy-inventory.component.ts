import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, debounceTime, of, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-pharmacy-inventory',
  standalone: false,
  templateUrl: './pharmacy-inventory.component.html',
  styleUrl: './pharmacy-inventory.component.scss',
})
export class PharmacyInventoryComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);

  items: MedicationDto[] = [];
  lowStockAlerts: MedicationDto[] = [];
  expiryAlerts: MedicationDto[] = [];
  lowStockAlertCount = 0;
  expiryAlertCount = 0;
  isLoading = true;
  loadError = false;
  totalCount = 0;
  currentPage = 1;
  totalPages = 0;

  search = '';
  selectedCategory = '';
  selectedStockStatus = '';
  selectedRequiresPrescription = '';

  sortBy = 'name';
  sortOrder: 'asc' | 'desc' = 'asc';

  displayedColumns = ['name', 'stock', 'expiry'];

  private filterChanged$ = new Subject<void>();
  private loadTrigger$ = new Subject<void>();

  ngOnInit(): void {
    this.loadTrigger$
      .pipe(
        switchMap(() => {
          this.isLoading = true;
          this.loadError = false;
          return this.pharmacyApi.getInventory(this.buildRequest()).pipe(
            catchError(() => {
              this.loadError = true;
              this.items = [];
              this.lowStockAlerts = [];
              this.expiryAlerts = [];
              this.toaster.error('Greška pri učitavanju inventara.');
              return of(null);
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((res) => {
        this.isLoading = false;
        if (!res) {
          return;
        }

        this.items = res.items;
        this.lowStockAlerts = res.lowStockAlerts;
        this.expiryAlerts = res.expiryAlerts;
        this.lowStockAlertCount = res.lowStockAlertCount ?? res.lowStockAlerts.length;
        this.expiryAlertCount = res.expiryAlertCount ?? res.expiryAlerts.length;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.currentPage = res.currentPage;
      });

    this.filterChanged$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadTrigger$.next();
      });

    this.loadTrigger$.next();
  }

  private buildRequest() {
    const requiresPrescription =
      this.selectedRequiresPrescription === 'true'
        ? true
        : this.selectedRequiresPrescription === 'false'
          ? false
          : undefined;

    return {
      search: this.search || undefined,
      category: this.selectedCategory || undefined,
      stockStatus: this.selectedStockStatus || undefined,
      requiresPrescription,
      pageNumber: this.currentPage,
      pageSize: 10,
      sortBy: this.sortBy,
      sortOrder: this.sortOrder,
    };
  }

  onFilterChange(): void {
    this.filterChanged$.next();
  }

  clearFilters(): void {
    this.search = '';
    this.selectedCategory = '';
    this.selectedStockStatus = '';
    this.selectedRequiresPrescription = '';
    this.currentPage = 1;
    this.loadTrigger$.next();
  }

  retryLoad(): void {
    this.loadTrigger$.next();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadTrigger$.next();
  }

  onSort(column: string): void {
    if (this.sortBy === column) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortOrder = 'asc';
    }
    this.currentPage = 1;
    this.loadTrigger$.next();
  }

  sortIndicator(column: string): string {
    if (this.sortBy !== column) return '';
    return this.sortOrder === 'asc' ? ' ▲' : ' ▼';
  }

  sortAriaSort(column: string): 'ascending' | 'descending' | 'none' {
    if (this.sortBy !== column) return 'none';
    return this.sortOrder === 'asc' ? 'ascending' : 'descending';
  }

  onSortKeydown(event: KeyboardEvent, column: string): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onSort(column);
    }
  }
}
