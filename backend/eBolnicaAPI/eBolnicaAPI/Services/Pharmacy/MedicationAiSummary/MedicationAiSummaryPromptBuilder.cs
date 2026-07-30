using System.Globalization;
using System.Text;
using eBolnicaAPI.Models.Entities;

namespace eBolnicaAPI.Services.Pharmacy.MedicationAiSummary
{
    public static class MedicationAiSummaryPromptBuilder
    {
        public const string SystemPrompt =
            """
            You are a pharmacy inventory assistant. Summarize ONLY the medication facts provided in the user message.
            Do not invent clinical facts, dosing guidance, or external drug information.
            Output must be valid JSON with exactly these camelCase keys:
            overview, usageNotes, stockExpiryAlert, prescriptionRequirement.
            Keep each value concise (1-3 sentences). This is informational inventory text, not medical advice.
            """;

        /// <summary>
        /// Scalar <see cref="Medication"/> fields included in the AI prompt, aligned with inventory summary needs.
        /// Navigation properties and internal metadata are excluded.
        /// </summary>
        public static readonly IReadOnlyList<string> EntityFieldKeys =
        [
            "name",
            "category",
            "dosageForm",
            "strength",
            "description",
            "stockQuantity",
            "minimumStockLevel",
            "expiryDate",
            "requiresPrescription"
        ];

        public static string BuildUserPrompt(Medication medication)
        {
            ArgumentNullException.ThrowIfNull(medication);

            var fields = ReadEntityFields(medication);
            var builder = new StringBuilder();
            builder.AppendLine("Medication record (use only these stored fields):");

            foreach (var key in EntityFieldKeys)
            {
                builder.AppendLine($"{key}: {fields[key]}");
            }

            builder.AppendLine();
            builder.AppendLine("Section guidance:");
            builder.AppendLine("- overview: brief inventory summary from name, category, dosageForm, and strength.");
            builder.AppendLine("- usageNotes: paraphrase description only; if missing say no usage notes are stored.");
            builder.AppendLine("- stockExpiryAlert: use stockQuantity, minimumStockLevel, and expiryDate only.");
            builder.AppendLine("- prescriptionRequirement: use requiresPrescription only.");

            return builder.ToString().TrimEnd();
        }

        public static IReadOnlyDictionary<string, string> ReadEntityFields(Medication medication)
        {
            ArgumentNullException.ThrowIfNull(medication);

            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = medication.Name.Trim(),
                ["category"] = FormatOptional(medication.Category),
                ["dosageForm"] = FormatOptional(medication.DosageForm),
                ["strength"] = FormatOptional(medication.Strength),
                ["description"] = FormatOptional(medication.Description),
                ["stockQuantity"] = medication.StockQuantity.ToString(CultureInfo.InvariantCulture),
                ["minimumStockLevel"] = medication.MinimumStockLevel.ToString(CultureInfo.InvariantCulture),
                ["expiryDate"] = FormatDate(medication.ExpiryDate),
                ["requiresPrescription"] = medication.RequiresPrescription.ToString(CultureInfo.InvariantCulture)
            };
        }

        private static string FormatOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "Not recorded" : value.Trim();

        private static string FormatDate(DateTime? value) =>
            value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "Not recorded";
    }
}
