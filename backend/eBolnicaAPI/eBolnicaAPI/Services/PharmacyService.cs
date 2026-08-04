using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace eBolnicaAPI.Services
{
    /// <summary>
    /// Service for Pharmacy module dynamic query building, filtering, and sorting
    /// Separates query logic from controller for better maintainability and testability
    /// </summary>
    public class PharmacyService : IPharmacyService
    {
        /// <summary>
        /// Builds a filtered query for medications based on query parameters.
        /// Delegates to the PharmacyQueryParameters overload.
        /// </summary>
        public IQueryable<Medication> GetFilteredMedications(
            IQueryable<Medication> baseQuery,
            IQueryCollection queryParams,
            bool defaultToActiveOnly = false)
        {
            var parameters = BindFromQueryCollection(queryParams);
            return GetFilteredMedications(baseQuery, parameters, defaultToActiveOnly);
        }

        /// <summary>
        /// Builds a filtered query for prescriptions based on query parameters.
        /// Delegates to the PharmacyQueryParameters overload.
        /// </summary>
        public IQueryable<Prescription> GetFilteredPrescriptions(IQueryable<Prescription> baseQuery, IQueryCollection queryParams)
        {
            return GetFilteredPrescriptions(baseQuery, BindFromQueryCollection(queryParams));
        }

        /// <summary>
        /// Builds a filtered query for inventory (medications) based on query parameters.
        /// Inventory defaults to active medications when isActive is not specified.
        /// </summary>
        public IQueryable<Medication> GetFilteredInventory(IQueryable<Medication> baseQuery, IQueryCollection queryParams)
        {
            return GetFilteredInventory(baseQuery, BindFromQueryCollection(queryParams));
        }

        /// <summary>
        /// Applies sorting to Medication queries with support for multi-column sorting
        /// Supported sortBy values: name, price, dateCreated, createdAt, stockQuantity, stock, category, expiryDate
        /// Supports single column: sortBy=name&amp;sortOrder=asc
        /// Supports multi-column: sortBy=name,price,createdAt&amp;sortOrder=asc,desc,desc
        /// Or: sortBy=name:asc,price:desc,createdAt:desc
        /// </summary>
        public IQueryable<Medication> ApplySorting(IQueryable<Medication> query, string? sortBy, string? sortOrder)
        {
            // Default sorting if sortBy is not provided: createdAt desc
            if (string.IsNullOrEmpty(sortBy))
            {
                var defaultOrder = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "desc";
                return defaultOrder ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt);
            }

            // Check if multi-column sorting format (comma-separated or colon-separated)
            var sortColumns = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            if (sortColumns.Length > 1)
            {
                // Multi-column sorting
                return ApplyMultiColumnSorting(query, sortColumns, sortOrder);
            }
            else
            {
                // Single column sorting (backward compatible)
                return ApplySingleColumnSorting(query, sortBy, sortOrder);
            }
        }

        /// <summary>
        /// Applies single column sorting
        /// </summary>
        private IQueryable<Medication> ApplySingleColumnSorting(IQueryable<Medication> query, string sortBy, string? sortOrder)
        {
            var isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";
            var isDescending = !string.IsNullOrEmpty(sortOrder) && sortOrder.ToLower() == "desc";

            switch (sortBy.ToLower())
            {
                case "name":
                    return isAscending ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name);
                
                case "price":
                    return isAscending ? query.OrderBy(m => m.Price) : query.OrderByDescending(m => m.Price);
                
                case "datecreated":
                case "createdat":
                    return isAscending ? query.OrderBy(m => m.CreatedAt) : query.OrderByDescending(m => m.CreatedAt);
                
                case "stockquantity":
                case "stock":
                    return isAscending ? query.OrderBy(m => m.StockQuantity) : query.OrderByDescending(m => m.StockQuantity);
                
                case "category":
                    return isAscending ? query.OrderBy(m => m.Category ?? "") : query.OrderByDescending(m => m.Category ?? "");
                
                case "expirydate":
                case "expiry":
                    return isAscending 
                        ? query.OrderBy(m => m.ExpiryDate ?? DateTime.MaxValue) 
                        : query.OrderByDescending(m => m.ExpiryDate ?? DateTime.MinValue);
                
                default:
                    return query.OrderByDescending(m => m.CreatedAt);
            }
        }

        /// <summary>
        /// Applies multi-column sorting
        /// Supports: sortBy=name,price&amp;sortOrder=asc,desc
        /// Or: sortBy=name:asc,price:desc (order embedded in column name)
        /// </summary>
        private IQueryable<Medication> ApplyMultiColumnSorting(IQueryable<Medication> query, string[] sortColumns, string? sortOrder)
        {
            IOrderedQueryable<Medication>? orderedQuery = null;
            var sortOrders = string.IsNullOrEmpty(sortOrder) 
                ? new string[sortColumns.Length] 
                : sortOrder.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (int i = 0; i < sortColumns.Length; i++)
            {
                var column = sortColumns[i].Trim();
                var order = i < sortOrders.Length && !string.IsNullOrEmpty(sortOrders[i])
                    ? sortOrders[i].Trim().ToLower()
                    : "asc";

                // Check if order is embedded in column (name:asc format)
                if (column.Contains(':'))
                {
                    var parts = column.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        column = parts[0];
                        order = parts[1].ToLower();
                    }
                }

                var isAscending = order == "asc";

                if (orderedQuery == null)
                {
                    // First column - use OrderBy or OrderByDescending
                    orderedQuery = ApplyFirstSortColumn(query, column, isAscending);
                }
                else
                {
                    // Subsequent columns - use ThenBy or ThenByDescending
                    orderedQuery = ApplySubsequentSortColumn(orderedQuery, column, isAscending);
                }
            }

            return orderedQuery ?? query.OrderByDescending(m => m.CreatedAt);
        }

        /// <summary>
        /// Applies the first sort column
        /// </summary>
        private IOrderedQueryable<Medication> ApplyFirstSortColumn(IQueryable<Medication> query, string column, bool isAscending)
        {
            return column.ToLower() switch
            {
                "name" => isAscending ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name),
                "price" => isAscending ? query.OrderBy(m => m.Price) : query.OrderByDescending(m => m.Price),
                "datecreated" or "createdat" => isAscending ? query.OrderBy(m => m.CreatedAt) : query.OrderByDescending(m => m.CreatedAt),
                "stockquantity" or "stock" => isAscending ? query.OrderBy(m => m.StockQuantity) : query.OrderByDescending(m => m.StockQuantity),
                "category" => isAscending ? query.OrderBy(m => m.Category ?? "") : query.OrderByDescending(m => m.Category ?? ""),
                "expirydate" or "expiry" => isAscending 
                    ? query.OrderBy(m => m.ExpiryDate ?? DateTime.MaxValue) 
                    : query.OrderByDescending(m => m.ExpiryDate ?? DateTime.MinValue),
                _ => query.OrderByDescending(m => m.CreatedAt)
            };
        }

        /// <summary>
        /// Applies subsequent sort columns
        /// </summary>
        private IOrderedQueryable<Medication> ApplySubsequentSortColumn(IOrderedQueryable<Medication> query, string column, bool isAscending)
        {
            return column.ToLower() switch
            {
                "name" => isAscending ? query.ThenBy(m => m.Name) : query.ThenByDescending(m => m.Name),
                "price" => isAscending ? query.ThenBy(m => m.Price) : query.ThenByDescending(m => m.Price),
                "datecreated" or "createdat" => isAscending ? query.ThenBy(m => m.CreatedAt) : query.ThenByDescending(m => m.CreatedAt),
                "stockquantity" or "stock" => isAscending ? query.ThenBy(m => m.StockQuantity) : query.ThenByDescending(m => m.StockQuantity),
                "category" => isAscending ? query.ThenBy(m => m.Category ?? "") : query.ThenByDescending(m => m.Category ?? ""),
                "expirydate" or "expiry" => isAscending 
                    ? query.ThenBy(m => m.ExpiryDate ?? DateTime.MaxValue) 
                    : query.ThenByDescending(m => m.ExpiryDate ?? DateTime.MinValue),
                _ => query
            };
        }

        /// <summary>
        /// Applies sorting to Prescription queries with support for multi-column sorting
        /// Supported sortBy values: dateCreated, createdAt, totalAmount, amount, prescriptionNumber, number, status, prescribedDate
        /// Supports multi-column: sortBy=status,createdAt&amp;sortOrder=asc,desc
        /// </summary>
        public IQueryable<Prescription> ApplySorting(IQueryable<Prescription> query, string? sortBy, string? sortOrder)
        {
            // Default sorting if sortBy is not provided: createdAt desc
            if (string.IsNullOrEmpty(sortBy))
            {
                var defaultOrder = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "desc";
                return defaultOrder ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
            }

            // Check if multi-column sorting format
            var sortColumns = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            if (sortColumns.Length > 1)
            {
                // Multi-column sorting
                return ApplyMultiColumnSortingPrescription(query, sortColumns, sortOrder);
            }
            else
            {
                // Single column sorting
                return ApplySingleColumnSortingPrescription(query, sortBy, sortOrder);
            }
        }

        /// <summary>
        /// Applies single column sorting for Prescriptions
        /// </summary>
        private IQueryable<Prescription> ApplySingleColumnSortingPrescription(IQueryable<Prescription> query, string sortBy, string? sortOrder)
        {
            var isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";
            var isDescending = !string.IsNullOrEmpty(sortOrder) && sortOrder.ToLower() == "desc";

            return sortBy.ToLower() switch
            {
                "datecreated" or "createdat" => isAscending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                "totalamount" or "amount" => isAscending ? query.OrderBy(p => p.TotalAmount) : query.OrderByDescending(p => p.TotalAmount),
                "prescriptionnumber" or "number" => isAscending ? query.OrderBy(p => p.PrescriptionNumber) : query.OrderByDescending(p => p.PrescriptionNumber),
                "status" => isAscending ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
                "prescribeddate" => isAscending ? query.OrderBy(p => p.PrescribedDate) : query.OrderByDescending(p => p.PrescribedDate),
                _ => isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            };
        }

        /// <summary>
        /// Applies multi-column sorting for Prescriptions
        /// </summary>
        private IQueryable<Prescription> ApplyMultiColumnSortingPrescription(IQueryable<Prescription> query, string[] sortColumns, string? sortOrder)
        {
            IOrderedQueryable<Prescription>? orderedQuery = null;
            var sortOrders = string.IsNullOrEmpty(sortOrder) 
                ? new string[sortColumns.Length] 
                : sortOrder.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (int i = 0; i < sortColumns.Length; i++)
            {
                var column = sortColumns[i].Trim();
                var order = i < sortOrders.Length && !string.IsNullOrEmpty(sortOrders[i])
                    ? sortOrders[i].Trim().ToLower()
                    : "asc";

                // Check if order is embedded in column
                if (column.Contains(':'))
                {
                    var parts = column.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        column = parts[0];
                        order = parts[1].ToLower();
                    }
                }

                var isAscending = order == "asc";

                if (orderedQuery == null)
                {
                    orderedQuery = ApplyFirstSortColumnPrescription(query, column, isAscending);
                }
                else
                {
                    orderedQuery = ApplySubsequentSortColumnPrescription(orderedQuery, column, isAscending);
                }
            }

            return orderedQuery ?? query.OrderByDescending(p => p.CreatedAt);
        }

        /// <summary>
        /// Applies the first sort column for Prescriptions
        /// </summary>
        private IOrderedQueryable<Prescription> ApplyFirstSortColumnPrescription(IQueryable<Prescription> query, string column, bool isAscending)
        {
            return column.ToLower() switch
            {
                "datecreated" or "createdat" => isAscending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                "totalamount" or "amount" => isAscending ? query.OrderBy(p => p.TotalAmount) : query.OrderByDescending(p => p.TotalAmount),
                "prescriptionnumber" or "number" => isAscending ? query.OrderBy(p => p.PrescriptionNumber) : query.OrderByDescending(p => p.PrescriptionNumber),
                "status" => isAscending ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
                "prescribeddate" => isAscending ? query.OrderBy(p => p.PrescribedDate) : query.OrderByDescending(p => p.PrescribedDate),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }

        /// <summary>
        /// Applies subsequent sort columns for Prescriptions
        /// </summary>
        private IOrderedQueryable<Prescription> ApplySubsequentSortColumnPrescription(IOrderedQueryable<Prescription> query, string column, bool isAscending)
        {
            return column.ToLower() switch
            {
                "datecreated" or "createdat" => isAscending ? query.ThenBy(p => p.CreatedAt) : query.ThenByDescending(p => p.CreatedAt),
                "totalamount" or "amount" => isAscending ? query.ThenBy(p => p.TotalAmount) : query.ThenByDescending(p => p.TotalAmount),
                "prescriptionnumber" or "number" => isAscending ? query.ThenBy(p => p.PrescriptionNumber) : query.ThenByDescending(p => p.PrescriptionNumber),
                "status" => isAscending ? query.ThenBy(p => p.Status) : query.ThenByDescending(p => p.Status),
                "prescribeddate" => isAscending ? query.ThenBy(p => p.PrescribedDate) : query.ThenByDescending(p => p.PrescribedDate),
                _ => query
            };
        }

        #region DTO-based Overloads

        /// <summary>
        /// Builds a filtered query for medications based on PharmacyQueryParameters DTO
        /// </summary>
        public IQueryable<Medication> GetFilteredMedications(
            IQueryable<Medication> baseQuery,
            PharmacyQueryParameters queryParams,
            bool defaultToActiveOnly = false)
        {
            return BuildFilteredMedicationsQuery(baseQuery, queryParams, defaultToActiveOnly, applyExpiryFilters: true);
        }

        private IQueryable<Medication> BuildFilteredMedicationsQuery(
            IQueryable<Medication> baseQuery,
            PharmacyQueryParameters queryParams,
            bool defaultToActiveOnly,
            bool applyExpiryFilters)
        {
            var query = baseQuery;

            // String filters: Category
            if (!string.IsNullOrEmpty(queryParams.Category))
            {
                var categoryValue = queryParams.Category.ToLower();
                query = query.Where(m => m.Category != null && m.Category.ToLower() == categoryValue);
            }

            // String filters: Search
            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                var searchTerm = queryParams.SearchTerm.ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(searchTerm) ||
                    (m.GenericName != null && m.GenericName.ToLower().Contains(searchTerm)) ||
                    (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(searchTerm))
                );
            }

            // String filters: Stock Status
            if (!string.IsNullOrEmpty(queryParams.StockStatus))
            {
                var status = queryParams.StockStatus.ToLower();
                switch (status)
                {
                    case "low stock":
                        query = query.Where(m => m.StockQuantity > 0 && m.StockQuantity < m.MinimumStockLevel);
                        break;
                    case "out of stock":
                        query = query.Where(m => m.StockQuantity == 0);
                        break;
                    case "normal stock":
                    case "in stock":
                        query = query.Where(m => m.StockQuantity >= m.MinimumStockLevel);
                        break;
                    case "critical stock":
                        query = query.Where(m => m.StockQuantity > 0 && m.StockQuantity < 5);
                        break;
                }
            }

            // String filters: Status
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                var statusValue = queryParams.Status.ToLower();
                if (statusValue == "active")
                {
                    query = query.Where(m => m.IsActive);
                }
                else if (statusValue == "inactive" || statusValue == "discontinued")
                {
                    query = query.Where(m => !m.IsActive);
                }
            }

            // Numeric filters: Price range
            if (queryParams.MinPrice.HasValue)
            {
                query = query.Where(m => m.Price >= queryParams.MinPrice.Value);
            }
            if (queryParams.MaxPrice.HasValue)
            {
                query = query.Where(m => m.Price <= queryParams.MaxPrice.Value);
            }

            // Numeric filters: Stock quantity range
            if (queryParams.MinStock.HasValue)
            {
                query = query.Where(m => m.StockQuantity >= queryParams.MinStock.Value);
            }
            if (queryParams.MaxStock.HasValue)
            {
                query = query.Where(m => m.StockQuantity <= queryParams.MaxStock.Value);
            }

            // Boolean filters
            if (queryParams.RequiresPrescription.HasValue)
            {
                query = query.Where(m => m.RequiresPrescription == queryParams.RequiresPrescription.Value);
            }
            if (queryParams.IsActive.HasValue)
            {
                query = query.Where(m => m.IsActive == queryParams.IsActive.Value);
            }
            else if (defaultToActiveOnly)
            {
                query = query.Where(m => m.IsActive);
            }

            // Date filters: Created date range
            if (queryParams.CreatedAfter.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= queryParams.CreatedAfter.Value);
            }
            if (queryParams.CreatedBefore.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= queryParams.CreatedBefore.Value);
            }

            if (applyExpiryFilters)
            {
                query = ApplyMedicationExpiryFilters(query, queryParams.ExpiryAfter, queryParams.ExpiryBefore);
            }

            return query;
        }

        /// <summary>
        /// Builds a filtered query for prescriptions based on PharmacyQueryParameters DTO
        /// </summary>
        public IQueryable<Prescription> GetFilteredPrescriptions(IQueryable<Prescription> baseQuery, PharmacyQueryParameters queryParams)
        {
            var query = baseQuery;

            // String filters: Status (case-insensitive)
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                var status = queryParams.Status.Trim();
                query = query.Where(p => p.Status.ToLower() == status.ToLower());
            }

            // Search: prescription number, patient name, doctor name, medication name
            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            {
                var term = queryParams.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.PrescriptionNumber.ToLower().Contains(term) ||
                    (p.Patient.FirstName + " " + p.Patient.LastName).ToLower().Contains(term) ||
                    (p.Doctor.FirstName + " " + p.Doctor.LastName).ToLower().Contains(term) ||
                    p.PrescriptionItems.Any(pi =>
                        pi.Medication.Name.ToLower().Contains(term) ||
                        (pi.Medication.GenericName != null && pi.Medication.GenericName.ToLower().Contains(term))));
            }

            // Numeric filters: ID filters
            if (queryParams.PatientId.HasValue)
            {
                query = query.Where(p => p.PatientId == queryParams.PatientId.Value);
            }
            if (queryParams.DoctorId.HasValue)
            {
                query = query.Where(p => p.DoctorId == queryParams.DoctorId.Value);
            }
            if (queryParams.PharmacistId.HasValue)
            {
                query = query.Where(p => p.PharmacistId == queryParams.PharmacistId.Value);
            }

            // Numeric filters: Amount range
            if (queryParams.MinAmount.HasValue)
            {
                query = query.Where(p => p.TotalAmount >= queryParams.MinAmount.Value);
            }
            if (queryParams.MaxAmount.HasValue)
            {
                query = query.Where(p => p.TotalAmount <= queryParams.MaxAmount.Value);
            }

            // Date filters: Prescribed date range
            if (queryParams.PrescribedAfter.HasValue)
            {
                query = query.Where(p => p.PrescribedDate >= queryParams.PrescribedAfter.Value);
            }
            if (queryParams.PrescribedBefore.HasValue)
            {
                query = query.Where(p => p.PrescribedDate <= queryParams.PrescribedBefore.Value);
            }

            // Date filters: Dispensed date range
            if (queryParams.DispensedAfter.HasValue)
            {
                query = query.Where(p => p.DispensedDate.HasValue && p.DispensedDate.Value >= queryParams.DispensedAfter.Value);
            }
            if (queryParams.DispensedBefore.HasValue)
            {
                query = query.Where(p => p.DispensedDate.HasValue && p.DispensedDate.Value <= queryParams.DispensedBefore.Value);
            }

            return query;
        }

        /// <summary>
        /// Builds a filtered query for inventory (medications) based on PharmacyQueryParameters DTO.
        /// Applies inventory-specific expiry rules (e.g. missing expiry counts as "good").
        /// </summary>
        public IQueryable<Medication> GetFilteredInventory(IQueryable<Medication> baseQuery, PharmacyQueryParameters queryParams)
        {
            var query = BuildFilteredMedicationsQuery(baseQuery, queryParams, defaultToActiveOnly: true, applyExpiryFilters: false);
            return ApplyInventoryExpiryFilters(query, queryParams.ExpiryAfter, queryParams.ExpiryBefore);
        }

        private static IQueryable<Medication> ApplyMedicationExpiryFilters(
            IQueryable<Medication> query,
            DateTime? expiryAfter,
            DateTime? expiryBefore)
        {
            if (expiryAfter.HasValue)
            {
                var afterDate = expiryAfter.Value.Date;
                query = query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date >= afterDate);
            }

            if (expiryBefore.HasValue)
            {
                var beforeDate = expiryBefore.Value.Date;
                query = query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date <= beforeDate);
            }

            return query;
        }

        private static IQueryable<Medication> ApplyInventoryExpiryFilters(
            IQueryable<Medication> query,
            DateTime? expiryAfter,
            DateTime? expiryBefore)
        {
            if (expiryAfter.HasValue)
            {
                var afterDate = expiryAfter.Value.Date;
                if (!expiryBefore.HasValue)
                {
                    query = query.Where(m => !m.ExpiryDate.HasValue || m.ExpiryDate.Value.Date >= afterDate);
                }
                else
                {
                    query = query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date >= afterDate);
                }
            }

            if (expiryBefore.HasValue)
            {
                var beforeDate = expiryBefore.Value.Date;
                query = query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value.Date <= beforeDate);
            }

            return query;
        }

        #endregion

        private static PharmacyQueryParameters BindFromQueryCollection(IQueryCollection queryParams)
        {
            var parameters = new PharmacyQueryParameters();

            if (TryParseInt(queryParams, "pageNumber", out var pageNumber))
            {
                parameters.PageNumber = pageNumber;
            }
            else if (TryParseInt(queryParams, "page", out var page) && page != 1)
            {
                parameters.PageNumber = page;
            }

            if (TryParseInt(queryParams, "pageSize", out var pageSize))
            {
                parameters.PageSize = pageSize;
            }

            parameters.SortBy = GetQueryParamValue(queryParams, "sortBy");
            parameters.SortOrder = GetQueryParamValue(queryParams, "sortOrder") ?? parameters.SortOrder;
            parameters.SearchTerm = GetQueryParamValue(queryParams, "searchTerm", "search");
            parameters.Category = GetQueryParamValue(queryParams, "category");
            parameters.Status = GetQueryParamValue(queryParams, "status");
            parameters.StockStatus = GetQueryParamValue(queryParams, "stockStatus");

            if (TryParseDecimal(queryParams, "minPrice", out var minPrice))
            {
                parameters.MinPrice = minPrice;
            }
            if (TryParseDecimal(queryParams, "maxPrice", out var maxPrice))
            {
                parameters.MaxPrice = maxPrice;
            }
            if (TryParseInt(queryParams, "minStock", out var minStock))
            {
                parameters.MinStock = minStock;
            }
            if (TryParseInt(queryParams, "maxStock", out var maxStock))
            {
                parameters.MaxStock = maxStock;
            }
            if (TryParseBool(queryParams, "requiresPrescription", out var requiresPrescription))
            {
                parameters.RequiresPrescription = requiresPrescription;
            }
            if (TryParseBool(queryParams, "isActive", out var isActive))
            {
                parameters.IsActive = isActive;
            }
            if (TryParseDateTime(queryParams, "createdAfter", out var createdAfter))
            {
                parameters.CreatedAfter = createdAfter;
            }
            if (TryParseDateTime(queryParams, "createdBefore", out var createdBefore))
            {
                parameters.CreatedBefore = createdBefore;
            }
            if (TryParseDateTime(queryParams, "expiryAfter", out var expiryAfter))
            {
                parameters.ExpiryAfter = expiryAfter;
            }
            if (TryParseDateTime(queryParams, "expiryBefore", out var expiryBefore))
            {
                parameters.ExpiryBefore = expiryBefore;
            }
            if (TryParseInt(queryParams, "patientId", out var patientId))
            {
                parameters.PatientId = patientId;
            }
            if (TryParseInt(queryParams, "doctorId", out var doctorId))
            {
                parameters.DoctorId = doctorId;
            }
            if (TryParseInt(queryParams, "pharmacistId", out var pharmacistId))
            {
                parameters.PharmacistId = pharmacistId;
            }
            if (TryParseDecimal(queryParams, "minAmount", out var minAmount))
            {
                parameters.MinAmount = minAmount;
            }
            if (TryParseDecimal(queryParams, "maxAmount", out var maxAmount))
            {
                parameters.MaxAmount = maxAmount;
            }
            if (TryParseDateTime(queryParams, "prescribedAfter", out var prescribedAfter))
            {
                parameters.PrescribedAfter = prescribedAfter;
            }
            if (TryParseDateTime(queryParams, "prescribedBefore", out var prescribedBefore))
            {
                parameters.PrescribedBefore = prescribedBefore;
            }
            if (TryParseDateTime(queryParams, "dispensedAfter", out var dispensedAfter))
            {
                parameters.DispensedAfter = dispensedAfter;
            }
            if (TryParseDateTime(queryParams, "dispensedBefore", out var dispensedBefore))
            {
                parameters.DispensedBefore = dispensedBefore;
            }

            return parameters;
        }

        private static string? GetQueryParamValue(IQueryCollection queryParams, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (queryParams.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                {
                    return value.ToString();
                }
            }

            return null;
        }

        private static bool TryParseInt(IQueryCollection queryParams, string key, out int value)
        {
            value = default;
            return queryParams.TryGetValue(key, out var raw) && int.TryParse(raw, out value);
        }

        private static bool TryParseDecimal(IQueryCollection queryParams, string key, out decimal value)
        {
            value = default;
            return queryParams.TryGetValue(key, out var raw) && decimal.TryParse(raw, out value);
        }

        private static bool TryParseBool(IQueryCollection queryParams, string key, out bool value)
        {
            value = default;
            return queryParams.TryGetValue(key, out var raw) && bool.TryParse(raw, out value);
        }

        private static bool TryParseDateTime(IQueryCollection queryParams, string key, out DateTime value)
        {
            value = default;
            return queryParams.TryGetValue(key, out var raw) && DateTime.TryParse(raw, out value);
        }
    }
}
