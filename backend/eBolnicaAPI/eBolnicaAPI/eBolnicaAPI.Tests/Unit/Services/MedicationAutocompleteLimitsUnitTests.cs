using eBolnicaAPI.Services.Pharmacy;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAutocompleteLimitsUnitTests
    {
        [Theory]
        [InlineData("", false)]
        [InlineData("a", false)]
        [InlineData(" a ", false)]
        [InlineData("ab", true)]
        [InlineData("  aspirin  ", true)]
        public void IsQueryLongEnough_RequiresAtLeastTwoTrimmedCharacters(string query, bool expected)
        {
            Assert.Equal(expected, MedicationAutocompleteLimits.IsQueryLongEnough(query));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        [InlineData(25, 10)]
        [InlineData(-5, 1)]
        public void CapSuggestionLimit_ClampsToOneThroughTen(int input, int expected)
        {
            Assert.Equal(expected, MedicationAutocompleteLimits.CapSuggestionLimit(input));
        }
    }
}
