using eBolnica.Application.Modules.Pharmacy.Medications.Commands.CreateMedication;
using FluentValidation.TestHelper;
using Xunit;

namespace eBolnica.Tests.Pharmacy;

public class CreateMedicationCommandValidatorTests
{
    private readonly CreateMedicationCommandValidator _validator = new();

    [Fact]
    public void InvalidMedication_FailsNamePriceAndExpiryRules()
    {
        var command = new CreateMedicationCommand
        {
            Name = "ab",
            Category = "",
            Price = 0,
            StockQuantity = 10,
            MinimumStockLevel = 2,
            ExpiryDate = DateTime.UtcNow.Date.AddDays(-1),
        };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Category);
        result.ShouldHaveValidationErrorFor(x => x.Price);
        result.ShouldHaveValidationErrorFor(x => x.ExpiryDate);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidMedication_PassesValidation()
    {
        var command = new CreateMedicationCommand
        {
            Name = "Amoksicilin",
            GenericName = "Amoxicillin",
            Manufacturer = "Bosnalijek",
            Category = "Antibiotic",
            Price = 12.50m,
            StockQuantity = 40,
            MinimumStockLevel = 10,
            ExpiryDate = DateTime.UtcNow.Date.AddYears(1),
            DosageForm = "Capsule",
            Strength = "500mg",
        };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
        Assert.True(result.IsValid);
    }
}
