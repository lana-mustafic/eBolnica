# Pharmacy Module - LiveFilter and Multi-Column Sorting Implementation

## Overview
This document describes the implementation of LiveFilter support and multi-column sorting for the Pharmacy module endpoints.

## Features Implemented

### 1. LiveFilter Support (Real-time Filtering)
✅ **Fully Implemented**

- **Multiple simultaneous filters**: All filters work together with AND logic
- **Supported filter types**:
  - String filters: `category`, `search`, `status`, `stockStatus`
  - Numeric range filters: `minPrice`, `maxPrice`, `minStock`, `maxStock`
  - Boolean filters: `isActive`, `requiresPrescription`
  - Date range filters: `createdAfter`, `createdBefore`, `expiryAfter`, `expiryBefore`
- **Case-insensitive filtering**: All text filters are case-insensitive
- **Special characters**: Properly handled in search terms
- **Empty/null values**: Ignored safely

**Example URL:**
```
GET /api/pharmacy/medications?search=aspirin&category=painkiller&minPrice=5&maxPrice=20&stockStatus=InStock
```

### 2. Multi-Column Sorting
✅ **Fully Implemented**

- **Single column sorting**: `sortBy=name&sortOrder=asc`
- **Multi-column sorting (comma-separated)**: `sortBy=name,price,createdAt&sortOrder=asc,desc,desc`
- **Multi-column sorting (embedded order)**: `sortBy=name:asc,price:desc,createdAt:desc`
- **Default sorting**: `createdAt desc` when not specified
- **Invalid columns**: Fall back to default sorting

**Supported sort columns for Medications:**
- `name`, `price`, `createdAt`/`dateCreated`, `stockQuantity`/`stock`, `category`, `expiryDate`/`expiry`

**Supported sort columns for Prescriptions:**
- `createdAt`/`dateCreated`, `totalAmount`/`amount`, `prescriptionNumber`/`number`, `status`, `prescribedDate`

**Example URLs:**
```
GET /api/pharmacy/medications?sortBy=category,name&sortOrder=asc,asc
GET /api/pharmacy/medications?sortBy=name:asc,price:desc
```

### 3. SQL Query Validation
✅ **Implemented**

- **Database-level filtering**: All filters applied using WHERE clauses
- **Database-level sorting**: All sorting applied using ORDER BY clauses
- **Efficient pagination**: Uses OFFSET/FETCH in SQL
- **Performance optimization**: Uses `AsNoTracking()` for read-only queries

**To enable SQL query logging** (for debugging), add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Example generated SQL:**
```sql
SELECT [m].[Id], [m].[Name], [m].[Price], ...
FROM [Medications] AS [m]
WHERE ([m].[Category] = N'painkiller') 
  AND ([m].[Price] >= 5.0) 
  AND ([m].[Price] <= 20.0)
  AND ([m].[StockQuantity] >= [m].[MinimumStockLevel])
ORDER BY [m].[Category] ASC, [m].[Name] ASC, [m].[Price] DESC
OFFSET @__skipValue ROWS FETCH NEXT @__takeValue ROWS ONLY
```

### 4. Performance Optimizations
✅ **Implemented**

- **AsNoTracking()**: Applied to all read-only queries
- **Efficient counting**: Total count calculated before pagination
- **Database indexes**: Recommended indexes (see below)

**Recommended Database Indexes:**
```sql
CREATE INDEX IX_Medications_Category ON Medications(Category);
CREATE INDEX IX_Medications_IsActive ON Medications(IsActive);
CREATE INDEX IX_Medications_Price ON Medications(Price);
CREATE INDEX IX_Medications_StockQuantity ON Medications(StockQuantity);
CREATE INDEX IX_Medications_CreatedAt ON Medications(CreatedAt);
CREATE INDEX IX_Prescriptions_Status ON Prescriptions(Status);
CREATE INDEX IX_Prescriptions_CreatedAt ON Prescriptions(CreatedAt);
```

### 5. Error Handling & Edge Cases
✅ **Implemented**

