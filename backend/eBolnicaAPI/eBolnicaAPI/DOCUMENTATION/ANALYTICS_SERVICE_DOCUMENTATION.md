# Pharmacy Analytics Service Documentation

## Overview

The `PharmacyAnalyticsService` provides optimized business logic for aggregating pharmacy analytics data including monthly revenue, top medication categories, and stock level trends.

## Architecture

### Service Implementation
- **Location**: `Services/PharmacyAnalyticsService.cs`
- **Interface**: `Services/IPharmacyAnalyticsService.cs`
- **Caching**: In-memory caching with configurable expiration
- **Performance**: Optimized database queries with proper indexing

## Core Methods

### 1. GetMonthlyRevenueAsync

**Purpose**: Calculate monthly revenue from prescription sales

**Business Logic**:
- Aggregates revenue from `PrescriptionItems` (Quantity * UnitPrice) for accuracy
- Only includes "Dispensed" prescriptions
- Groups by month and year
- Fills missing months with zero revenue
- Calculates percentage change vs previous month

**Query Optimization**:
```csharp
// Uses JOIN and GROUP BY at database level
from prescription in Prescriptions
join prescriptionItem in PrescriptionItems
group by Year, Month
select Sum(TotalPrice), Count(Distinct PrescriptionId)
```

**Performance**:
- Uses `AsNoTracking()` for read-only queries
- Single database query with aggregation
- Typical execution: < 200ms for 12 months

**Caching**: 5 minutes

### 2. GetTopCategoriesAsync

**Purpose**: Calculate top medication categories by sales

**Business Logic**:
- Calculates categories from prescription sales (not just inventory)
- Weights by both quantity sold and revenue
- Includes percentage of total sales
- Aggregates remaining categories as "Other"
- Falls back to inventory-based calculation if no sales data

**Query Optimization**:
```csharp
// Three-table JOIN with aggregation
from prescriptionItem in PrescriptionItems
join prescription in Prescriptions
join medication in Medications
where Status = 'Dispensed' AND IsActive = true
group by Category
select Sum(Quantity), Sum(TotalPrice)
```

**Performance**:
- Single optimized query
- Typical execution: < 150ms for top 10 categories

**Caching**: 15 minutes (longer cache as category data changes less frequently)

### 3. GetStockTrendsAsync

**Purpose**: Calculate stock level trends over time

**Business Logic**:
- Calculates stock level percentage: (CurrentStock / MaxStock) * 100
- Max stock = MinimumStockLevel * 3 (or current stock if higher)
- Determines status: Critical (<20%), Low (20-50%), Normal (50-80%), Optimal (>80%)
- Calculates trend direction (improving/declining)
- Supports daily, weekly, monthly intervals

**Query Optimization**:
- Single query to fetch all medications
- In-memory processing for timeline generation
- Limits to top 5 medications if not specified

**Performance**:
- Typical execution: < 100ms for 30 days, 5 medications

**Caching**: 5 minutes

## Performance Optimizations

### Database Indexes

The following indexes are recommended/created for optimal performance:

1. **IX_Prescriptions_Status_DispensedDate**
   - Composite index for revenue queries
   - Covers: Status = 'Dispensed' AND DispensedDate BETWEEN dates

2. **IX_PrescriptionItems_MedicationId**
   - Index for category calculations
   - Speeds up JOIN with Medications table

3. **IX_PrescriptionItems_PrescriptionId_MedicationId**
   - Composite index for complex joins
   - Optimizes category aggregation queries

4. **IX_Medications_Category_IsActive**
   - Composite index for category filtering
   - Speeds up active medication queries by category

### Caching Strategy

**Cache Keys**:
- Include all query parameters in cache key
- Format: `{method_name}_{param1}_{param2}_{...}`

**Cache Expiration**:
- **Short-term** (5 minutes): Revenue data, stock trends
- **Long-term** (15 minutes): Category data (changes less frequently)

**Cache Invalidation**:
- Automatic expiration based on time
- Manual invalidation on data changes (future enhancement)

### Query Optimization Techniques

1. **AsNoTracking()**: All read queries use `AsNoTracking()` for better performance
2. **Projection**: Select only needed columns
3. **Database Aggregation**: Use GROUP BY at database level, not in-memory
4. **Parallel Execution**: Dashboard stats queries run in parallel
5. **Date Range Limits**: Maximum 2 years for revenue queries

## Business Rules

### Revenue Calculation

