namespace eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetMedicationStockHistory;

public sealed class GetMedicationStockHistoryQueryValidator : AbstractValidator<GetMedicationStockHistoryQuery>
{
    public GetMedicationStockHistoryQueryValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, 200);
    }
}
