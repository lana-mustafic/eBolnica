using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;

namespace eBolnicaAPI.Services
{
    /// <summary>
    /// Service interface for Pharmacy module query building and filtering
    /// </summary>
    public interface IPharmacyService
    {
        /// <summary>
        /// Builds a filtered query for medications based on query parameters
        /// </summary>
        IQueryable<Medication> GetFilteredMedications(IQueryable<Medication> baseQuery, IQueryCollection queryParams);

        /// <summary>
        /// Builds a filtered query for prescriptions based on query parameters
        /// </summary>
        IQueryable<Prescription> GetFilteredPrescriptions(IQueryable<Prescription> baseQuery, IQueryCollection queryParams);

        /// <summary>
        /// Builds a filtered query for inventory (medications) based on query parameters
        /// </summary>
        IQueryable<Medication> GetFilteredInventory(IQueryable<Medication> baseQuery, IQueryCollection queryParams);

        /// <summary>
        /// Applies sorting to Medication queries
        /// </summary>
        IQueryable<Medication> ApplySorting(IQueryable<Medication> query, string? sortBy, string? sortOrder);

        /// <summary>
        /// Applies sorting to Prescription queries
        /// </summary>
        IQueryable<Prescription> ApplySorting(IQueryable<Prescription> query, string? sortBy, string? sortOrder);
    }
}
