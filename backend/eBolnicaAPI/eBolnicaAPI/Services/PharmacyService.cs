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
        /// Builds a filtered query for medications based on query parameters
        /// Supports: category, status, minPrice, maxPrice, stockStatus, requiresPrescription, isActive, 
        /// minStock, maxStock, search, createdAfter, createdBefore, expiryAfter, expiryBefore
        /// </summary>
        public IQueryable<Medication> GetFilteredMedications(IQueryable<Medication> baseQuery, IQueryCollection queryParams)
        {
            var query = baseQuery;

            // String filters: Category (exact match, case-insensitive)
            if (queryParams.ContainsKey("category") && !string.IsNullOrEmpty(queryParams["category"]))
            {
                var categoryValue = queryParams["category"].ToString().ToLower();
                query = query.Where(m => m.Category != null && m.Category.ToLower() == categoryValue);
            }

            // String filters: Search (across Name, GenericName, and Manufacturer - case-insensitive)
            if (queryParams.ContainsKey("search") && !string.IsNullOrEmpty(queryParams["search"]))
            {
                var searchTerm = queryParams["search"].ToString().ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(searchTerm) ||
                    (m.GenericName != null && m.GenericName.ToLower().Contains(searchTerm)) ||
                    (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(searchTerm))
                );
            }

            // String filters: Stock Status (special filter)
            if (queryParams.ContainsKey("stockStatus") && !string.IsNullOrEmpty(queryParams["stockStatus"]))
            {
                var status = queryParams["stockStatus"].ToString().ToLower();
                switch (status)
                {
                    case "low stock":
                        query = query.Where(m => m.StockQuantity < m.MinimumStockLevel && m.StockQuantity > 0);
                        break;
                    case "out of stock":
                        query = query.Where(m => m.StockQuantity == 0);
                        break;
                    case "normal stock":
                        query = query.Where(m => m.StockQuantity >= m.MinimumStockLevel);
                        break;
                    // If invalid stockStatus, ignore the filter
                }
            }

            // String filters: Status (active/inactive)
            if (queryParams.ContainsKey("status") && !string.IsNullOrEmpty(queryParams["status"]))
            {
                var statusValue = queryParams["status"].ToString().ToLower();
                if (statusValue == "active")
                {
                    query = query.Where(m => m.IsActive);
                }
                else if (statusValue == "inactive")
                {
                    query = query.Where(m => !m.IsActive);
                }
            }

            // Numeric filters: Price range
            if (queryParams.ContainsKey("minPrice") && decimal.TryParse(queryParams["minPrice"], out decimal minPrice))
            {
                query = query.Where(m => m.Price >= minPrice);
            }
            if (queryParams.ContainsKey("maxPrice") && decimal.TryParse(queryParams["maxPrice"], out decimal maxPrice))
            {
                query = query.Where(m => m.Price <= maxPrice);
            }

            // Numeric filters: Stock quantity range
            if (queryParams.ContainsKey("minStock") && int.TryParse(queryParams["minStock"], out int minStock))
            {
                query = query.Where(m => m.StockQuantity >= minStock);
            }
            if (queryParams.ContainsKey("maxStock") && int.TryParse(queryParams["maxStock"], out int maxStock))
            {
                query = query.Where(m => m.StockQuantity <= maxStock);
            }

            // Boolean filters: Requires Prescription
            if (queryParams.ContainsKey("requiresPrescription") && bool.TryParse(queryParams["requiresPrescription"], out bool requiresPrescription))
            {
                query = query.Where(m => m.RequiresPrescription == requiresPrescription);
            }

            // Boolean filters: Is Active
            if (queryParams.ContainsKey("isActive") && bool.TryParse(queryParams["isActive"], out bool isActive))
            {
                query = query.Where(m => m.IsActive == isActive);
            }

            // Date filters: Created date range
            if (queryParams.ContainsKey("createdAfter") && DateTime.TryParse(queryParams["createdAfter"], out DateTime createdAfter))
            {
                query = query.Where(m => m.CreatedAt >= createdAfter);
            }
            if (queryParams.ContainsKey("createdBefore") && DateTime.TryParse(queryParams["createdBefore"], out DateTime createdBefore))
            {
                query = query.Where(m => m.CreatedAt <= createdBefore);
            }

            // Date filters: Expiry date range
            if (queryParams.ContainsKey("expiryAfter") && DateTime.TryParse(queryParams["expiryAfter"], out DateTime expiryAfter))
            {
                query = query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value >= expiryAfter);
            }
            if (queryParams.ContainsKey("expiryBefore") && DateTime.TryParse(queryParams["expiryBefore"], out DateTime expiryBefore))
            {
                query = query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= expiryBefore);
            }

            return query;
        }

        /// <summary>
        /// Builds a filtered query for prescriptions based on query parameters
        /// Supports: status, patientId, doctorId, pharmacistId, minAmount, maxAmount, 
        /// prescribedAfter, prescribedBefore, dispensedAfter, dispensedBefore
        /// </summary>
        public IQueryable<Prescription> GetFilteredPrescriptions(IQueryable<Prescription> baseQuery, IQueryCollection queryParams)
        {
            var query = baseQuery;

            // String filters: Status (exact match)
            if (queryParams.ContainsKey("status") && !string.IsNullOrEmpty(queryParams["status"]))
            {
                var statusValue = queryParams["status"].ToString();
                query = query.Where(p => p.Status == statusValue);
            }

            // Numeric filters: ID filters
            if (queryParams.ContainsKey("patientId") && int.TryParse(queryParams["patientId"], out int patientId))
            {
                query = query.Where(p => p.PatientId == patientId);
            }
            if (queryParams.ContainsKey("doctorId") && int.TryParse(queryParams["doctorId"], out int doctorId))
            {
                query = query.Where(p => p.DoctorId == doctorId);
            }
            if (queryParams.ContainsKey("pharmacistId") && int.TryParse(queryParams["pharmacistId"], out int pharmacistId))
            {
                query = query.Where(p => p.PharmacistId == pharmacistId);
            }

            // Numeric filters: Amount range
            if (queryParams.ContainsKey("minAmount") && decimal.TryParse(queryParams["minAmount"], out decimal minAmount))
            {
                query = query.Where(p => p.TotalAmount >= minAmount);
            }
            if (queryParams.ContainsKey("maxAmount") && decimal.TryParse(queryParams["maxAmount"], out decimal maxAmount))
            {
                query = query.Where(p => p.TotalAmount <= maxAmount);
            }

            // Date filters: Prescribed date range
            if (queryParams.ContainsKey("prescribedAfter") && DateTime.TryParse(queryParams["prescribedAfter"], out DateTime prescribedAfter))
            {
                query = query.Where(p => p.PrescribedDate >= prescribedAfter);
            }
            if (queryParams.ContainsKey("prescribedBefore") && DateTime.TryParse(queryParams["prescribedBefore"], out DateTime prescribedBefore))
            {
                query = query.Where(p => p.PrescribedDate <= prescribedBefore);
            }

            // Date filters: Dispensed date range
            if (queryParams.ContainsKey("dispensedAfter") && DateTime.TryParse(queryParams["dispensedAfter"], out DateTime dispensedAfter))
            {
                query = query.Where(p => p.DispensedDate.HasValue && p.DispensedDate.Value >= dispensedAfter);
            }
            if (queryParams.ContainsKey("dispensedBefore") && DateTime.TryParse(queryParams["dispensedBefore"], out DateTime dispensedBefore))
            {
                query = query.Where(p => p.DispensedDate.HasValue && p.DispensedDate.Value <= dispensedBefore);
            }

            return query;
        }

        /// <summary>
        /// Builds a filtered query for inventory (medications) based on query parameters
        /// Similar to GetFilteredMedications but specifically for inventory endpoints
        /// </summary>
        public IQueryable<Medication> GetFilteredInventory(IQueryable<Medication> baseQuery, IQueryCollection queryParams)
        {
            // Inventory uses the same filtering logic as medications
            return GetFilteredMedications(baseQuery, queryParams);
        }

        /// <summary>
        /// Applies sorting to Medication queries
        /// Supported sortBy values: name, price, dateCreated, createdAt, stockQuantity, stock, category, expiryDate
        /// Supports sortOrder: "asc" or "desc" (default: "desc")
        /// </summary>
        public IQueryable<Medication> ApplySorting(IQueryable<Medication> query, string? sortBy, string? sortOrder)
        {
            // Normalize sortOrder
            var isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";
            var isDescending = !string.IsNullOrEmpty(sortOrder) && sortOrder.ToLower() == "desc";

            // Default sorting if sortBy is not provided: createdAt desc
            if (string.IsNullOrEmpty(sortBy))
            {
                return isDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt);
            }

            // Apply sorting based on sortBy parameter
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
                    // Default to CreatedAt desc if invalid sortBy
                    return isDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt);
            }
        }

        /// <summary>
        /// Applies sorting to Prescription queries
        /// Supported sortBy values: dateCreated, createdAt, totalAmount, amount, prescriptionNumber, number, status, prescribedDate
        /// Supports sortOrder: "asc" or "desc" (default: "desc")
        /// </summary>
        public IQueryable<Prescription> ApplySorting(IQueryable<Prescription> query, string? sortBy, string? sortOrder)
        {
            // Normalize sortOrder
            var isAscending = string.IsNullOrEmpty(sortOrder) || sortOrder.ToLower() == "asc";
            var isDescending = !string.IsNullOrEmpty(sortOrder) && sortOrder.ToLower() == "desc";

            // Default sorting if sortBy is not provided: createdAt desc
            if (string.IsNullOrEmpty(sortBy))
            {
                return isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
            }

            // Apply sorting based on sortBy parameter
            switch (sortBy.ToLower())
            {
                case "datecreated":
                case "createdat":
                    return isAscending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt);
                
                case "totalamount":
                case "amount":
                    return isAscending ? query.OrderBy(p => p.TotalAmount) : query.OrderByDescending(p => p.TotalAmount);
                
                case "prescriptionnumber":
                case "number":
                    return isAscending ? query.OrderBy(p => p.PrescriptionNumber) : query.OrderByDescending(p => p.PrescriptionNumber);
                
                case "status":
                    return isAscending ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status);
                
                case "prescribeddate":
                    return isAscending ? query.OrderBy(p => p.PrescribedDate) : query.OrderByDescending(p => p.PrescribedDate);
                
                default:
                    // Default to CreatedAt desc if invalid sortBy
                    return isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
            }
        }

        #region DTO-based Overloads

        /// <summary>
        /// Builds a filtered query for medications based on PharmacyQueryParameters DTO
        /// </summary>
        public IQueryable<Medication> GetFilteredMedications(IQueryable<Medication> baseQuery, PharmacyQueryParameters queryParams)
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
                        query = query.Where(m => m.StockQuantity < m.MinimumStockLevel && m.StockQuantity > 0);
                        break;
                    case "out of stock":
                        query = query.Where(m => m.StockQuantity == 0);
                        break;
                    case "normal stock":
                    case "in stock":
                        query = query.Where(m => m.StockQuantity >= m.MinimumStockLevel);
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

            // Date filters: Created date range
            if (queryParams.CreatedAfter.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= queryParams.CreatedAfter.Value);
            }
            if (queryParams.CreatedBefore.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= queryParams.CreatedBefore.Value);
            }

            // Date filters: Expiry date range
            if (queryParams.ExpiryAfter.HasValue)
            {
                query = query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value >= queryParams.ExpiryAfter.Value);
            }
            if (queryParams.ExpiryBefore.HasValue)
            {
                query = query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value <= queryParams.ExpiryBefore.Value);
            }

            return query;
        }

        /// <summary>
        /// Builds a filtered query for prescriptions based on PharmacyQueryParameters DTO
        /// </summary>
        public IQueryable<Prescription> GetFilteredPrescriptions(IQueryable<Prescription> baseQuery, PharmacyQueryParameters queryParams)
        {
            var query = baseQuery;

            // String filters: Status
            if (!string.IsNullOrEmpty(queryParams.Status))
            {
                query = query.Where(p => p.Status == queryParams.Status);
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
        /// Builds a filtered query for inventory (medications) based on PharmacyQueryParameters DTO
        /// </summary>
        public IQueryable<Medication> GetFilteredInventory(IQueryable<Medication> baseQuery, PharmacyQueryParameters queryParams)
        {
            // Inventory uses the same filtering logic as medications
            return GetFilteredMedications(baseQuery, queryParams);
        }

        #endregion
    }
}
