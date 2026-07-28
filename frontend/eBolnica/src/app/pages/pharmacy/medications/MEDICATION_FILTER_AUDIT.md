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
