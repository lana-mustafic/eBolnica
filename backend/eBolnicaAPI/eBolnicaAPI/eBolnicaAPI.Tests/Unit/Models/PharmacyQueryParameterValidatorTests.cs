using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Models
{
    public class PharmacyQueryParameterValidatorTests
    {
        [Fact]
        public void Validate_MinPriceGreaterThanMaxPrice_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                MinPrice = 50,
                MaxPrice = 10
            };

            var results = PharmacyQueryParameterValidator.Validate(parameters);

            Assert.Contains(results, r =>
                r.ErrorMessage == "MinPrice must be less than or equal to MaxPrice");
        }

        [Fact]
        public void Validate_MinAmountGreaterThanMaxAmount_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                MinAmount = 100,
                MaxAmount = 20
            };

            var results = PharmacyQueryParameterValidator.Validate(parameters);

            Assert.Contains(results, r =>
                r.ErrorMessage == "MinAmount must be less than or equal to MaxAmount");
        }

        [Fact]
        public void Validate_MinStockGreaterThanMaxStock_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                MinStock = 100,
                MaxStock = 10
            };

            var results = PharmacyQueryParameterValidator.Validate(parameters);

            Assert.Contains(results, r =>
                r.ErrorMessage == "MinStock must be less than or equal to MaxStock");
        }

        [Fact]
        public void Validate_InvalidStockStatus_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                StockStatus = "invalid status"
            };

            var results = PharmacyQueryParameterValidator.Validate(parameters);

            Assert.Contains(results, r =>
                r.ErrorMessage != null &&
                r.ErrorMessage.Contains("StockStatus must be one of"));
        }

        [Fact]
        public void Validate_NegativeMinPrice_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                MinPrice = -5
            };

            var results = PharmacyQueryParameterValidator.Validate(parameters);

            Assert.Contains(results, r =>
                r.ErrorMessage == "MinPrice must be greater than or equal to 0");
        }

        [Fact]
        public void Validate_ValidPriceRange_ReturnsNoErrors()
        {
            var parameters = new PharmacyQueryParameters
            {
                MinPrice = 10,
                MaxPrice = 50,
                StockStatus = "low stock"
            };

            var results = PharmacyQueryParameterValidator.Validate(parameters);

            Assert.Empty(results);
        }

        [Fact]
        public void Validate_CreatedAfterAfterCreatedBefore_ReturnsError()
        {
            var parameters = new PharmacyQueryParameters
            {
                CreatedAfter = new DateTime(2025, 6, 1),
                CreatedBefore = new DateTime(2025, 1, 1)
            };

            var results = PharmacyQueryParameterValidator.Validate(parameters);

            Assert.Contains(results, r =>
                r.ErrorMessage == "CreatedAfter must be less than or equal to CreatedBefore");
        }
    }
}
