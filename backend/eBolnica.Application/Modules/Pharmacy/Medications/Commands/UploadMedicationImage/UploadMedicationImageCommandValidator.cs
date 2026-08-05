namespace eBolnica.Application.Modules.Pharmacy.Medications.Commands.UploadMedicationImage;

public sealed class UploadMedicationImageCommandValidator : AbstractValidator<UploadMedicationImageCommand>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public UploadMedicationImageCommandValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.FileName)
            .Must(name =>
            {
                var ext = Path.GetExtension(name);
                return !string.IsNullOrWhiteSpace(ext) && AllowedExtensions.Contains(ext);
            })
            .WithMessage("Allowed image types: JPG, PNG, WEBP.");
    }
}
