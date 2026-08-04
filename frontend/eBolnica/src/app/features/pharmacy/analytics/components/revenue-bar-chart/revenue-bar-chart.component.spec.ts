import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { RevenueBarChartComponent } from './revenue-bar-chart.component';
import { PharmacyService } from '../../../../../shared/services/pharmacy/pharmacy.service';
import { MonthlyRevenueData } from '../../../../../models/analytics.dto';

describe('RevenueBarChartComponent', () => {
  let component: RevenueBarChartComponent;
  let fixture: ComponentFixture<RevenueBarChartComponent>;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;
  let revenueSubject: Subject<MonthlyRevenueData[]>;

  beforeEach(async () => {
    revenueSubject = new Subject<MonthlyRevenueData[]>();
    pharmacyService = jasmine.createSpyObj('PharmacyService', [
      'getMonthlyRevenue',
      'clearAnalyticsCache'
    ]);
    pharmacyService.getMonthlyRevenue.and.returnValue(revenueSubject.asObservable());

    await TestBed.configureTestingModule({
      imports: [RevenueBarChartComponent],
      providers: [
        provideCharts(withDefaultRegisterables()),
        { provide: PharmacyService, useValue: pharmacyService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RevenueBarChartComponent);
    component = fixture.componentInstance;
  });

  it('should show loading state before data arrives', () => {
    fixture.detectChanges();

    expect(component.isLoading).toBeTrue();
    expect(fixture.nativeElement.querySelector('.loading-state')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.empty-state')).toBeFalsy();
  });

  it('should show empty state when API returns no rows', () => {
    pharmacyService.getMonthlyRevenue.and.returnValue(of([]));
    fixture.detectChanges();

    expect(component.isLoading).toBeFalse();
    expect(component.showEmptyState).toBeTrue();
    expect(fixture.nativeElement.querySelector('.empty-state')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.chart-wrapper')).toBeFalsy();
  });

  it('should render chart when revenue data exists', () => {
    pharmacyService.getMonthlyRevenue.and.returnValue(of([
      { month: 'January', revenue: 1200 }
    ]));
    fixture.detectChanges();

    expect(component.hasData).toBeTrue();
    expect(fixture.nativeElement.querySelector('.chart-wrapper')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.empty-state')).toBeFalsy();
  });
});
