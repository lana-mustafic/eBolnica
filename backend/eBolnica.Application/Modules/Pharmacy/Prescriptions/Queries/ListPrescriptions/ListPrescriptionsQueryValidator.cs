namespace eBolnica.Application.Modules.Pharmacy.Prescriptions.Queries.ListPrescriptions;

public sealed class ListPrescriptionsQueryValidator : AbstractValidator<ListPrescriptionsQuery>
{
    public ListPrescriptionsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x)
            .Must(q => !q.PrescribedFrom.HasValue || !q.PrescribedTo.HasValue || q.PrescribedTo >= q.PrescribedFrom)
            .WithMessage("PrescribedTo must be on or after PrescribedFrom.");
    }
}
