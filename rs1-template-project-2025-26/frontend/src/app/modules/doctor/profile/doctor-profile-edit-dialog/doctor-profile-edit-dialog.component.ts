import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DoctorApiService } from '../../../../api-services/doctor/doctor-api.service';
import { DoctorProfileDto } from '../../../../api-services/doctor/doctor-api.models';
import { ToasterService } from '../../../../core/services/toaster.service';

@Component({
  selector: 'app-doctor-profile-edit-dialog',
  standalone: false,
  templateUrl: './doctor-profile-edit-dialog.component.html',
})
export class DoctorProfileEditDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private doctorApi = inject(DoctorApiService);
  private toaster = inject(ToasterService);
  private dialogRef = inject(MatDialogRef<DoctorProfileEditDialogComponent>);
  doctorData = inject<DoctorProfileDto>(MAT_DIALOG_DATA);

  isSaving = false;

  form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    phoneNumber: ['', Validators.maxLength(30)],
    specialization: ['', Validators.maxLength(100)],
    address: ['', Validators.maxLength(300)],
  });

  ngOnInit(): void {
    this.form.patchValue({
      firstName: this.doctorData.firstName,
      lastName: this.doctorData.lastName,
      phoneNumber: this.doctorData.phoneNumber,
      specialization: this.doctorData.specialization ?? '',
      address: this.doctorData.address,
    });
  }

  save(): void {
    if (this.form.invalid) return;

    this.isSaving = true;
    this.doctorApi.updateProfile(this.form.getRawValue() as any).subscribe({
      next: () => {
        this.toaster.success('Profil ažuriran.');
        this.dialogRef.close(true);
      },
      error: () => {
        this.isSaving = false;
        this.toaster.error('Greška pri ažuriranju profila.');
      },
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
