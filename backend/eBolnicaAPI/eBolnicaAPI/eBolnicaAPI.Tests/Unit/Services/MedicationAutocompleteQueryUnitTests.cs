using eBolnicaAPI.Services.Pharmacy;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAutocompleteQueryUnitTests
    {
        [Theory]
        [InlineData("  Aspirin  ", "aspirin")]
        [InlineData("IBUPROFEN", "ibuprofen")]
        public void NormalizeSearchTerm_TrimsAndLowerCases(string input, string expected)
        {
            Assert.Equal(expected, MedicationAutocompleteQuery.NormalizeSearchTerm(input));
        }
    }
}
