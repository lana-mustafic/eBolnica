using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Services.Pharmacy;
using System;
using System.Collections.Generic;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationCsvRowValidatorUnitTests
    {
        private static readonly Dictionary<string, int> ColumnIndexes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = 0,
            ["Generic Name"] = 1,
            ["Category"] = 2,
            ["Manufacturer"] = 3,
            ["Description"] = 4,
            ["Price"] = 5,
            ["Stock Quantity"] = 6,
            ["Minimum Stock Level"] = 7,
            ["Expiry Date"] = 8,
            ["Batch Number"] = 9,
            ["Dosage Form"] = 10,
            ["Strength"] = 11,
            ["Requires Prescription"] = 12,
            ["Active"] = 13
        };

        [Fact]
        public void TryValidateRow_ValidCells_ReturnsMedicationCreateDto()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var cells = new[]
            {
                "Import Med", "", "Vitamins", "", "", "12.50", "25", "5", expiry, "", "", "", "No", "Yes"
            };

            var valid = MedicationCsvRowValidator.TryValidateRow(2, cells, ColumnIndexes, out var dto, out var errors);

            Assert.True(valid);
            Assert.NotNull(dto);
            Assert.Empty(errors);
            Assert.Equal("Import Med", dto!.Name);
            Assert.Equal(12.50m, dto.Price);
            Assert.False(dto.RequiresPrescription);
            Assert.True(dto.IsActive);
        }

        [Fact]
        public void TryValidateRow_ShortName_UsesMedicationCreateDtoRuleMessage()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var cells = new[]
            {
                "AB", "", "Vitamins", "", "", "12.50", "25", "5", expiry, "", "", "", "No", "Yes"
            };

            var valid = MedicationCsvRowValidator.TryValidateRow(2, cells, ColumnIndexes, out _, out var errors);

            Assert.False(valid);
            Assert.Contains(errors, e =>
                e.Field == "Name"
                && e.Reason == "Name must be between 3 and 100 characters");
        }

        [Fact]
        public void TryValidateRow_PastExpiry_UsesMedicationCreateDtoRuleMessage()
        {
            var cells = new[]
            {
                "Import Med", "", "Vitamins", "", "", "12.50", "25", "5", "2020-01-01", "", "", "", "No", "Yes"
            };

            var valid = MedicationCsvRowValidator.TryValidateRow(2, cells, ColumnIndexes, out _, out var errors);

            Assert.False(valid);
            Assert.Contains(errors, e =>
                e.Field == "Expiry Date"
                && e.Reason == "Expiry date must be in the future.");
        }

        [Fact]
        public void TryValidateRow_InvalidPriceFormat_ReturnsParseError()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var cells = new[]
            {
                "Import Med", "", "Vitamins", "", "", "not-a-price", "25", "5", expiry, "", "", "", "No", "Yes"
            };

            var valid = MedicationCsvRowValidator.TryValidateRow(2, cells, ColumnIndexes, out _, out var errors);

            Assert.False(valid);
            Assert.Contains(errors, e => e.Field == "Price" && e.Reason == "Price must be a valid number.");
        }

        [Fact]
        public void TryValidateRow_InvalidYesNo_ReturnsParseError()
        {
            var expiry = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
            var cells = new[]
            {
                "Import Med", "", "Vitamins", "", "", "12.50", "25", "5", expiry, "", "", "", "Maybe", "Yes"
            };

            var valid = MedicationCsvRowValidator.TryValidateRow(2, cells, ColumnIndexes, out _, out var errors);

            Assert.False(valid);
            Assert.Contains(errors, e =>
                e.Field == "Requires Prescription"
                && e.Reason == "Requires Prescription must be Yes or No.");
        }
    }
}
