import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { MedicationsComponent } from './medications.component';
import { PharmacyService } from '../../../shared/services/pharmacy/pharmacy.service';
import { PharmacyFilterService } from '../../../shared/services/pharmacy/pharmacy-filter.service';

describe('MedicationsComponent autocomplete selection', () => {
  let component: MedicationsComponent;
  let fixture: ComponentFixture<MedicationsComponent>;
  let filterService: PharmacyFilterService;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  const emptyPagedResponse = {
    items: [],
    totalCount: 0,
    totalPages: 0,
    currentPage: 1,
    pageSize: 10,
    hasNext: false,
    hasPrevious: false
  };

  beforeEach(async () => {
    pharmacyService = jasmine.createSpyObj('PharmacyService', [
      'getMedicationsWithFilters',
      'getMedicationAutocompleteSuggestions'
    ]);
    pharmacyService.getMedicationsWithFilters.and.returnValue(of(emptyPagedResponse));
    pharmacyService.getMedicationAutocompleteSuggestions.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [MedicationsComponent],
      providers: [
        PharmacyFilterService,
        { provide: PharmacyService, useValue: pharmacyService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationsComponent);
    component = fixture.componentInstance;
    filterService = TestBed.inject(PharmacyFilterService);
  });

  it('populates search, closes dropdown, and reloads list on suggestion select', () => {
    fixture.detectChanges();

    component.showAutocompleteDropdown = true;
    component.autocompleteSuggestions = [{ id: 3, name: 'Aspirin', category: 'painkiller' }];
    filterService.updateFilters({ pageNumber: 2, searchTerm: 'asp' });

    component.selectAutocompleteSuggestion({ id: 3, name: 'Aspirin', category: 'painkiller' });

    expect(component.searchTerm).toBe('Aspirin');
    expect(component.showAutocompleteDropdown).toBeFalse();
    expect(component.autocompleteSuggestions).toEqual([]);
    expect(filterService.getFilters().searchTerm).toBe('Aspirin');
    expect(filterService.getFilters().pageNumber).toBe(1);
    expect(pharmacyService.getMedicationsWithFilters).toHaveBeenCalled();
  });

  it('ignores blank suggestion names', () => {
    fixture.detectChanges();
    const updateSpy = spyOn(filterService, 'updateFilters').and.callThrough();

    component.selectAutocompleteSuggestion({ id: 1, name: '   ' });

    expect(updateSpy).not.toHaveBeenCalled();
  });
});
