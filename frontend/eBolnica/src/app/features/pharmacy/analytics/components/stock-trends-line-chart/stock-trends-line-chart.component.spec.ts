import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { StockTrendsLineChartComponent } from './stock-trends-line-chart.component';
import { PharmacyService } from '../../../../../shared/services/pharmacy/pharmacy.service';
import { StockTrendData } from '../../../../../models/analytics.dto';

describe('StockTrendsLineChartComponent', () => {
  let component: StockTrendsLineChartComponent;
  let fixture: ComponentFixture<StockTrendsLineChartComponent>;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  beforeEach(async () => {
    pharmacyService = jasmine.createSpyObj('PharmacyService', [
      'getStockTrends',
      'clearAnalyticsCache'
    ]);
    pharmacyService.getStockTrends.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [StockTrendsLineChartComponent],
      providers: [
        provideCharts(withDefaultRegisterables()),
        { provide: PharmacyService, useValue: pharmacyService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StockTrendsLineChartComponent);
    component = fixture.componentInstance;
  });

  it('should show loading state before data arrives', () => {
    pharmacyService.getStockTrends.and.returnValue(new Subject<StockTrendData[]>().asObservable());
    fixture.detectChanges();

    expect(component.isLoading).toBeTrue();
    expect(fixture.nativeElement.querySelector('.loading-state')).toBeTruthy();
  });

  it('should show empty state when no medications are available', () => {
    fixture.detectChanges();

    expect(component.showEmptyState).toBeTrue();
    expect(component.emptyStateMessage).toContain('No medications available');
    expect(fixture.nativeElement.querySelector('.empty-state')).toBeTruthy();
  });

  it('should show selection empty state when data exists but nothing is selected', () => {
    const trends: StockTrendData[] = [
      {
        date: '2026-01-01',
        medicationId: 1,
        medicationName: 'Paracetamol',
        stockLevel: 55
      },
      {
        date: '2026-01-01',
        medicationId: 2,
        medicationName: 'Ibuprofen',
        stockLevel: 70
      }
    ];
    pharmacyService.getStockTrends.and.returnValue(of(trends));

    fixture.detectChanges();
    component.selectedMedicationIds = [];
    component.hasData = true;
    fixture.detectChanges();

    expect(component.showEmptyState).toBeTrue();
    expect(component.emptyStateMessage).toContain('Select at least one medication');
  });

  it('should render chart when data and selections exist', () => {
    pharmacyService.getStockTrends.and.returnValue(of([
      {
        date: '2026-01-01',
        medicationId: 1,
        medicationName: 'Paracetamol',
        stockLevel: 55
      },
      {
        date: '2026-01-02',
        medicationId: 1,
        medicationName: 'Paracetamol',
        stockLevel: 55
      }
    ]));
    fixture.detectChanges();

    expect(component.showChart).toBeTrue();
    expect(fixture.nativeElement.querySelector('.chart-wrapper')).toBeTruthy();
  });
});
