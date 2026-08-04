import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Subject, debounceTime } from 'rxjs';
import { DoctorApiService } from '../../../api-services/doctor/doctor-api.service';
import {
  DoctorAssignedPatientDto,
  ListDoctorPatientsRequest,
} from '../../../api-services/doctor/doctor-api.models';

@Component({
  selector: 'app-doctor-patients',
  standalone: false,
  templateUrl: './doctor-patients.component.html',
  styleUrl: './doctor-patients.component.scss',
})
export class DoctorPatientsComponent implements OnInit {
  private doctorApi = inject(DoctorApiService);
  private router = inject(Router);

  patients: DoctorAssignedPatientDto[] = [];
  isLoading = false;
  totalCount = 0;
  totalPages = 0;
  currentPage = 1;
  pageSize = 10;

  filters: ListDoctorPatientsRequest = {
    firstName: '',
    lastName: '',
    gender: '',
    bloodType: '',
    birthYear: undefined,
  };

  private filterChanged$ = new Subject<void>();

  ngOnInit(): void {
    this.filterChanged$.pipe(debounceTime(300)).subscribe(() => {
      this.currentPage = 1;
      this.loadPatients();
    });
    this.loadPatients();
  }

  loadPatients(): void {
    this.isLoading = true;
    this.doctorApi
      .listPatients({
        ...this.filters,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.patients = res.items;
          this.totalCount = res.totalCount;
          this.totalPages = res.totalPages;
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
        },
      });
  }

  onFilterChange(): void {
    this.filterChanged$.next();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadPatients();
  }

  openMedicalRecord(patientId: number): void {
    this.router.navigate(['/doctor/medical-record', patientId]);
  }

  displayedColumns = [
    'firstName',
    'lastName',
    'dateOfBirth',
    'gender',
    'recordNumber',
    'bloodType',
    'actions',
  ];
}
