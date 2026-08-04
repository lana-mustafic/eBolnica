using System.Text.RegularExpressions;

namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    internal static partial class MedicationAiSummarySecretSanitizer
    {
        [GeneratedRegex(@"(Bearer\s+)[^\s""']+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex BearerTokenPattern();

        [GeneratedRegex(@"\bsk-[A-Za-z0-9_-]+\b", RegexOptions.CultureInvariant)]
        private static partial Regex OpenAiKeyPattern();

        [GeneratedRegex(@"(api[-_ ]?key\s*[:=]\s*)[^\s""',}]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ApiKeyAssignmentPattern();

        public static string SanitizeForLogs(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value ?? string.Empty;
            }

            var sanitized = value;
            sanitized = BearerTokenPattern().Replace(sanitized, "$1***");
            sanitized = OpenAiKeyPattern().Replace(sanitized, "sk-***");
            sanitized = ApiKeyAssignmentPattern().Replace(sanitized, "$1***");
            return sanitized;
        }

        public static bool ContainsPotentialSecret(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return BearerTokenPattern().IsMatch(value)
                || OpenAiKeyPattern().IsMatch(value)
                || ApiKeyAssignmentPattern().IsMatch(value);
        }
    }
}
