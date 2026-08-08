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
import { catchError, debounceTime, distinctUntilChanged, of, Subject, switchMap, tap } from 'rxjs';
import { PharmacyApiService } from '../../../../api-services/pharmacy/pharmacy-api.service';
import {
  CreatePrescriptionRequest,
  MedicationAutocompleteSuggestion,
  PrescriptionFormMedicalReportDto,
  PrescriptionFormPatientDto,
} from '../../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';
import { isPharmacyErrorCode, resolvePharmacyApiErrorMessage } from '../../shared/utils/pharmacy-api-error.util';
import { getMedicationCategoryLabel } from '../../constants/medication-categories.constant';

/** Prescription items may only include medications that require a prescription (Rx). */
const PRESCRIPTION_AUTOCOMPLETE_RX_ONLY = true;

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
  showMedicationAutocomplete = signal(false);
  isMedicationAutocompleteLoading = signal(false);
  medicationAutocompleteEmpty = signal(false);
  selectedMedicationSuggestionIndex = signal(-1);
  activeMedicationSuggestionId = signal<string | null>(null);
  activeItemIndex = signal(0);
  isSaving = signal(false);
  isLoadingReports = signal(false);

  private patientSearch$ = new Subject<string>();
  private medicationSearch$ = new Subject<string>();
  private medicationSearchBlurTimeoutId?: ReturnType<typeof setTimeout>;

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
        tap(() => {
          this.isMedicationAutocompleteLoading.set(true);
          this.showMedicationAutocomplete.set(true);
          this.medicationAutocompleteEmpty.set(false);
        }),
        switchMap((term) =>
          this.pharmacyApi
            .getAutocomplete(term, 10, PRESCRIPTION_AUTOCOMPLETE_RX_ONLY)
            .pipe(catchError(() => of([] as MedicationAutocompleteSuggestion[])))
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((suggestions) => {
        const rxOnlySuggestions = suggestions.filter(
          (suggestion) => suggestion.requiresPrescription !== false
        );

        this.isMedicationAutocompleteLoading.set(false);
        this.medicationSuggestions.set(rxOnlySuggestions);
        this.showMedicationAutocomplete.set(true);
        this.medicationAutocompleteEmpty.set(rxOnlySuggestions.length === 0);
        this.selectedMedicationSuggestionIndex.set(rxOnlySuggestions.length > 0 ? 0 : -1);
        this.activeMedicationSuggestionId.set(
          rxOnlySuggestions.length > 0 ? 'rx-medication-suggestion-0' : null
        );
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
    if (this.activeItemIndex() === index) {
      this.closeMedicationAutocomplete();
    }
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

    const trimmed = term.trim();
    if (trimmed.length < 2) {
      this.closeMedicationAutocomplete();
      return;
    }

    this.medicationSearch$.next(trimmed);
  }

  onMedicationSearchFocus(index: number): void {
    this.activeItemIndex.set(index);
    const term = this.items.at(index).get('medicationName')?.value?.trim() ?? '';
    if (term.length >= 2 && this.medicationSuggestions().length > 0) {
      this.showMedicationAutocomplete.set(true);
    }
  }

  onMedicationSearchBlur(): void {
    this.clearMedicationSearchBlurTimeout();
    this.medicationSearchBlurTimeoutId = window.setTimeout(() => {
      this.closeMedicationAutocomplete();
      this.medicationSearchBlurTimeoutId = undefined;
    }, 150);
  }

  onMedicationSearchKeydown(event: KeyboardEvent, index: number): void {
    if (this.activeItemIndex() !== index || !this.showMedicationAutocomplete()) {
      return;
    }

    const suggestions = this.medicationSuggestions();
    if (this.isMedicationAutocompleteLoading() || suggestions.length === 0) {
      if (event.key === 'Escape') {
        this.closeMedicationAutocomplete();
      }
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      const nextIndex = Math.min(this.selectedMedicationSuggestionIndex() + 1, suggestions.length - 1);
      this.selectedMedicationSuggestionIndex.set(nextIndex);
      this.activeMedicationSuggestionId.set(`rx-medication-suggestion-${nextIndex}`);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      const nextIndex = Math.max(this.selectedMedicationSuggestionIndex() - 1, 0);
      this.selectedMedicationSuggestionIndex.set(nextIndex);
      this.activeMedicationSuggestionId.set(`rx-medication-suggestion-${nextIndex}`);
    } else if (event.key === 'Enter' && this.selectedMedicationSuggestionIndex() >= 0) {
      event.preventDefault();
      this.selectMedication(suggestions[this.selectedMedicationSuggestionIndex()], index);
    } else if (event.key === 'Escape') {
      this.closeMedicationAutocomplete();
    }
  }

  selectMedication(suggestion: MedicationAutocompleteSuggestion, index: number): void {
    if (suggestion.requiresPrescription === false) {
      const group = this.items.at(index);
      group.get('medicationName')?.setErrors({ otc: true });
      group.get('medicationName')?.markAsTouched();
      this.closeMedicationAutocomplete();
      return;
    }

    const group = this.items.at(index);
    group.patchValue({
      medicationId: suggestion.id,
      medicationName: suggestion.name,
    });
    this.clearMedicationFieldError(group);
    this.closeMedicationAutocomplete();
  }

  medicationSuggestionMeta(suggestion: MedicationAutocompleteSuggestion): string {
    const parts = [getMedicationCategoryLabel(suggestion.category)];
    if (suggestion.manufacturer) {
      parts.push(suggestion.manufacturer);
    }
    return parts.filter(Boolean).join(' · ');
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

  private closeMedicationAutocomplete(): void {
    this.showMedicationAutocomplete.set(false);
    this.isMedicationAutocompleteLoading.set(false);
    this.medicationAutocompleteEmpty.set(false);
    this.medicationSuggestions.set([]);
    this.selectedMedicationSuggestionIndex.set(-1);
    this.activeMedicationSuggestionId.set(null);
  }

  private clearMedicationSearchBlurTimeout(): void {
    if (this.medicationSearchBlurTimeoutId != null) {
      clearTimeout(this.medicationSearchBlurTimeoutId);
      this.medicationSearchBlurTimeoutId = undefined;
    }
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
          if (isPharmacyErrorCode(err, 'prescription.medication_otc')) {
            this.markMedicationOtcErrors();
          }
          this.toaster.error(resolvePharmacyApiErrorMessage(err, 'Greška pri kreiranju recepta.'));
        },
      });
  }

  cancel(): void {
    this.router.navigate(['/pharmacy/prescriptions']);
  }
}