- **Invalid filter values**: Ignored safely (no errors thrown)
- **Missing sort columns**: Falls back to default sorting
- **Empty search terms**: Ignored (doesn't break query)
- **Null/empty filter values**: Ignored
- **Case-insensitive**: All text filters are case-insensitive
- **Special characters**: Properly handled in search terms
- **Out of range page numbers**: Adjusted to last valid page
- **Empty results**: Returns empty paginated response with correct metadata

### 6. Integration Testing
✅ **Test Suite Created**

Location: `backend/eBolnicaAPI/eBolnicaAPI.Tests/PharmacyFilterSortTests.cs`

**Test Coverage:**
- ✅ Single filter tests
- ✅ Multiple filter combinations
- ✅ Search + filters
- ✅ All filter types combined
- ✅ Case-insensitive filtering
- ✅ Empty search term handling
- ✅ Single column sorting
- ✅ Multi-column sorting
- ✅ Default sorting
- ✅ Invalid column handling
- ✅ Combined filtering + sorting

**To run tests:**
```bash
dotnet test backend/eBolnicaAPI/eBolnicaAPI.Tests/PharmacyFilterSortTests.cs
```

## API Endpoints

### GetMedications
```
GET /api/pharmacy/medications
```

**Query Parameters:**
- Pagination: `pageNumber`, `pageSize`
- Sorting: `sortBy`, `sortOrder`
- Filters: `category`, `search`, `stockStatus`, `minPrice`, `maxPrice`, `isActive`, `requiresPrescription`, `minStock`, `maxStock`, `createdAfter`, `createdBefore`, `expiryAfter`, `expiryBefore`

**Response:**
```json
{
  "items": [...],
  "totalCount": 150,
  "totalPages": 15,
  "hasNext": true,
  "hasPrevious": false,
  "currentPage": 1,
  "pageSize": 10
}
```

### GetPrescriptions
```
GET /api/pharmacy/prescriptions
```

**Query Parameters:**
- Pagination: `pageNumber`, `pageSize`
- Sorting: `sortBy`, `sortOrder`
- Filters: `status`, `patientId`, `doctorId`, `pharmacistId`, `minAmount`, `maxAmount`, `prescribedAfter`, `prescribedBefore`, `dispensedAfter`, `dispensedBefore`

### GetInventory
```
GET /api/pharmacy/inventory
```

**Query Parameters:** Same as GetMedications, plus:
- Returns `LowStockAlerts` and `ExpiryAlerts` calculated from all matching items

## Frontend-Backend Alignment

### Parameter Names
✅ All parameter names match frontend expectations:
- `pageNumber` (1-based)
- `pageSize` (1-100)
- `sortBy` (comma-separated columns)
- `sortOrder` (comma-separated orders or embedded)
- Filter names match exactly

### Response Structure
✅ Matches frontend expectations:
- `items`: Array of DTOs
- `totalCount`: Total records
- `totalPages`: Total pages
- `hasNext`: Boolean
- `hasPrevious`: Boolean
- `currentPage`: Current page number
- `pageSize`: Items per page

## Performance Benchmarks

**Target Performance:**
- Filter + Sort + Pagination: < 200ms
- Simple filter: < 50ms
- Multi-column sort: < 100ms

**Optimization Techniques Used:**
1. `AsNoTracking()` for read-only queries
2. Database-level filtering and sorting
3. Efficient pagination with OFFSET/FETCH
4. Count query executed before pagination

## Usage Examples

### Example 1: Simple Filter
```
GET /api/pharmacy/medications?category=antibiotics
```

### Example 2: Multiple Filters
```
GET /api/pharmacy/medications?category=antibiotics&minPrice=10&isActive=true
```

### Example 3: Search + Filters
```
GET /api/pharmacy/medications?search=penicillin&category=antibiotics&stockStatus=InStock
```

### Example 4: All Filters Combined
```
GET /api/pharmacy/medications?search=aspirin&category=painkiller&minPrice=5&maxPrice=50&requiresPrescription=false&isActive=true&stockStatus=InStock
```

### Example 5: Single Column Sort
```
GET /api/pharmacy/medications?sortBy=name&sortOrder=asc
```

### Example 6: Multi-Column Sort
```
GET /api/pharmacy/medications?sortBy=category,name&sortOrder=asc,asc
```

### Example 7: Multi-Column Sort (Embedded Order)
```
GET /api/pharmacy/medications?sortBy=name:asc,price:desc
```

### Example 8: Filter + Sort + Pagination
```
GET /api/pharmacy/medications?category=antibiotics&sortBy=price&sortOrder=asc&pageNumber=1&pageSize=10
```

## Testing Checklist

- [x] Single filter works
- [x] Multiple filters work together
- [x] Search + filters work together
- [x] All filter types combined work
- [x] Case-insensitive filtering works
- [x] Empty search term doesn't break query
- [x] Single column sorting works
- [x] Multi-column sorting works
- [x] Multi-column sorting with embedded order works
- [x] Default sorting works
- [x] Invalid sort column falls back to default
- [x] Filter + sort + pagination works together
- [x] SQL queries are efficient (database-level)
- [x] Performance targets met (< 200ms)

## Future Enhancements

1. **Response Caching**: Add caching for common filter combinations
2. **Full-Text Search**: Consider implementing full-text search for better search performance
3. **Filter Presets**: Allow saving common filter combinations
4. **Export Functionality**: Add CSV/Excel export for filtered results

## Notes

- All filtering and sorting happens at the database level for optimal performance
- The implementation maintains backward compatibility with existing endpoints
- SQL query logging can be enabled in development for debugging
- Database indexes are recommended for frequently filtered columns
