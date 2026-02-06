# Pharmacy API Documentation

## Overview
The Pharmacy API provides endpoints for managing medications, prescriptions, and inventory with comprehensive pagination, filtering, and sorting capabilities.

## Base URL
```
http://localhost:5004/api/pharmacy
```

## Authentication
All endpoints require JWT Bearer token authentication with the `Pharmacist` role.

**Header:**
```
Authorization: Bearer {your_jwt_token}
```

---

## Endpoints

### 1. Get Medications

Get paginated list of medications with filtering and sorting support.

**Endpoint:** `GET /api/pharmacy/medications`

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Page number (1-based, min: 1) |
| `pageSize` | int | No | 10 | Items per page (range: 1-100) |
| `sortBy` | string | No | createdAt | Field(s) to sort by. Single: "name", Multi: "name,price", Embedded: "name:asc,price:desc" |
| `sortOrder` | string | No | desc | Sort order(s). Single: "asc", Multi: "asc,desc" |
| `category` | string | No | - | Filter by category (exact match, case-insensitive) |
| `search` | string | No | - | Search term across name, generic name, manufacturer |
| `stockStatus` | string | No | - | Filter by stock status: "InStock", "LowStock", "OutOfStock" |
| `minPrice` | decimal | No | - | Minimum price filter |
| `maxPrice` | decimal | No | - | Maximum price filter |
| `isActive` | bool | No | true | Filter by active status (default: true) |
| `requiresPrescription` | bool | No | - | Filter by prescription requirement |
| `minStock` | int | No | - | Minimum stock quantity filter |
| `maxStock` | int | No | - | Maximum stock quantity filter |
| `createdAfter` | datetime | No | - | Filter records created after this date |
| `createdBefore` | datetime | No | - | Filter records created before this date |
| `expiryAfter` | datetime | No | - | Filter medications expiring after this date |
| `expiryBefore` | datetime | No | - | Filter medications expiring before this date |

**Supported Sort Fields:**
- `name` - Medication name
- `price` - Medication price
- `createdAt` / `dateCreated` - Creation date
- `stockQuantity` / `stock` - Stock quantity
- `category` - Category name
- `expiryDate` / `expiry` - Expiry date

**Response:** `200 OK`

```json
{
  "items": [
    {
      "id": 1,
      "name": "Penicillin",
      "category": "antibiotics",
      "price": 15.50,
      "stockQuantity": 100,
      "isActive": true,
      "requiresPrescription": true,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "totalCount": 150,
  "totalPages": 15,
  "hasNext": true,
  "hasPrevious": false,
  "currentPage": 1,
  "pageSize": 10
}
```

**Example Requests:**

```bash
# Simple pagination
GET /api/pharmacy/medications?pageNumber=1&pageSize=10

# Filter by category
GET /api/pharmacy/medications?category=antibiotics

# Multiple filters
GET /api/pharmacy/medications?category=antibiotics&minPrice=10&maxPrice=50&isActive=true

# Search + filters
GET /api/pharmacy/medications?search=penicillin&category=antibiotics&stockStatus=InStock

# Single column sorting
GET /api/pharmacy/medications?sortBy=name&sortOrder=asc

# Multi-column sorting
GET /api/pharmacy/medications?sortBy=category,name&sortOrder=asc,asc

# Multi-column sorting (embedded order)
GET /api/pharmacy/medications?sortBy=name:asc,price:desc

# Combined: pagination + filtering + sorting
GET /api/pharmacy/medications?pageNumber=2&pageSize=20&category=painkiller&sortBy=price&sortOrder=asc
```

---

### 2. Get Prescriptions

Get paginated list of prescriptions with filtering and sorting support.

**Endpoint:** `GET /api/pharmacy/prescriptions`

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `pageNumber` | int | No | 1 | Page number (1-based) |
| `pageSize` | int | No | 10 | Items per page (1-100) |
| `sortBy` | string | No | createdAt | Field(s) to sort by |
| `sortOrder` | string | No | desc | Sort order(s) |
| `status` | string | No | - | Filter by status: "Pending", "Dispensed", "Cancelled" |
| `patientId` | int | No | - | Filter by patient ID |
| `doctorId` | int | No | - | Filter by doctor ID |
| `pharmacistId` | int | No | - | Filter by pharmacist ID |
| `minAmount` | decimal | No | - | Minimum total amount filter |
| `maxAmount` | decimal | No | - | Maximum total amount filter |
| `prescribedAfter` | datetime | No | - | Filter prescriptions prescribed after this date |
| `prescribedBefore` | datetime | No | - | Filter prescriptions prescribed before this date |
| `dispensedAfter` | datetime | No | - | Filter prescriptions dispensed after this date |
| `dispensedBefore` | datetime | No | - | Filter prescriptions dispensed before this date |

