import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, Subject, switchMap } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import {
  CreatePrescriptionRequest,
  MedicationAutocompleteSuggestion,
  PrescriptionFormMedicalReportDto,
  PrescriptionFormPatientDto,
} from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';

@Component({
  selector: 'app-prescription-form',
  standalone: false,
  templateUrl: './prescription-form.component.html',
  styleUrl: './prescription-form.component.scss',
})
export class PrescriptionFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private pharmacyApi = inject(PharmacyApiService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);

  form = this.fb.group({
    patientId: this.fb.control<number | null>(null, Validators.required),
    medicalReportId: this.fb.control<number | null>(null, Validators.required),
    notes: this.fb.control(''),
    items: this.fb.array([this.createItemGroup()]),
  });

  patientSearch = '';
  patients: PrescriptionFormPatientDto[] = [];
  selectedPatient: PrescriptionFormPatientDto | null = null;
  medicalReports: PrescriptionFormMedicalReportDto[] = [];
  medicationSuggestions: MedicationAutocompleteSuggestion[] = [];
  activeItemIndex = 0;
  isSaving = false;
  isLoadingReports = false;

  private patientSearch$ = new Subject<string>();
  private medicationSearch$ = new Subject<string>();

  ngOnInit(): void {
    this.patientSearch$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((term) => this.pharmacyApi.searchPrescriptionPatients(term)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((patients) => {
        this.patients = patients;
      });

    this.medicationSearch$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((term) => this.pharmacyApi.getAutocomplete(term, 10, true)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((suggestions) => {
        this.medicationSuggestions = suggestions;
      });

    this.patientSearch$.next('');
  }

  get items(): FormArray {
    return this.form.get('items') as FormArray;
  }

  createItemGroup() {
    return this.fb.group({
      medicationId: this.fb.control<number | null>(null, Validators.required),
      medicationName: this.fb.control(''),
      quantity: this.fb.control(1, [Validators.required, Validators.min(1), Validators.max(1000)]),
      instructions: this.fb.control(''),
    });
  }

  addItem(): void {
    this.items.push(this.createItemGroup());
  }

  removeItem(index: number): void {
    if (this.items.length <= 1) {
      return;
    }
    this.items.removeAt(index);
  }

  onPatientSearchInput(): void {
    this.patientSearch$.next(this.patientSearch);
  }

  selectPatient(patient: PrescriptionFormPatientDto): void {
    this.selectedPatient = patient;
    this.form.patchValue({ patientId: patient.id, medicalReportId: null });
    this.patients = [];
    this.patientSearch = `${patient.firstName} ${patient.lastName}`;
    this.loadMedicalReports(patient.id);
  }

  loadMedicalReports(patientId: number): void {
    this.isLoadingReports = true;
    this.medicalReports = [];
    this.pharmacyApi
      .listPatientMedicalReportsForPrescription(patientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (reports) => {
          this.medicalReports = reports;
          this.isLoadingReports = false;
          if (reports.length === 1) {
            this.form.patchValue({ medicalReportId: reports[0].id });
          }
        },
        error: () => {
          this.isLoadingReports = false;
          this.toaster.error('Greška pri učitavanju medicinskih izvještaja.');
        },
      });
  }

  onMedicationSearch(term: string, index: number): void {
    this.activeItemIndex = index;
    if (term.trim().length < 2) {
      this.medicationSuggestions = [];
      return;
    }
    this.medicationSearch$.next(term);
  }

  selectMedication(suggestion: MedicationAutocompleteSuggestion, index: number): void {
    const group = this.items.at(index);
    group.patchValue({
      medicationId: suggestion.id,
      medicationName: suggestion.name,
    });
    this.medicationSuggestions = [];
  }

  reportLabel(report: PrescriptionFormMedicalReportDto): string {
    const date = new Date(report.createdAt).toLocaleDateString('bs-BA');
    const doctor = `Dr. ${report.doctorFirstName} ${report.doctorLastName}`;
    const diagnosis = report.diagnosis ? ` — ${report.diagnosis}` : '';
    return `${date} · ${doctor}${diagnosis}`;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toaster.error('Popunite sva obavezna polja.');
      return;
    }

    const value = this.form.getRawValue();
    const body: CreatePrescriptionRequest = {
      patientId: value.patientId!,
      medicalReportId: value.medicalReportId!,
      notes: value.notes?.trim() || null,
      prescriptionItems: value.items.map((item) => ({
        medicationId: item.medicationId!,
        quantity: item.quantity!,
        instructions: item.instructions?.trim() || null,
      })),
    };

    this.isSaving = true;
    this.pharmacyApi
      .createPrescription(body)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.isSaving = false;
          this.toaster.success('Recept uspješno kreiran.');
          this.router.navigate(['/pharmacy/prescriptions', created.id]);
        },
        error: (err) => {
          this.isSaving = false;
          const msg = err?.error?.message ?? err?.error?.title ?? 'Greška pri kreiranju recepta.';
          this.toaster.error(msg);
        },
      });
  }

  cancel(): void {
    this.router.navigate(['/pharmacy/prescriptions']);
  }
}
