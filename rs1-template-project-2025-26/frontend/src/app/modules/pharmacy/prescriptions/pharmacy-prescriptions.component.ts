import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PharmacyApiService } from '../../../api-services/pharmacy/pharmacy-api.service';
import { PrescriptionDto } from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-pharmacy-prescriptions',
  standalone: false,
  templateUrl: './pharmacy-prescriptions.component.html',
  styleUrl: './pharmacy-prescriptions.component.scss',
})
export class PharmacyPrescriptionsComponent implements OnInit {
  private pharmacyApi = inject(PharmacyApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);

  prescriptions: PrescriptionDto[] = [];
  isLoading = false;
  totalCount = 0;
  totalPages = 0;
  currentPage = 1;
  pageSize = 10;

  search = '';
  selectedStatus = 'Pending';

  statusFilters = [
    { value: 'All', label: 'Svi' },
    { value: 'Pending', label: 'Na čekanju' },
    { value: 'Dispensed', label: 'Izdani' },
    { value: 'Cancelled', label: 'Otkazani' },
  ];

  displayedColumns = ['number', 'patient', 'doctor', 'status', 'amount', 'date', 'actions'];

  ngOnInit(): void {
    this.loadPrescriptions();
  }

  loadPrescriptions(): void {
    this.isLoading = true;
    this.pharmacyApi
      .listPrescriptions({
        status: this.selectedStatus === 'All' ? undefined : this.selectedStatus,
        search: this.search.trim() || undefined,
        pageNumber: this.currentPage,
        pageSize: this.pageSize,
        sortBy: 'prescribedDate',
        sortOrder: 'desc',
      })
      .subscribe({
        next: (res) => {
          this.prescriptions = res.items;
          this.totalCount = res.totalCount;
          this.totalPages = res.totalPages;
          this.currentPage = res.currentPage;
          this.isLoading = false;
        },
        error: () => {
          this.toaster.error('Greška pri učitavanju recepata.');
          this.isLoading = false;
        },
      });
  }

  onStatusChange(status: string): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadPrescriptions();
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadPrescriptions();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadPrescriptions();
  }

  openDetail(id: number): void {
    this.router.navigate(['/pharmacy/prescriptions', id]);
  }

  exportPdf(): void {
    this.pharmacyApi
      .exportPrescriptionsPdf({
        status: this.selectedStatus === 'All' ? undefined : this.selectedStatus,
        search: this.search.trim() || undefined,
        sortBy: 'prescribedDate',
        sortOrder: 'desc',
      })
      .subscribe({
        next: (res) => {
          this.pharmacyApi.downloadBlobResponse(res, 'prescriptions.pdf');
          this.toaster.success('PDF recepata preuzet.');
        },
        error: () => this.toaster.error('Greška pri exportu PDF.'),
      });
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'status-pending';
      case 'Dispensed':
        return 'status-dispensed';
      case 'Cancelled':
        return 'status-cancelled';
      default:
        return '';
    }
  }
}
