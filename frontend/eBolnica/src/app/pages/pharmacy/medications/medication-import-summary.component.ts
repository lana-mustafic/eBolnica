import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MedicationImportSummary } from '../../../models/medication-import.dto';

@Component({
  selector: 'app-medication-import-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './medication-import-summary.component.html',
  styleUrl: './medication-import-summary.component.css'
})
export class MedicationImportSummaryComponent {
  @Input({ required: true }) summary!: MedicationImportSummary;

  get hasErrors(): boolean {
    return this.summary.failureCount > 0 && this.summary.errors.length > 0;
  }

  get allSucceeded(): boolean {
    return this.summary.failureCount === 0 && this.summary.successCount > 0;
  }

  get allFailed(): boolean {
    return this.summary.successCount === 0 && this.summary.failureCount > 0;
  }

  trackByRowNumber(_: number, error: { rowNumber: number }): number {
    return error.rowNumber;
  }
}
