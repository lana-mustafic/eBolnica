using eBolnica.Application.Modules.MedicalRecord.Commands.CreateMedicalReport;

public sealed class CreateMedicalReportCommandValidator : AbstractValidator<CreateMedicalReportCommand>
{
    public CreateMedicalReportCommandValidator()
    {
        RuleFor(x => x.MedicalRecordId).GreaterThan(0);
        RuleFor(x => x.Symptoms).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Diagnosis).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Therapy).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
