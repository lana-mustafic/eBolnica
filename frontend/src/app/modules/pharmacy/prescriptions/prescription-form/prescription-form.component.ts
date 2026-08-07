import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
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
import { getApiErrorCode, getApiErrorMessage } from '../../../../core/utils/api-error.util';

@Component({
  selector: 'app-prescription-form',
  standalone: false,
  templateUrl: './prescription-form.component.html',
  styleUrl: './prescription-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
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
  patients = signal<PrescriptionFormPatientDto[]>([]);
  selectedPatient = signal<PrescriptionFormPatientDto | null>(null);
  medicalReports = signal<PrescriptionFormMedicalReportDto[]>([]);
  medicationSuggestions = signal<MedicationAutocompleteSuggestion[]>([]);
  activeItemIndex = signal(0);
  isSaving = signal(false);
  isLoadingReports = signal(false);

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
        this.patients.set(patients);
      });

    this.medicationSearch$
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap((term) => this.pharmacyApi.getAutocomplete(term, 10, true)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((suggestions) => {
        this.medicationSuggestions.set(suggestions);
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
    this.selectedPatient.set(patient);
    this.form.patchValue({ patientId: patient.id, medicalReportId: null });
    this.patients.set([]);
    this.patientSearch = `${patient.firstName} ${patient.lastName}`;
    this.loadMedicalReports(patient.id);
  }

  loadMedicalReports(patientId: number): void {
    this.isLoadingReports.set(true);
    this.medicalReports.set([]);
    this.pharmacyApi
      .listPatientMedicalReportsForPrescription(patientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (reports) => {
          this.medicalReports.set(reports);
          this.isLoadingReports.set(false);
          if (reports.length === 1) {
            this.form.patchValue({ medicalReportId: reports[0].id });
          }
        },
        error: () => {
          this.isLoadingReports.set(false);
          this.toaster.error('Greška pri učitavanju medicinskih izvještaja.');
        },
      });
  }

  onMedicationSearch(term: string, index: number): void {
    this.activeItemIndex.set(index);
    const group = this.items.at(index);
    group.patchValue({ medicationId: null });
    this.clearMedicationFieldError(group);

    if (term.trim().length < 2) {
      this.medicationSuggestions.set([]);
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
    this.clearMedicationFieldError(group);
    this.medicationSuggestions.set([]);
  }

  medicationFieldError(index: number): string | null {
    const group = this.items.at(index);
    const control = group.get('medicationName');
    if (control?.hasError('otc')) {
      return 'Odabrani lijek ne zahtijeva recept (OTC). Samo lijekovi na recept su dozvoljeni.';
    }
    if (control?.hasError('required') && control.touched) {
      return 'Odaberite lijek s liste (samo Rx).';
    }
    if (!group.get('medicationId')?.value && control?.touched && control.value?.trim()) {
      return 'Odaberite lijek s liste (samo Rx).';
    }
    return null;
  }

  private clearMedicationFieldError(group: ReturnType<FormArray['at']>): void {
    const control = group.get('medicationName');
    if (!control?.errors) {
      return;
    }

    const { otc: _otc, ...remaining } = control.errors;
    control.setErrors(Object.keys(remaining).length > 0 ? remaining : null);
  }

  private markMedicationOtcErrors(): void {
    this.items.controls.forEach((group) => {
      group.get('medicationName')?.setErrors({ otc: true });
      group.get('medicationName')?.markAsTouched();
    });
  }

  reportLabel(report: PrescriptionFormMedicalReportDto): string {
    const date = new Date(report.createdAt).toLocaleDateString('bs-BA');
    const doctor = `Dr. ${report.doctorFirstName} ${report.doctorLastName}`;
    const diagnosis = report.diagnosis ? ` — ${report.diagnosis}` : '';
    return `${date} · ${doctor}${diagnosis}`;
  }

  submit(): void {
    this.items.controls.forEach((group) => {
      if (!group.get('medicationId')?.value) {
        group.get('medicationName')?.setErrors({ required: true });
      }
    });

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

    this.isSaving.set(true);
    this.pharmacyApi
      .createPrescription(body)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.isSaving.set(false);
          this.toaster.success('Recept uspješno kreiran.');
          this.router.navigate(['/pharmacy/prescriptions', created.id]);
        },
        error: (err) => {
          this.isSaving.set(false);
          if (getApiErrorCode(err) === 'prescription.medication_otc') {
            this.markMedicationOtcErrors();
          }
          this.toaster.error(getApiErrorMessage(err, 'Greška pri kreiranju recepta.'));
        },
      });
  }

  cancel(): void {
    this.router.navigate(['/pharmacy/prescriptions']);
  }
}
