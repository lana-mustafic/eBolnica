# Pharmacy Dashboard Metrics Audit

**Task:** Audit `pharmacy-dashboard.component.ts` for hardcoded/mock metrics  
**Date:** 2026-07-29  
**Scope:** Top metric cards, data loading in dashboard component, and how chart child components relate to mock/synthetic data.

---

## Executive summary

`pharmacy-dashboard.component.ts` loads top metric cards from `GET /api/pharmacy/analytics/dashboard-stats` via `PharmacyService.getDashboardStats()`. Metrics are no longer derived from capped paginated list responses.

Chart child components still load analytics endpoints independently and may show incomplete data when those APIs fail (see component `analyticsFailures` banner).

---

## Top metric cards (`pharmacy-dashboard.component.ts`)

| Metric | Source in code | Hardcoded? | Risk |
|--------|----------------|------------|------|
| `totalMedications` | `dashboard-stats` API | No literal | **Low** — server-side aggregate |
| `pendingPrescriptions` | `dashboard-stats` API | No literal | **Low** |
| `lowStockAlerts` | `dashboard-stats` API | No literal | **Low** |
| `expiringSoon` / `expiredMedications` | `dashboard-stats` API | No literal | **Low** |
| `inventoryValue` | `dashboard-stats` API | No literal | **Low** |

### Data loading (`loadDashboardData`)

Dashboard stats come from the analytics endpoint; recent prescriptions still load from the prescriptions list API for the sidebar/table section.

**Remaining note:** chart widgets use separate analytics calls — failures are surfaced via `hasAnalyticsErrors`, not silent mock substitution in the dashboard component itself.
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
