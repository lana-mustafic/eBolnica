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
        public static IReadOnlyList<ValidationResult> Validate(PharmacyQueryParameters parameters)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(
                parameters,
                new ValidationContext(parameters),
                results,
                validateAllProperties: true);
            return results;
        }
    }
}
