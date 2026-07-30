import { Observable, concat, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, map, switchMap } from 'rxjs/operators';
import { MedicationAutocompleteSuggestion } from '../../models/medication-autocomplete.dto';

/** Debounce delay before autocomplete API calls (ms). */
export const MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS = 300;

/** Minimum trimmed query length before autocomplete runs. */
export const MEDICATION_AUTOCOMPLETE_MIN_LENGTH = 2;

/** Maximum suggestions returned to the dropdown. */
export const MEDICATION_AUTOCOMPLETE_MAX_SUGGESTIONS = 10;

export type MedicationAutocompleteFetchFn = (
  term: string,
  limit: number
) => Observable<MedicationAutocompleteSuggestion[]>;

export interface MedicationAutocompleteSearchOptions {
  debounceMs?: number;
  minLength?: number;
  maxSuggestions?: number;
}

export type MedicationAutocompleteSearchResult =
  | { kind: 'idle' }
  | { kind: 'loading'; term: string }
  | { kind: 'success'; suggestions: MedicationAutocompleteSuggestion[] }
  | { kind: 'error' };

/**
 * Resolve the search term to apply when a suggestion is selected.
 */
export function resolveMedicationAutocompleteSelection(
  suggestion: MedicationAutocompleteSuggestion
): string | null {
  const searchTerm = suggestion.name?.trim() ?? '';
  return searchTerm.length > 0 ? searchTerm : null;
}

/**
 * Debounced autocomplete pipeline for medication search input.
 * Waits for typing to settle, then fetches up to maxSuggestions items.
 */
export function createMedicationAutocompleteSearch$(
  input$: Observable<string>,
  fetchSuggestions: MedicationAutocompleteFetchFn,
  options: MedicationAutocompleteSearchOptions = {}
): Observable<MedicationAutocompleteSearchResult> {
  const debounceMs = options.debounceMs ?? MEDICATION_AUTOCOMPLETE_DEBOUNCE_MS;
  const minLength = options.minLength ?? MEDICATION_AUTOCOMPLETE_MIN_LENGTH;
  const maxSuggestions = options.maxSuggestions ?? MEDICATION_AUTOCOMPLETE_MAX_SUGGESTIONS;

  return input$.pipe(
    debounceTime(debounceMs),
    distinctUntilChanged(),
    switchMap(term => {
      const trimmed = term.trim();

      if (trimmed.length < minLength) {
        return of({ kind: 'idle' } as const);
      }

      return concat(
        of({ kind: 'loading', term: trimmed } as const),
        fetchSuggestions(trimmed, maxSuggestions).pipe(
          map(suggestions => ({
            kind: 'success' as const,
            suggestions: suggestions.slice(0, maxSuggestions)
          })),
          catchError(() => of({ kind: 'error' } as const))
        )
      );
    })
  );
}
