namespace eBolnica.Application.Modules.Pharmacy.Medications;

internal static class MedicationDisplayLabels
{
    public static void Apply(MedicationDto dto)
    {
        dto.Category = MedicationCategoryAliases.ToBosnian(dto.Category);
        dto.DosageForm = MedicationDosageFormAliases.ToBosnian(dto.DosageForm);
    }

    public static void Apply(IEnumerable<MedicationDto> items)
    {
        foreach (var item in items)
            Apply(item);
    }
}
