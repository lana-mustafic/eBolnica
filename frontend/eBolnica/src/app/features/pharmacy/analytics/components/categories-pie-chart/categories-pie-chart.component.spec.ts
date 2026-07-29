import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { CategoriesPieChartComponent } from './categories-pie-chart.component';
import { PharmacyService } from '../../../../../shared/services/pharmacy/pharmacy.service';
import { MedicationCategoryData } from '../../../../../models/analytics.dto';

describe('CategoriesPieChartComponent', () => {
  let component: CategoriesPieChartComponent;
  let fixture: ComponentFixture<CategoriesPieChartComponent>;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  beforeEach(async () => {
    pharmacyService = jasmine.createSpyObj('PharmacyService', [
      'getTopMedicationCategories',
      'clearAnalyticsCache'
    ]);
    pharmacyService.getTopMedicationCategories.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [CategoriesPieChartComponent],
      providers: [
        provideCharts(withDefaultRegisterables()),
        { provide: PharmacyService, useValue: pharmacyService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CategoriesPieChartComponent);
    component = fixture.componentInstance;
  });

  it('should show loading state before data arrives', () => {
    pharmacyService.getTopMedicationCategories.and.returnValue(new Subject<MedicationCategoryData[]>().asObservable());
    fixture.detectChanges();

    expect(component.isLoading).toBeTrue();
    expect(fixture.nativeElement.querySelector('.loading-state')).toBeTruthy();
  });

  it('should show empty state when no categories are returned', () => {
    fixture.detectChanges();

    expect(component.showEmptyState).toBeTrue();
    expect(fixture.nativeElement.querySelector('.empty-state')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.chart-wrapper')).toBeFalsy();
  });

  it('should show insufficient state when only one category is returned', () => {
    const oneCategory: MedicationCategoryData[] = [
      { category: 'Antibiotics', count: 5, percentage: 100 }
    ];
    pharmacyService.getTopMedicationCategories.and.returnValue(of(oneCategory));
    fixture.detectChanges();

    expect(component.showInsufficientState).toBeTrue();
    expect(fixture.nativeElement.querySelector('.insufficient-state')).toBeTruthy();
  });

  it('should render chart when at least two categories exist', () => {
    pharmacyService.getTopMedicationCategories.and.returnValue(of([
      { category: 'Antibiotics', count: 5, percentage: 60 },
      { category: 'Pain Relief', count: 3, percentage: 40 }
    ]));
    fixture.detectChanges();

    expect(component.hasData).toBeTrue();
    expect(fixture.nativeElement.querySelector('.chart-wrapper')).toBeTruthy();
  });
});
