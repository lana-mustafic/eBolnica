using eBolnica.Application.Modules.Pharmacy;
using FluentValidation;
using Xunit;

namespace eBolnica.Tests.Pharmacy;

public class PharmacySortValidatorTests
{
    [Fact]
    public void AllowedMedicationSortColumn_DoesNotThrow()
    {
        var exception = Record.Exception(() => PharmacySortValidator.ValidateMedicationSort("name"));

        Assert.Null(exception);
    }

    [Fact]
    public void UnknownMedicationSortColumn_ThrowsValidationException()
    {
        var exception = Assert.Throws<ValidationException>(
            () => PharmacySortValidator.ValidateMedicationSort("color"));

        Assert.Contains("Unknown medication sort column", exception.Message);
    }
}
