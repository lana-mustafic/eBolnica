# Pharmacy Module Test Suite

## Overview
Comprehensive test suite for Pharmacy module pagination, filtering, and sorting functionality.

## Test Structure

```
Tests/
├── Unit/
│   └── Services/
│       └── PharmacyServiceUnitTests.cs      # Unit tests for service logic
├── Integration/
│   └── Controllers/
│       └── PharmacyControllerIntegrationTests.cs  # Integration tests for API endpoints
└── PharmacyFilterSortTests.cs               # Original integration tests
```

## Running Tests

### Run All Tests
```bash
dotnet test backend/eBolnicaAPI/eBolnicaAPI.Tests/
```

### Run Unit Tests Only
```bash
dotnet test --filter "FullyQualifiedName~PharmacyServiceUnitTests"
```

### Run Integration Tests Only
```bash
dotnet test --filter "FullyQualifiedName~PharmacyControllerIntegrationTests"
```

### Run with Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Coverage

### Unit Tests (PharmacyServiceUnitTests.cs)

**Pagination Logic:**
- ✅ Skip/Take calculation
- ✅ Page number validation (zero defaults to one)
- ✅ Page size clamping (exceeds max defaults to max)
- ✅ Empty results handling
- ✅ Total pages calculation
- ✅ HasNext/HasPrevious calculation

**Filter Builder:**
- ✅ Category filter
- ✅ Search term with special characters
- ✅ Price range validation (min > max)
- ✅ Invalid category handling
- ✅ Empty search term handling
- ✅ Multiple filters with AND logic
- ✅ Stock status filters (InStock, LowStock, OutOfStock)

**Sort Builder:**
- ✅ Single column sorting
- ✅ Invalid column fallback
- ✅ Invalid sort order handling
- ✅ Multi-column sorting
- ✅ Multi-column with different column/order count
- ✅ Embedded order format (name:asc,price:desc)
- ✅ Default sorting (createdAt desc)

**Parameter Validation:**
- ✅ Page number < 1 defaults to 1
- ✅ Page size < 1 defaults to 1
- ✅ Page size > 100 clamps to 100

### Integration Tests (PharmacyControllerIntegrationTests.cs)

**GetMedications Endpoint:**
- ✅ Pagination only
- ✅ Filtering only
- ✅ Sorting only
- ✅ Multi-column sorting
- ✅ All combined (pagination + filtering + sorting)
- ✅ Empty results
- ✅ Invalid page number handling
- ✅ Page size exceeding max

**GetPrescriptions Endpoint:**
- ✅ Pagination
- ✅ Filtering
- ✅ Sorting

**GetInventory Endpoint:**
- ✅ Returns paginated results with alerts
- ✅ Filtering works correctly

## Test Data

Tests use in-memory database with seeded test data:
- Medications with various categories, prices, stock levels
- Prescriptions with different statuses
- Test data is isolated per test run

## Test Authentication

Integration tests use a custom `TestAuthHandler` that bypasses JWT authentication:
- Automatically authenticates as Pharmacist role
- No need for real JWT tokens in tests

## Code Coverage Goals

- **Target:** >80% coverage on PharmacyService and PharmacyController
- **Current Coverage Areas:**
  - ✅ All filter methods
  - ✅ All sort methods
  - ✅ Pagination logic
  - ✅ Parameter validation
  - ✅ Edge cases

## Notes

- Tests use in-memory database for isolation
- Each test class cleans up after itself
- Tests are independent and can run in any order
- No external dependencies required
