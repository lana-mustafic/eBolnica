using System.ComponentModel.DataAnnotations;
using eBolnicaAPI.Models.DTOs;

namespace eBolnicaAPI.Services
{
    /// <summary>
    /// Validates pharmacy list query parameters (filter combinations and field constraints).
    /// Pagination bounds are normalized in the controller; not validated here.
    /// </summary>
    public static class PharmacyQueryParameterValidator
    {
        public static IReadOnlyList<ValidationResult> Validate(
            PharmacyQueryParameters parameters,
            PharmacyListEndpoint? endpoint = null)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(
                parameters,
                new ValidationContext(parameters),
                results,
                validateAllProperties: true);

            if (endpoint.HasValue)
            {
                results.AddRange(PharmacySortValidator.Validate(parameters, endpoint.Value));
            }

            return results;
        }
    }
}
