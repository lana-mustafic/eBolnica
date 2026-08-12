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
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, of, Subject, switchMap, tap } from 'rxjs';
import { DoctorApiService } from '../../../api-services/doctor/doctor-api.service';
import { MedicationAutocompleteSuggestion } from '../../../api-services/pharmacy/pharmacy-api.models';
import { ToasterService } from '../../../core/services/toaster.service';
import { getApiErrorMessage } from '../../../core/utils/api-error.util';
import { getMedicationCategoryLabel } from '../../pharmacy/constants/medication-categories.constant';

@Component({
  selector: 'app-doctor-prescription-form',
  standalone: false,
  templateUrl: './doctor-prescription-form.component.html',
  styleUrl: './doctor-prescription-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DoctorPrescriptionFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private doctorApi = inject(DoctorApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private destroyRef = inject(DestroyRef);

  patientId = signal<number | null>(null);
  medicalReportId = signal<number | null>(null);
  patientLabel = signal('');
  reportLabel = signal('');
  isSaving = signal(false);

  medicationSuggestions = signal<MedicationAutocompleteSuggestion[]>([]);
  showMedicationAutocomplete = signal(false);
  isMedicationAutocompleteLoading = signal(false);
  medicationAutocompleteEmpty = signal(false);
  selectedMedicationSuggestionIndex = signal(-1);
  activeItemIndex = signal(0);

  form = this.fb.group({
    notes: this.fb.control(''),
    items: this.fb.array([this.createItemGroup()]),
  });

  private medicationSearch$ = new Subject<string>();
  private medicationSearchBlurTimeoutId?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    const patientId = Number(this.route.snapshot.queryParamMap.get('patientId'));
    const medicalReportId = Number(this.route.snapshot.queryParamMap.get('medicalReportId'));
    const patientName = this.route.snapshot.queryParamMap.get('patientName') ?? '';
    const reportSummary = this.route.snapshot.queryParamMap.get('reportSummary') ?? '';

    if (!patientId || !medicalReportId) {
      this.toaster.error('Nedostaju podaci pacijenta ili medicinskog izvještaja.');
      void this.router.navigate(['/doctor/prescriptions']);
      return;
    }

    this.patientId.set(patientId);
    this.medicalReportId.set(medicalReportId);
    this.patientLabel.set(patientName);
    this.reportLabel.set(reportSummary);

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
          this.doctorApi.getMedicationAutocomplete(term, 10).pipe(
            catchError(() => of([] as MedicationAutocompleteSuggestion[]))
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((suggestions) => {
        this.isMedicationAutocompleteLoading.set(false);
        this.medicationSuggestions.set(suggestions);
        this.showMedicationAutocomplete.set(true);
        this.medicationAutocompleteEmpty.set(suggestions.length === 0);
        this.selectedMedicationSuggestionIndex.set(suggestions.length > 0 ? 0 : -1);
      });
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
    if (this.items.length <= 1) return;
    this.items.removeAt(index);
  }

  onMedicationSearch(term: string, index: number): void {
    this.activeItemIndex.set(index);
    this.items.at(index).patchValue({ medicationId: null });
    const trimmed = term.trim();
    if (trimmed.length < 2) {
      this.closeMedicationAutocomplete();
      return;
    }
    this.medicationSearch$.next(trimmed);
  }

  onMedicationSearchBlur(): void {
    if (this.medicationSearchBlurTimeoutId != null) {
      clearTimeout(this.medicationSearchBlurTimeoutId);
    }
    this.medicationSearchBlurTimeoutId = window.setTimeout(() => {
      this.closeMedicationAutocomplete();
      this.medicationSearchBlurTimeoutId = undefined;
    }, 150);
  }

  selectMedication(suggestion: MedicationAutocompleteSuggestion, index: number): void {
    this.items.at(index).patchValue({
      medicationId: suggestion.id,
      medicationName: suggestion.name,
    });
    this.closeMedicationAutocomplete();
  }

  medicationSuggestionMeta(suggestion: MedicationAutocompleteSuggestion): string {
    const parts = [getMedicationCategoryLabel(suggestion.category)];
    if (suggestion.manufacturer) parts.push(suggestion.manufacturer);
    return parts.filter(Boolean).join(' · ');
  }

  submit(): void {
    const patientId = this.patientId();
    const medicalReportId = this.medicalReportId();
    if (!patientId || !medicalReportId) return;

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
    this.isSaving.set(true);
    this.doctorApi
      .createPrescription({
        patientId,
        medicalReportId,
        notes: value.notes?.trim() || null,
        prescriptionItems: value.items.map((item) => ({
          medicationId: item.medicationId!,
          quantity: item.quantity!,
          instructions: item.instructions?.trim() || null,
        })),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.isSaving.set(false);
          this.toaster.success('Recept poslan apoteci.');
          void this.router.navigate(['/doctor/prescriptions', created.id]);
        },
        error: (err) => {
          this.isSaving.set(false);
          this.toaster.error(getApiErrorMessage(err, 'Greška pri kreiranju recepta.'));
        },
      });
  }

  cancel(): void {
    void this.router.navigate(['/doctor/prescriptions']);
  }

  private closeMedicationAutocomplete(): void {
    this.showMedicationAutocomplete.set(false);
    this.isMedicationAutocompleteLoading.set(false);
    this.medicationAutocompleteEmpty.set(false);
    this.medicationSuggestions.set([]);
    this.selectedMedicationSuggestionIndex.set(-1);
  }
}
