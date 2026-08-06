import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Subject, catchError, debounceTime, of, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { MedicationDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { AuthFacadeService } from '../../../core/services/auth/auth-facade.service';
import { getMedicationCategoryLabel } from '../constants/medication-categories.constant';
import { ToasterService } from '../../../core/services/toaster.service';

interface InventoryActivityItem {
  type: 'success' | 'warning';
  message: string;
}

@Component({
  selector: 'app-pharmacy-inventory',
  standalone: false,
  templateUrl: './pharmacy-inventory.component.html',
  styleUrl: './pharmacy-inventory.component.scss',
})
export class PharmacyInventoryComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private toaster = inject(ToasterService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  auth = inject(AuthFacadeService);

  readonly categoryLabel = getMedicationCategoryLabel;

  items: MedicationDto[] = [];
  lowStockAlerts: MedicationDto[] = [];
  expiryAlerts: MedicationDto[] = [];
  lowStockAlertCount = 0;
  expiryAlertCount = 0;
  inventoryValue = 0;
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

  displayedColumns = ['name', 'stock', 'expiry', 'actions'];
  selectedMedication: MedicationDto | null = null;

  private filterChanged$ = new Subject<void>();
  private loadTrigger$ = new Subject<void>();

  ngOnInit(): void {
    this.pharmacyApi
      .getDashboardStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.inventoryValue = res.metadata.summary.inventoryValue;
        },
        error: () => {
          this.inventoryValue = 0;
        },
      });

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

  get firstLowStockAlert(): MedicationDto | null {
    return this.lowStockAlerts[0] ?? null;
  }

  get recentActivities(): InventoryActivityItem[] {
    const activities: InventoryActivityItem[] = [];

    for (const medication of this.lowStockAlerts.slice(0, 3)) {
      activities.push({
        type: 'warning',
        message: `${medication.name} ispod minimuma zalihe (${medication.stockQuantity} / min ${medication.minimumStockLevel})`,
      });
    }

    for (const medication of this.expiryAlerts.slice(0, 3)) {
      const date = medication.expiryDate
        ? new Date(medication.expiryDate).toLocaleDateString('bs-BA')
        : '-';
      activities.push({
        type: 'warning',
        message: `${medication.name} ističe uskoro (${date})`,
      });
    }

    if (activities.length === 0) {
      activities.push({
        type: 'success',
        message: 'Nema aktivnih upozorenja za inventar.',
      });
    }

    return activities.slice(0, 6);
  }

  hasActiveFilters(): boolean {
    return !!(
      this.search ||
      this.selectedCategory ||
      this.selectedStockStatus ||
      this.selectedRequiresPrescription
    );
  }

  getStockStatus(m: MedicationDto): string {
    if (m.stockQuantity <= 0) return 'Kritično';
    if (m.stockQuantity < m.minimumStockLevel) return 'Niska zaliha';
    return 'Dostupno';
  }

  getStockStatusClass(m: MedicationDto): string {
    const status = this.getStockStatus(m);
    if (status === 'Niska zaliha') return 'low';
    if (status === 'Kritično') return 'critical';
    return 'ok';
  }

  getStockFillPercent(m: MedicationDto): number {
    if (m.minimumStockLevel <= 0) {
      return m.stockQuantity > 0 ? 100 : 0;
    }

    const target = m.minimumStockLevel * 2;
    return Math.min(100, Math.round((m.stockQuantity / target) * 100));
  }

  getExpiryStatus(m: MedicationDto): string {
    if (!m.expiryDate) return '-';

    const now = new Date();
    const expiry = new Date(m.expiryDate);
    const horizon = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);

    if (expiry < now) return 'Istekao';
    if (expiry <= horizon) return 'Ističe uskoro';
    return 'Važi';
  }

  getExpiryStatusClass(m: MedicationDto): string {
    const status = this.getExpiryStatus(m);
    if (status === 'Ističe uskoro') return 'low';
    if (status === 'Istekao') return 'critical';
    if (status === 'Važi') return 'ok';
    return 'neutral';
  }

  viewMedication(id: number): void {
    this.router.navigate(['/pharmacy/medications', id]);
  }

  editMedication(id: number): void {
    this.router.navigate(['/pharmacy/medications', id, 'edit']);
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
