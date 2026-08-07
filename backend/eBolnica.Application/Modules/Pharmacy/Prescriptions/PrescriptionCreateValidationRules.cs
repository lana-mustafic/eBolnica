using System.Linq.Expressions;
using eBolnica.Application.Modules.Pharmacy.Prescriptions.Commands.CreatePrescription;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

public static class PrescriptionCreateValidationRules
{
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, int>> medicalReportId,
        Expression<Func<T, int>> patientId,
        Expression<Func<T, string?>> notes,
        Expression<Func<T, IEnumerable<CreatePrescriptionItemCommand>>> items)
    {
        validator.RuleFor(medicalReportId).GreaterThan(0);
        validator.RuleFor(patientId).GreaterThan(0);
        validator.RuleFor(notes).MaximumLength(500);
        validator.RuleFor(items).NotEmpty();
        validator.RuleForEach(items).SetValidator(new PrescriptionCreateItemValidator());
    }
}
