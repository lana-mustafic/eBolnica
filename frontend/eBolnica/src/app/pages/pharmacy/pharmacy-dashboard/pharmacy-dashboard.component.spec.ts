import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
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
      'getPrescriptions'
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

    await TestBed.configureTestingModule({
      imports: [PharmacyDashboardComponent],
      providers: [
        { provide: PharmacyService, useValue: pharmacyService },
        {
          provide: AuthService,
          useValue: { userLoggedInfo: () => 'Test User', logout: jasmine.createSpy('logout') }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PharmacyDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load summary metrics from dashboard-stats API', () => {
    expect(pharmacyService.getDashboardSummaryMetrics).toHaveBeenCalled();
    expect(component.totalMedications).toBe(42);
    expect(component.pendingPrescriptions).toBe(3);
    expect(component.lowStockAlerts).toBe(5);
    expect(component.expiringSoon).toBe(2);
  });

  it('should show error when summary API fails', () => {
    pharmacyService.getDashboardSummaryMetrics.and.returnValue(
      throwError(() => new Error('API error'))
    );

    component.loadDashboardData();
    fixture.detectChanges();

    expect(component.errorMessage).toContain('Failed to load dashboard data');
  });
});
