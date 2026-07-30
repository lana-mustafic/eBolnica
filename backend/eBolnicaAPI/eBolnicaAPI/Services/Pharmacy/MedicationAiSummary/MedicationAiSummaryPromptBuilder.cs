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

        public static string BuildUserPrompt(Medication medication)
        {
            var dosage = BuildDosageLabel(medication.DosageForm, medication.Strength);
            var stockStatus = BuildStockStatus(medication.StockQuantity, medication.MinimumStockLevel);
            var expiryStatus = BuildExpiryStatus(medication.ExpiryDate);

            var builder = new StringBuilder();
            builder.AppendLine("Medication record (use only these fields):");
            builder.AppendLine($"name: {medication.Name}");
            builder.AppendLine($"genericName: {ValueOrDash(medication.GenericName)}");
            builder.AppendLine($"category: {ValueOrDash(medication.Category)}");
            builder.AppendLine($"dosage: {dosage}");
            builder.AppendLine($"description: {ValueOrDash(medication.Description)}");
            builder.AppendLine($"stockQuantity: {medication.StockQuantity}");
            builder.AppendLine($"minimumStockLevel: {medication.MinimumStockLevel}");
            builder.AppendLine($"stockStatus: {stockStatus}");
            builder.AppendLine($"expiryDate: {FormatDate(medication.ExpiryDate)}");
            builder.AppendLine($"expiryStatus: {expiryStatus}");
            builder.AppendLine($"requiresPrescription: {medication.RequiresPrescription}");
            builder.AppendLine($"isActive: {medication.IsActive}");
            builder.AppendLine();
            builder.AppendLine("Section guidance:");
            builder.AppendLine("- overview: brief inventory-oriented summary from name, category, and dosage.");
            builder.AppendLine("- usageNotes: paraphrase description only; if missing say no usage notes are stored.");
            builder.AppendLine("- stockExpiryAlert: mention stockStatus and expiryStatus.");
            builder.AppendLine("- prescriptionRequirement: state whether prescription is required.");

            return builder.ToString().TrimEnd();
        }

        public static string BuildDosageLabel(string? dosageForm, string? strength)
        {
            var parts = new[] { dosageForm?.Trim(), strength?.Trim() }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

            return parts.Length == 0 ? "Not recorded" : string.Join(" ", parts);
        }

        public static string BuildStockStatus(int stockQuantity, int minimumStockLevel)
        {
            if (stockQuantity <= 0)
            {
                return "out of stock";
            }

            if (stockQuantity < minimumStockLevel)
            {
                return "low stock";
            }

            return "normal stock";
        }

        internal static string BuildExpiryStatus(DateTime? expiryDate)
        {
            if (!expiryDate.HasValue)
            {
                return "expiry date not recorded";
            }

            var today = DateTime.UtcNow.Date;
            var expiry = expiryDate.Value.Date;

            if (expiry < today)
            {
                return "expired";
            }

            if (expiry <= today.AddDays(30))
            {
                return "expiring within 30 days";
            }

            return "not expiring soon";
        }

        private static string ValueOrDash(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "Not recorded" : value.Trim();

        private static string FormatDate(DateTime? value) =>
            value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "Not recorded";
    }
}
