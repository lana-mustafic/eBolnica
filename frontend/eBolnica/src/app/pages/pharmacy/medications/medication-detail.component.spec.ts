import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, NEVER } from 'rxjs';
import { MedicationDetailComponent } from './medication-detail.component';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { MedicationDto } from '../../../models/medication.dto';
import { MedicationAiSummaryDto } from '../../../models/medication-ai-summary.dto';

describe('MedicationDetailComponent AI summary states', () => {
  let fixture: ComponentFixture<MedicationDetailComponent>;
  let component: MedicationDetailComponent;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  const medication: MedicationDto = {
    id: 10,
    name: 'Aspirin',
    price: 8.5,
    stockQuantity: 100,
    minimumStockLevel: 20,
    expiryDate: '2027-01-01',
    isActive: true,
    requiresPrescription: false,
    category: 'painkiller',
    description: 'Pain relief medication'
  };

  const summary: MedicationAiSummaryDto = {
    overview: 'Aspirin overview',
    usageNotes: 'Use as directed',
    stockExpiryAlert: 'Stock is healthy',
    prescriptionRequirement: 'No prescription required'
  };

  beforeEach(async () => {
    pharmacyService = jasmine.createSpyObj<PharmacyService>('PharmacyService', [
      'getMedicationById',
      'generateMedicationAiSummary'
    ]);
    pharmacyService.getMedicationById.and.returnValue(of(medication));
    pharmacyService.generateMedicationAiSummary.and.returnValue(of(summary));

    await TestBed.configureTestingModule({
      imports: [MedicationDetailComponent],
      providers: [
        { provide: PharmacyService, useValue: pharmacyService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => '10' } } }
        },
        {
          provide: Router,
          useValue: jasmine.createSpyObj<Router>('Router', ['navigate'])
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('starts in idle AI summary state', () => {
    expect(component.aiSummaryState).toBe('idle');
    expect(fixture.nativeElement.querySelector('.ai-summary-disclaimer-label')?.textContent?.trim())
      .toBe('AI-generated summary');
  });

  it('shows loading state while summary is generating', () => {
    pharmacyService.generateMedicationAiSummary.and.returnValue(NEVER);

    component.onGenerateAiSummary();

    expect(component.aiSummaryState).toBe('loading');
    expect(component.isGeneratingAiSummary).toBeTrue();
  });

  it('shows success state after summary is generated', () => {
    component.onGenerateAiSummary();
    fixture.detectChanges();

    expect(component.aiSummaryState).toBe('success');
    expect(component.aiSummary).toEqual(summary);
    expect(fixture.nativeElement.querySelector('.ai-summary-success')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.ai-summary-result')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.ai-summary-disclaimer-label')?.textContent?.trim())
      .toBe('AI-generated summary');
  });

  it('shows friendly error state when summary generation fails', () => {
    pharmacyService.generateMedicationAiSummary.and.returnValue(
      throwError(() => ({ status: 503 }))
    );

    component.onGenerateAiSummary();
    fixture.detectChanges();

    expect(component.aiSummaryState).toBe('error');
    expect(component.aiSummaryError).toContain('temporarily unavailable');
    expect(fixture.nativeElement.querySelector('.ai-summary-error')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.ai-summary-result')).toBeFalsy();
  });
});
