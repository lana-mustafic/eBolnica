# Pharmacy Dashboard Metrics Audit

**Task:** Audit `pharmacy-dashboard.component.ts` for hardcoded/mock metrics  
**Date:** 2026-07-29  
**Scope:** Top metric cards, data loading in dashboard component, and how chart child components relate to mock/synthetic data.

---

## Executive summary

`pharmacy-dashboard.component.ts` does **not** contain literal hardcoded metric numbers (no `totalMedications = 42` style values). Metrics are initialized to `0` and computed in `calculateMetrics()` from API list responses.

However, the dashboard **does not use analytics or inventory summary endpoints**. Top cards are derived client-side from paginated list APIs, which can produce **inaccurate counts** and diverge from backend/inventory logic. Chart components load independently and can show **silent mock data** via `PharmacyService` fallbacks when analytics APIs fail (outside this file, but affects the same page).

---

## Top metric cards (`pharmacy-dashboard.component.ts`)

| Metric | Source in code | Hardcoded? | Risk |
|--------|----------------|------------|------|
| `totalMedications` | `medications.filter(m => m.isActive).length` after `getAllMedications()` | No literal | **High** — capped by `pageSize: 1000`; not `totalCount` from API |
| `pendingPrescriptions` | `prescriptions.filter(p => p.status === 'Pending').length` | No literal | **Medium** — depends on default prescription pagination |
| `lowStockAlerts` | `stockQuantity < minimumStockLevel` on loaded medications | No literal | **High** — partial list + differs from inventory stock-status buckets |
| `expiringSoon` | Active meds with expiry in next 30 days (client date math) | No literal | **Medium** — partial list; no **expired** count (AC mentions both) |

### Data loading (`loadDashboardData`)

```typescript
forkJoin({
  medications: this.pharmacyService.getAllMedications({ isActive: true, page: 1, pageSize: 1000 }),
  prescriptions: this.pharmacyService.getPrescriptions()
})
```

**Findings:**

1. **Not using** `GET /api/pharmacy/analytics/dashboard-stats` (exists on backend).
2. **Not using** `PharmacyService.getDashboardStats()` (exists on FE, unused by dashboard).
3. **Not using** inventory `totalCount` / alert counts from `GET /api/pharmacy/inventory`.
4. On API failure, dashboard shows **explicit error** + retry — **good** (no mock substitution in component).
5. Charts are **not** part of `forkJoin` — they load in child components with separate error/loading state.

---

## Hardcoded / magic values in template (`pharmacy-dashboard.component.html`)

Not mock metrics, but fixed presentation/config:

| Location | Value | Notes |
|----------|-------|-------|
| Revenue chart | `[period]="'last12months'"` | OK if matches API |
| Categories chart | `[maxCategories]="8"` | OK |
| Stock trends chart | `[days]="30"`, title `'Medication Stock Trends'` | Title implies historical trend; backend may use current stock only |
| Expiring window | — | 30-day window only in TS (`calculateMetrics`), not configurable |

---

## Chart components (embedded, load independently)

| Component | API | Mock on failure? |
|-----------|-----|------------------|
| `RevenueBarChartComponent` | `GET .../analytics/monthly-revenue` | **Yes** — via `PharmacyService.getMockMonthlyRevenue()` (`Math.random()` revenue) |
| `CategoriesPieChartComponent` | `GET .../analytics/top-categories` | **Yes** — via `getMockTopCategories()` (fixed fake categories) |
| `StockTrendsLineChartComponent` | `GET .../analytics/stock-trends` | **Yes** — via `getMockStockTrends()` (30-day **synthetic** timeline) |

Chart components show their own error UI only when the observable **errors**. Because `PharmacyService` `catchError` returns `of(mockData)`, charts typically **succeed with fake data** and no error banner — **violates AC** (“mock data is not silently substituted”).

---

## Backend context (relevant to dashboard accuracy)

| Area | Behavior |
|------|----------|
| Revenue / categories | Real dispensed data in `PharmacyAnalyticsService` |
| Stock trends | Uses **current stock**; comment references missing `InventoryHistory` table |
| `dashboard-stats` endpoint | Aggregates revenue, categories, stock trends; summary has `TotalMedications` but not low-stock/expiry counts |

---

## Acceptance criteria gap analysis

| AC | Dashboard component status |
|----|----------------------------|
| Top metrics reflect API/DB, not mock | **Partial** — no mocks in TS, but counts are client-derived from incomplete lists |
| Revenue chart matches API | **N/A in component** — child component; mock fallback in service |
| Categories chart matches API | **N/A in component** — same |
| Stock trends honest or replaced | **N/A in component** — title says “Trends”; service can serve synthetic mock |
| API fail → explicit error, no silent mock | **Partial** — top cards yes; charts no (service swallows errors) |
| No SignalR required | **Pass** — none used |

---

## Recommended follow-up tasks (not in this audit)

1. Replace list-based `calculateMetrics()` with authoritative API (inventory summary or dedicated dashboard metrics endpoint).
2. Wire top cards to `totalCount` / alert fields from inventory or analytics metadata.
3. Remove or gate `PharmacyService` mock fallbacks; propagate errors to chart components.
4. Rename or replace stock trends chart per backend capability (`Current stock levels by medication`).
5. Add `expired` metric card if required by product.
6. Optionally unify loading via single `getDashboardStats()` or coordinated `forkJoin` including chart data.

---

## Files reviewed

- `pharmacy-dashboard.component.ts`
- `pharmacy-dashboard.component.html`
- `pharmacy-dashboard.component.spec.ts` (minimal — create only)
- `pharmacy.service.ts` (analytics fallbacks — downstream)
- Chart components under `features/pharmacy/analytics/components/`
- `PharmacyAnalyticsService.cs`, `DashboardStatsDto.cs`
