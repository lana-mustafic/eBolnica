using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Services;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Models
{
    public class PharmacySortValidatorTests
    {
        [Theory]
        [InlineData("name")]
        [InlineData("price")]
        [InlineData("createdAt")]
        [InlineData("stockQuantity")]
        [InlineData("stock")]
        [InlineData("category")]
        [InlineData("expiryDate")]
        public void Validate_Medications_AllowsSupportedColumns(string sortBy)
        {
            var parameters = new PharmacyQueryParameters { SortBy = sortBy, SortOrder = "asc" };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Medications);

            Assert.Empty(results);
        }

        [Theory]
        [InlineData("name")]
        [InlineData("expiryDate")]
        [InlineData("stock")]
        public void Validate_Inventory_AllowsSupportedColumns(string sortBy)
        {
            var parameters = new PharmacyQueryParameters { SortBy = sortBy, SortOrder = "desc" };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Inventory);

            Assert.Empty(results);
        }

        [Theory]
        [InlineData("createdAt")]
        [InlineData("prescribedDate")]
        [InlineData("totalAmount")]
        [InlineData("status")]
        public void Validate_Prescriptions_AllowsSupportedColumns(string sortBy)
        {
            var parameters = new PharmacyQueryParameters { SortBy = sortBy, SortOrder = "desc" };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Prescriptions);

            Assert.Empty(results);
        }

        [Fact]
        public void Validate_Medications_UnknownSortColumn_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                SortBy = "invalidColumn",
                SortOrder = "asc"
            };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Medications);

            Assert.Single(results);
            Assert.Contains("Unknown sort column", results[0].ErrorMessage);
            Assert.Contains("invalidColumn", results[0].ErrorMessage);
        }

        [Fact]
        public void Validate_Prescriptions_UnknownSortColumn_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                SortBy = "patientName",
                SortOrder = "asc"
            };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Prescriptions);

            Assert.Single(results);
            Assert.Contains("Unknown sort column", results[0].ErrorMessage);
            Assert.Contains("patientName", results[0].ErrorMessage);
        }

        [Fact]
        public void Validate_Inventory_UnknownSortColumn_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                SortBy = "supplier",
                SortOrder = "asc"
            };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Inventory);

            Assert.Single(results);
            Assert.Contains("Unknown sort column", results[0].ErrorMessage);
            Assert.Contains("supplier", results[0].ErrorMessage);
        }

        [Fact]
        public void Validate_MultiColumnSort_FlagsUnknownColumn()
        {
            var parameters = new PharmacyQueryParameters
            {
                SortBy = "name,invalidColumn",
                SortOrder = "asc,desc"
            };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Medications);

            Assert.Single(results);
            Assert.Contains("invalidColumn", results[0].ErrorMessage);
        }

        [Fact]
        public void Validate_EmbeddedOrderFormat_ParsesColumnName()
        {
            var parameters = new PharmacyQueryParameters
            {
                SortBy = "name:asc,invalidColumn:desc",
                SortOrder = "asc"
            };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Inventory);

            Assert.Single(results);
            Assert.Contains("invalidColumn", results[0].ErrorMessage);
        }

        [Fact]
        public void Validate_EmptySortBy_ReturnsNoErrors()
        {
            var parameters = new PharmacyQueryParameters { SortBy = null, SortOrder = "desc" };

            var results = PharmacySortValidator.Validate(parameters, PharmacyListEndpoint.Medications);

            Assert.Empty(results);
        }

        [Fact]
        public void ValidateThroughQueryParameterValidator_ReturnsSortErrorForMedications()
        {
            var parameters = new PharmacyQueryParameters
            {
                SortBy = "notARealColumn",
                SortOrder = "asc"
            };

            var results = PharmacyQueryParameterValidator.Validate(parameters, PharmacyListEndpoint.Medications);

            Assert.Contains(results, result =>
                result.ErrorMessage != null &&
                result.ErrorMessage.Contains("Unknown sort column"));
        }
    }
}