1. **Include Only**: Prescriptions with Status = "Dispensed"
2. **Revenue Source**: Sum of PrescriptionItems.TotalPrice (Quantity * UnitPrice)
3. **Exclude**: Cancelled, Pending, or Refunded prescriptions
4. **Date Range**: Based on DispensedDate, not PrescribedDate

### Category Analysis

1. **Data Source**: Prescription sales (not just inventory)
2. **Weighting**: Both quantity sold and revenue considered
3. **Uncategorized**: Medications without category are excluded
4. **Aggregation**: Top N categories shown, rest as "Other"

### Stock Trends

1. **Stock Level Formula**: (CurrentStock / MaxStock) * 100
2. **Max Stock Calculation**: MinimumStockLevel * 3 (or current stock if higher)
3. **Status Thresholds**:
   - Critical: < 20%
   - Low: 20% - 50%
   - Normal: 50% - 80%
   - Optimal: > 80%
4. **Trend Direction**: Calculated as percentage change from first to last data point

## Error Handling

### Validation Errors
- Invalid date ranges (start > end)
- Date range exceeds maximum (2 years)
- Invalid medication IDs
- Invalid interval values

### Database Errors
- Timeout handling (30 second limit)
- Connection errors with retry logic
- Memory overflow protection (max 10,000 results)

### Error Responses
- ArgumentException: Re-thrown for validation errors
- InvalidOperationException: Wrapped for business logic errors
- All errors logged with context

## Monitoring

### Logging
- Query execution time
- Cache hit/miss rates
- Error occurrences with context
- Performance metrics

### Performance Targets
- Monthly Revenue: < 200ms
- Top Categories: < 150ms
- Stock Trends: < 100ms
- Dashboard Stats (combined): < 500ms

## Usage Examples

### Monthly Revenue
```csharp
var revenue = await analyticsService.GetMonthlyRevenueAsync(
    startDate: DateTime.UtcNow.AddMonths(-12),
    endDate: DateTime.UtcNow,
    months: 12
);
```

### Top Categories
```csharp
var categories = await analyticsService.GetTopCategoriesAsync(topCount: 10);
```

### Stock Trends
```csharp
var trends = await analyticsService.GetStockTrendsAsync(
    medicationIds: new[] { 1, 2, 3 },
    days: 30,
    interval: "daily"
);
```

## Database Index Recommendations

### Existing Indexes (Already Created)
- `IX_Prescriptions_Status_CreatedAt`
- `IX_Prescriptions_PrescribedDate`
- `IX_Medications_Category`
- `IX_Medications_ExpiryDate`
- `IX_Medications_StockQuantity`

### New Indexes (Added for Analytics)
- `IX_Prescriptions_Status_DispensedDate` - For revenue queries
- `IX_PrescriptionItems_MedicationId` - For category joins
- `IX_PrescriptionItems_PrescriptionId_MedicationId` - Composite for complex queries
- `IX_Medications_Category_IsActive` - For category filtering

### Future Enhancements
If `InventoryHistory` table is added:
- `IX_InventoryHistory_MedicationId_Timestamp` - For stock trend queries
- `IX_InventoryHistory_Timestamp` - For time-based queries

## Testing Recommendations

### Unit Tests
- Test each aggregation method with mock data
- Test edge cases (empty data, single record)
- Test validation logic

### Integration Tests
- Test with real database
- Test query performance
- Test caching behavior

### Performance Tests
- Test with large datasets (10,000+ prescriptions)
- Test concurrent requests
- Test memory usage

## Configuration

### Cache Settings
```json
{
  "AnalyticsSettings": {
    "CacheExpirationMinutes": 5,
    "LongCacheExpirationMinutes": 15,
    "MaxQueryTimeoutSeconds": 30
  }
}
```

### Performance Settings
```json
{
  "PerformanceSettings": {
    "MaxResultSetSize": 10000,
    "EnableQueryLogging": false
  }
}
```

## Troubleshooting

### Slow Queries
1. Check database indexes are created
2. Verify date ranges are reasonable
3. Check cache hit rates
4. Review query execution plans

### Memory Issues
1. Reduce date range
2. Limit number of medications
3. Use pagination for large datasets
4. Monitor result set sizes

### Cache Issues
1. Verify cache is working (check logs)
2. Clear cache if data is stale
3. Adjust cache expiration times

## Future Enhancements

1. **InventoryHistory Table**: Track historical stock levels
2. **Materialized Views**: Pre-aggregate common queries
3. **Background Jobs**: Pre-calculate dashboard stats
4. **Distributed Caching**: Use Redis for multi-server scenarios
5. **Real-time Updates**: WebSocket notifications for data changes
