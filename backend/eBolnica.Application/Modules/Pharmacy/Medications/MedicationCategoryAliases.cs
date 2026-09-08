namespace eBolnica.Application.Modules.Pharmacy.Medications;

public static class MedicationCategoryAliases
{
    private static readonly Dictionary<string, string[]> Equivalents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Analgetici"] = ["Analgetici", "Analgesics"],
        ["Analgesics"] = ["Analgetici", "Analgesics"],
        ["Antibiotici"] = ["Antibiotici", "Antibiotics"],
        ["Antibiotics"] = ["Antibiotici", "Antibiotics"],
        ["Antiviralni"] = ["Antiviralni", "Antivirals"],
        ["Antivirals"] = ["Antiviralni", "Antivirals"],
        ["Kardiovaskularni"] = ["Kardiovaskularni", "Cardiovascular"],
        ["Cardiovascular"] = ["Kardiovaskularni", "Cardiovascular"],
        ["Dijabetes"] = ["Dijabetes", "Diabetes"],
        ["Diabetes"] = ["Dijabetes", "Diabetes"],
        ["Gastrointestinalni"] = ["Gastrointestinalni", "Gastrointestinal"],
        ["Gastrointestinal"] = ["Gastrointestinalni", "Gastrointestinal"],
        ["Respiratorni"] = ["Respiratorni", "Respiratory"],
        ["Respiratory"] = ["Respiratorni", "Respiratory"],
        ["Vitamini"] = ["Vitamini", "Vitamins"],
        ["Vitamins"] = ["Vitamini", "Vitamins"],
        ["Ostalo"] = ["Ostalo", "Other"],
        ["Other"] = ["Ostalo", "Other"],
    };

    public static IReadOnlyList<string> Expand(string category)
    {
        var key = category.Trim();
        return Equivalents.TryGetValue(key, out var aliases) ? aliases : [key];
    }

    public static string? ToBosnian(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return category;

        var key = category.Trim();
        return Equivalents.TryGetValue(key, out var aliases) ? aliases[0] : key;
    }
}
