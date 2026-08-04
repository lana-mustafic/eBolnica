import { Location } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MedicalRecordApiService } from '../../../api-services/doctor/medical-record-api.service';
import { MedicalRecordDto } from '../../../api-services/doctor/medical-record-api.models';
import { ToasterService } from '../../../core/services/toaster.service';

@Component({
  selector: 'app-medical-record',
  standalone: false,
  templateUrl: './medical-record.component.html',
  styleUrl: './medical-record.component.scss',
})
export class MedicalRecordComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private medicalRecordApi = inject(MedicalRecordApiService);
  private fb = inject(FormBuilder);
  private location = inject(Location);
  private toaster = inject(ToasterService);

  patientId!: number;
  record: MedicalRecordDto | null = null;
  isLoading = true;
  isSaving = false;

  reportForm = this.fb.group({
    symptoms: ['', Validators.required],
    diagnosis: ['', Validators.required],
    therapy: ['', Validators.required],
    description: [''],
  });

  ngOnInit(): void {
    this.patientId = Number(this.route.snapshot.paramMap.get('patientId'));
    this.loadRecord();
  }

  loadRecord(): void {
    this.isLoading = true;
    this.medicalRecordApi.getByPatientId(this.patientId).subscribe({
      next: (data) => {
        this.record = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toaster.error('Greška pri učitavanju kartona.');
      },
    });
  }

  goBack(): void {
    this.location.back();
  }

  submitReport(): void {
    if (this.reportForm.invalid || !this.record) return;

    this.isSaving = true;
    const value = this.reportForm.getRawValue();

    this.medicalRecordApi
      .createReport({
        medicalRecordId: this.record.id,
        symptoms: value.symptoms!,
        diagnosis: value.diagnosis!,
        therapy: value.therapy!,
        description: value.description,
      })
      .subscribe({
        next: () => {
          this.toaster.success('Izvještaj sačuvan.');
          this.reportForm.reset();
          this.isSaving = false;
          this.loadRecord();
        },
        error: () => {
          this.isSaving = false;
          this.toaster.error('Greška pri čuvanju izvještaja.');
        },
      });
  }
}
