import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface MedicationImageLightboxData {
  imageUrl: string;
  fileName: string;
}

@Component({
  selector: 'app-medication-image-lightbox',
  standalone: false,
  templateUrl: './medication-image-lightbox.component.html',
  styleUrl: './medication-image-lightbox.component.scss',
})
export class MedicationImageLightboxComponent {
  data = inject<MedicationImageLightboxData>(MAT_DIALOG_DATA);
  private dialogRef = inject(MatDialogRef<MedicationImageLightboxComponent>);

  close(): void {
    this.dialogRef.close();
  }
}
