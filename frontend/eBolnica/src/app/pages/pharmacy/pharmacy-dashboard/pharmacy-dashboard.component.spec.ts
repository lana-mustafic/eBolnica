import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { PharmacyDashboardComponent } from './pharmacy-dashboard.component';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { AuthService } from '../../../shared/services/auth.service';

describe('PharmacyDashboardComponent', () => {
  let component: PharmacyDashboardComponent;
  let fixture: ComponentFixture<PharmacyDashboardComponent>;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  beforeEach(async () => {
    pharmacyService = jasmine.createSpyObj('PharmacyService', [
      'getDashboardSummaryMetrics',
      'getPrescriptions',
      'clearAnalyticsCache',
      'getMonthlyRevenue',
      'getTopMedicationCategories',
      'getStockTrends'
    ]);
    pharmacyService.getDashboardSummaryMetrics.and.returnValue(of({
      totalMedications: 42,
      pendingPrescriptions: 3,
      lowStockAlerts: 5,
      expiringSoon: 2
    }));
    pharmacyService.getPrescriptions.and.returnValue(of({
      items: [],
      totalCount: 0,
      currentPage: 1,
      pageSize: 5
    }));
    pharmacyService.getMonthlyRevenue.and.returnValue(of([]));
    pharmacyService.getTopMedicationCategories.and.returnValue(of([]));
    pharmacyService.getStockTrends.and.returnValue(of([]));
    pharmacyService.clearAnalyticsCache.and.stub();

    await TestBed.configureTestingModule({
      imports: [PharmacyDashboardComponent],
      providers: [
        provideCharts(withDefaultRegisterables()),
        { provide: PharmacyService, useValue: pharmacyService },
        {
          provide: AuthService,
          useValue: { userLoggedInfo: () => 'Test User', logout: jasmine.createSpy('logout') }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PharmacyDashboardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should load summary metrics from dashboard-stats API', () => {
    fixture.detectChanges();

    expect(pharmacyService.getDashboardSummaryMetrics).toHaveBeenCalled();
    expect(component.totalMedications).toBe(42);
    expect(component.pendingPrescriptions).toBe(3);
    expect(component.lowStockAlerts).toBe(5);
    expect(component.expiringSoon).toBe(2);
  });

  it('should show analytics banner when summary API fails but keep dashboard visible', () => {
    pharmacyService.getDashboardSummaryMetrics.and.returnValue(
      throwError(() => new Error('API error'))
    );

    fixture.detectChanges();

    expect(component.errorMessage).toBeNull();
    expect(component.hasAnalyticsErrors).toBeTrue();
    expect(component.analyticsErrorMessage).toContain('summary metrics');
    expect(fixture.nativeElement.querySelector('.analytics-error-banner')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.dashboard-main')).toBeTruthy();
  });

  it('should show analytics banner when a chart reports an error', () => {
    fixture.detectChanges();

    component.onAnalyticsError('revenue', new Error('Revenue API failed'));
    fixture.detectChanges();

    expect(component.hasAnalyticsErrors).toBeTrue();
    expect(component.analyticsErrorMessage).toContain('monthly revenue');
    expect(fixture.nativeElement.querySelector('.analytics-error-banner')).toBeTruthy();
  });

  it('should clear analytics banner when chart loads successfully', () => {
    fixture.detectChanges();
    component.onAnalyticsError('categories', new Error('Categories API failed'));
    fixture.detectChanges();

    component.onAnalyticsLoaded('categories');
    fixture.detectChanges();

    expect(component.hasAnalyticsErrors).toBeFalse();
    expect(fixture.nativeElement.querySelector('.analytics-error-banner')).toBeFalsy();
  });

  it('should show fatal error when prescriptions API fails', () => {
    pharmacyService.getPrescriptions.and.returnValue(
      throwError(() => new Error('Prescriptions API error'))
    );

    fixture.detectChanges();

    expect(component.errorMessage).toContain('Failed to load dashboard data');
    expect(fixture.nativeElement.querySelector('.dashboard-main')).toBeFalsy();
  });

  it('should retry analytics and clear cache', () => {
    fixture.detectChanges();
    component.onAnalyticsError('stock', new Error('Stock API failed'));

    component.retryAnalytics();

    expect(pharmacyService.clearAnalyticsCache).toHaveBeenCalled();
    expect(pharmacyService.getDashboardSummaryMetrics).toHaveBeenCalledTimes(2);
  });
});