**Supported Sort Fields:**
- `createdAt` / `dateCreated` - Creation date
- `totalAmount` / `amount` - Total amount
- `prescriptionNumber` / `number` - Prescription number
- `status` - Status
- `prescribedDate` - Prescribed date

**Response:** `200 OK`

```json
{
  "items": [
    {
      "id": 1,
      "prescriptionNumber": "RX-2024-0001",
      "status": "Pending",
      "totalAmount": 150.00,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "totalCount": 50,
  "totalPages": 5,
  "hasNext": true,
  "hasPrevious": false,
  "currentPage": 1,
  "pageSize": 10
}
```

**Example Requests:**

```bash
# Get pending prescriptions
GET /api/pharmacy/prescriptions?status=Pending

# Filter by amount range
GET /api/pharmacy/prescriptions?minAmount=50&maxAmount=200

# Sort by date
GET /api/pharmacy/prescriptions?sortBy=createdAt&sortOrder=desc

# Combined
GET /api/pharmacy/prescriptions?status=Pending&sortBy=totalAmount&sortOrder=asc&pageNumber=1&pageSize=10
```

---

### 3. Get Inventory

Get paginated inventory with alerts (low stock and expiry alerts).

**Endpoint:** `GET /api/pharmacy/inventory`

**Query Parameters:** Same as Get Medications endpoint

**Response:** `200 OK`

```json
{
  "items": [
    {
      "id": 1,
      "name": "Penicillin",
      "stockQuantity": 100,
      "minimumStockLevel": 20
    }
  ],
  "LowStockAlerts": [
    {
      "id": 2,
      "name": "Ibuprofen",
      "stockQuantity": 5,
      "minimumStockLevel": 20
    }
  ],
  "ExpiryAlerts": [],
  "totalCount": 150,
  "totalPages": 15,
  "hasNext": true,
  "hasPrevious": false,
  "currentPage": 1,
  "pageSize": 10
}
```

**Note:** `LowStockAlerts` and `ExpiryAlerts` are calculated from ALL matching items, not just the current page.

---

## Filter Combinations

All filters use **AND** logic - all specified filters must match.

**Example:**
```
GET /api/pharmacy/medications?category=antibiotics&minPrice=10&maxPrice=50&isActive=true&stockStatus=InStock
```

This returns medications that:
- Are in the "antibiotics" category **AND**
- Have price between 10 and 50 **AND**
- Are active **AND**
- Are in stock

---

## Sorting

### Single Column Sorting
```
?sortBy=name&sortOrder=asc
```

### Multi-Column Sorting (Comma-Separated)
```
?sortBy=category,name&sortOrder=asc,asc
```

### Multi-Column Sorting (Embedded Order)
```
?sortBy=name:asc,price:desc
```

**Note:** If `sortOrder` is not provided with embedded format, defaults to "desc" for all columns.

---

## Error Responses

### 400 Bad Request
Invalid query parameters (e.g., pageNumber < 1, pageSize > 100)

### 401 Unauthorized
Missing or invalid JWT token

### 403 Forbidden
User does not have Pharmacist role

---

## Performance Notes

- All filtering and sorting happens at the database level
- Queries use `AsNoTracking()` for optimal read performance
- Recommended database indexes:
  - `IX_Medications_Category`
  - `IX_Medications_IsActive`
  - `IX_Medications_Price`
  - `IX_Medications_StockQuantity`
  - `IX_Medications_CreatedAt`
  - `IX_Prescriptions_Status`
  - `IX_Prescriptions_CreatedAt`

---

## Testing

Run integration tests:
```bash
dotnet test backend/eBolnicaAPI/eBolnicaAPI.Tests/
```

Run specific test suite:
```bash
dotnet test --filter "FullyQualifiedName~PharmacyControllerIntegrationTests"
```
