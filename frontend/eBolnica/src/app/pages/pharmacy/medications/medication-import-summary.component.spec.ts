import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MedicationImportSummaryComponent } from './medication-import-summary.component';
import { MedicationImportSummary } from '../../../models/medication-import.dto';

describe('MedicationImportSummaryComponent', () => {
  let component: MedicationImportSummaryComponent;
  let fixture: ComponentFixture<MedicationImportSummaryComponent>;

  const partialSummary: MedicationImportSummary = {
    successCount: 2,
    failureCount: 1,
    totalRows: 3,
    committed: true,
    importedMedicationIds: [101, 102],
    errors: [
      { rowNumber: 4, field: 'Price', value: '-5', reason: 'Price must be greater than 0.' }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MedicationImportSummaryComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationImportSummaryComponent);
    component = fixture.componentInstance;
    component.summary = partialSummary;
    fixture.detectChanges();
  });

  it('should show success and failure counts', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('2');
    expect(element.textContent).toContain('Imported');
    expect(element.textContent).toContain('Failed');
  });

  it('should render error table rows', () => {
    const rows = fixture.nativeElement.querySelectorAll('.import-errors-table tbody tr');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('4');
    expect(rows[0].textContent).toContain('Price must be greater than 0.');
  });
});
