import { fakeAsync, tick } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import {
  MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS,
  MEDICATION_AUTOCOMPLETE_MIN_LENGTH,
  createMedicationAutocompleteSearch$,
  resolveMedicationAutocompleteSelection
} from './medication-autocomplete-search.util';

describe('createMedicationAutocompleteSearch$', () => {
  const suggestions = [{ id: 1, name: 'Aspirin', category: 'Painkillers' }];

  it('debounces autocomplete calls by 300ms', fakeAsync(() => {
    const fetch = jasmine.createSpy('fetch').and.returnValue(of(suggestions));
    const input$ = new Subject<string>();
    const results: string[] = [];

    createMedicationAutocompleteSearch$(input$, fetch).subscribe(result => {
      results.push(result.kind);
    });

    input$.next('as');
    tick(MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS - 1);
    expect(fetch).not.toHaveBeenCalled();

    tick(1);
    expect(fetch).toHaveBeenCalledOnceWith('as', 10);
    expect(results).toEqual(['loading', 'success']);
  }));

  it('does not fetch when query is shorter than minimum length', fakeAsync(() => {
    const fetch = jasmine.createSpy('fetch').and.returnValue(of(suggestions));
    const input$ = new Subject<string>();
    const results: string[] = [];

    createMedicationAutocompleteSearch$(input$, fetch).subscribe(result => {
      results.push(result.kind);
    });

    input$.next('a');
    tick(MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS);

    expect(fetch).not.toHaveBeenCalled();
    expect(results).toEqual(['idle']);
  }));

  it('ignores duplicate consecutive queries', fakeAsync(() => {
    const fetch = jasmine.createSpy('fetch').and.returnValue(of(suggestions));
    const input$ = new Subject<string>();

    createMedicationAutocompleteSearch$(input$, fetch).subscribe();

    input$.next('asp');
    input$.next('asp');
    tick(MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS);

    expect(fetch).toHaveBeenCalledTimes(1);
  }));

  it('cancels an in-flight request when the query changes', fakeAsync(() => {
    const fetch = jasmine.createSpy('fetch').and.callFake((term: string) => of([{ id: 2, name: term }]));
    const input$ = new Subject<string>();
    const successTerms: string[] = [];

    createMedicationAutocompleteSearch$(input$, fetch).subscribe(result => {
      if (result.kind === 'success') {
        successTerms.push(result.suggestions[0].name);
      }
    });

    input$.next('asp');
    tick(MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS);
    input$.next('ibu');
    tick(MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS);

    expect(fetch).toHaveBeenCalledTimes(2);
    expect(successTerms).toEqual(['ibu']);
  }));

  it('emits error when fetch fails', fakeAsync(() => {
    const fetch = jasmine.createSpy('fetch').and.returnValue(throwError(() => new Error('network')));
    const input$ = new Subject<string>();
    const results: string[] = [];

    createMedicationAutocompleteSearch$(input$, fetch).subscribe(result => {
      results.push(result.kind);
    });

    input$.next('asp');
    tick(MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS);

    expect(results).toEqual(['loading', 'error']);
  }));

  it('trims whitespace before checking minimum length and fetching', fakeAsync(() => {
    const fetch = jasmine.createSpy('fetch').and.returnValue(of(suggestions));
    const input$ = new Subject<string>();

    createMedicationAutocompleteSearch$(input$, fetch, {
      minLength: MEDICATION_AUTOCOMPLETE_MIN_LENGTH
    }).subscribe();

    input$.next('  as  ');
    tick(MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS);

    expect(fetch).toHaveBeenCalledOnceWith('as', 10);
  }));
});

describe('resolveMedicationAutocompleteSelection', () => {
  it('returns trimmed medication name', () => {
    expect(resolveMedicationAutocompleteSelection({ id: 1, name: '  Aspirin  ' }))
      .toBe('Aspirin');
  });

  it('returns null for blank suggestion names', () => {
    expect(resolveMedicationAutocompleteSelection({ id: 1, name: '   ' })).toBeNull();
  });
});
