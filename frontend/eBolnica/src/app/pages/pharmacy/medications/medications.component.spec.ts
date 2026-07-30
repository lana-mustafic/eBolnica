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
      'getMedicationAutocomplete'
    ]);
    pharmacyService.getMedicationsWithFilters.and.returnValue(of(emptyPagedResponse));
    pharmacyService.getMedicationAutocomplete.and.returnValue(of([]));

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
    filterService.updateFilters('medications', { pageNumber: 2, searchTerm: 'asp' });

    component.selectAutocompleteSuggestion({ id: 3, name: 'Aspirin', category: 'painkiller' });

    expect(component.searchTerm).toBe('Aspirin');
    expect(component.showAutocompleteDropdown).toBeFalse();
    expect(component.autocompleteSuggestions).toEqual([]);
    expect(filterService.getFilters('medications').searchTerm).toBe('Aspirin');
    expect(filterService.getFilters('medications').pageNumber).toBe(1);
    expect(pharmacyService.getMedicationsWithFilters).toHaveBeenCalled();
  });

  it('ignores blank suggestion names', () => {
    fixture.detectChanges();
    const updateSpy = spyOn(filterService, 'updateFilters').and.callThrough();

    component.selectAutocompleteSuggestion({ id: 1, name: '   ' });

    expect(updateSpy).not.toHaveBeenCalled();
  });
});

describe('MedicationsComponent autocomplete keyboard navigation', () => {
  let component: MedicationsComponent;
  let fixture: ComponentFixture<MedicationsComponent>;
  let pharmacyService: jasmine.SpyObj<PharmacyService>;

  const suggestions = [
    { id: 1, name: 'Aspirin', category: 'painkiller' },
    { id: 2, name: 'Amoxicillin', category: 'antibiotics' }
  ];

  beforeEach(async () => {
    pharmacyService = jasmine.createSpyObj('PharmacyService', [
      'getMedicationsWithFilters',
      'getMedicationAutocomplete'
    ]);
    pharmacyService.getMedicationsWithFilters.and.returnValue(of({
      items: [],
      totalCount: 0,
      totalPages: 0,
      currentPage: 1,
      pageSize: 10,
      hasNext: false,
      hasPrevious: false
    }));
    pharmacyService.getMedicationAutocomplete.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [MedicationsComponent],
      providers: [
        PharmacyFilterService,
        { provide: PharmacyService, useValue: pharmacyService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.showAutocompleteDropdown = true;
    component.autocompleteSuggestions = suggestions;
    component.autocompleteHighlightIndex = -1;
  });

  function keydown(key: string): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    spyOn(event, 'preventDefault');
    spyOn(event, 'stopPropagation');
    component.onSearchKeydown(event);
    return event;
  }

  it('highlights the first suggestion on ArrowDown', () => {
    keydown('ArrowDown');

    expect(component.autocompleteHighlightIndex).toBe(0);
  });

  it('selects highlighted suggestion on Enter', () => {
    component.autocompleteHighlightIndex = 1;
    spyOn(component, 'selectAutocompleteSuggestion');

    keydown('Enter');

    expect(component.selectAutocompleteSuggestion).toHaveBeenCalledWith(suggestions[1]);
  });

  it('closes dropdown on Escape without selecting', () => {
    component.autocompleteHighlightIndex = 0;
    const event = keydown('Escape');

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(component.showAutocompleteDropdown).toBeFalse();
    expect(component.autocompleteHighlightIndex).toBe(-1);
  });

  it('shows empty state when autocomplete returns no matches', () => {
    component.isAutocompleteLoading = false;
    component.autocompleteSuggestions = [];
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('.autocomplete-empty') as HTMLElement;

    expect(empty).toBeTruthy();
    expect(empty.textContent?.trim()).toBe('No medications found');
  });

  it('hides empty state while autocomplete is loading', () => {
    component.isAutocompleteLoading = true;
    component.autocompleteSuggestions = [];
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.autocomplete-empty')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Loading suggestions...');
  });
});
