using System.Text;

namespace eBolnicaAPI.Services.Pharmacy
{
    /// <summary>
    /// Minimal RFC 4180-style CSV parser for medication import/export.
    /// </summary>
    public static class CsvParser
    {
        public static IReadOnlyList<string[]> Parse(string content)
        {
            if (content.Length > 0 && content[0] == '\uFEFF')
            {
                content = content[1..];
            }

            var rows = new List<string[]>();
            var currentField = new StringBuilder();
            var currentRow = new List<string>();
            var inQuotes = false;

            for (var i = 0; i < content.Length; i++)
            {
                var ch = content[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(ch);
                    }

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        rows.Add(currentRow.ToArray());
                        currentRow.Clear();
                        break;
                    default:
                        currentField.Append(ch);
                        break;
                }
            }

            if (inQuotes)
            {
                throw new FormatException("CSV contains an unclosed quoted field.");
            }

            if (currentField.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentField.ToString());
                rows.Add(currentRow.ToArray());
            }

            return rows;
        }
    }
}
