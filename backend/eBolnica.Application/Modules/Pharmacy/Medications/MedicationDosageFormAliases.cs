namespace eBolnica.Application.Modules.Pharmacy.Medications;

public static class MedicationDosageFormAliases
{
    private static readonly Dictionary<string, string> ToBosnianMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tablet"] = "Tableta",
        ["Tableta"] = "Tableta",
        ["Capsule"] = "Kapsula",
        ["Kapsula"] = "Kapsula",
        ["Liquid"] = "Tečnost",
        ["Tečnost"] = "Tečnost",
        ["Injection"] = "Injekcija",
        ["Injekcija"] = "Injekcija",
        ["Cream"] = "Krema",
        ["Krema"] = "Krema",
        ["Drops"] = "Kapi",
        ["Kapi"] = "Kapi",
        ["Other"] = "Ostalo",
        ["Ostalo"] = "Ostalo",
    };

    public static string? ToBosnian(string? dosageForm)
    {
        if (string.IsNullOrWhiteSpace(dosageForm))
            return dosageForm;

        var key = dosageForm.Trim();
        return ToBosnianMap.TryGetValue(key, out var label) ? label : key;
    }
}
