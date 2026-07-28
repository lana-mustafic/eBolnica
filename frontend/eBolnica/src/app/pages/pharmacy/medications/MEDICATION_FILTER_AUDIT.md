# Medication List Filter Audit

## Task 3 — Price range UI decision

**Date:** 2026-07-28  
**Outcome:** Price range UI **not added**.

### Reason

The medications list already has **5 fully wired functional filters**:

1. Search (`searchTerm`)
2. Category (`category`)
3. Stock status (`stockStatus`)
4. Requires prescription (`requiresPrescription`)
5. Active/inactive (`isActive`)

RS1 requires **5+** filter parameters. Adding min/max price would be a **6th optional filter**, not required to meet the criterion.

### What was implemented instead

- `validatePriceRange()` in `medications.component.ts` — blocks API calls when `minPrice > maxPrice` or values are negative
- Validation runs in `pushFiltersFromUI()` before `PharmacyFilterService.updateFilters()`
- Backend and `PharmacyService.buildMedicationQueryParams()` already support `minPrice` / `maxPrice` for a future UI

### If price range UI is added later

1. Add `minPrice` / `maxPrice` inputs to `medications.component.html`
2. Bind UI state in `buildFiltersFromUI()` and `syncUIFromFilters()`
3. Handle `priceRange` badge removal in `removeFilter()`
4. Existing validation in `pushFiltersFromUI()` will apply automatically

## Task 4 — Active filter badges

- `PharmacyFilterService.getActiveFilters()` returns typed `ActiveFilter[]` with readable labels
- Stock status badges show title case (e.g. `Low Stock`)
- Min/max price use separate removable badges (`minPrice`, `maxPrice`)
- `ActiveFiltersComponent` uses `ActiveFilter` type and `trackByFilterKey`
- `medications.component.removeFilter()` clears all five medication filters plus price keys via `clearFilterByBadgeKey`

## Task 6 — Debounced search verification

- **Search debounce:** 300ms via `MEDICATION_SEARCH_DEBOUNCE_MS` on `searchSubject`
- **Dropdown filters:** immediate `pushFiltersFromUI()` including current `searchTerm` (AND combination)
- **Single API pipeline:** `getFilters$()` → `switchMap` (cancels in-flight requests)
- **Service debounce:** 200ms on `PharmacyFilterService.getFilters$()` coalesces rapid updates
- **Removed:** extra 150ms debounce on component subscription (was stacking with service debounce)
- **Removed:** duplicate `loadMedications()` on init before subscription setup
- **Added:** `filtersMatchServiceState()` skips `updateFilters` when filter JSON is unchanged
- **Init:** subscribe first, then one `pushFiltersFromUI()` — debounce coalesces to a single API call
