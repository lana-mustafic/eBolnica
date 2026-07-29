using eBolnicaAPI.Services.Pharmacy;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class CsvParserUnitTests
    {
        [Fact]
        public void Parse_HandlesQuotedCommas()
        {
            var rows = CsvParser.Parse("Name,Description\nMed,\"Pain, fever\"");

            Assert.Equal(2, rows.Count);
            Assert.Equal("Med", rows[1][0]);
            Assert.Equal("Pain, fever", rows[1][1]);
        }

        [Fact]
        public void Parse_HandlesEscapedQuotes()
        {
            var rows = CsvParser.Parse("Name\n" + "\"Say \"\"Hi\"\"\"");

            Assert.Equal("Say \"Hi\"", rows[1][0]);
        }

        [Fact]
        public void Parse_UnclosedQuote_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => CsvParser.Parse("Name\n\"open"));
        }
    }
}
