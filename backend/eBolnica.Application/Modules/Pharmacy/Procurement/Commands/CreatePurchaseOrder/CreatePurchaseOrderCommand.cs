using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;

namespace eBolnica.Application.Modules.Pharmacy.Procurement.Commands.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommand : IRequest<PurchaseOrderSummaryDto>
{
    public int SupplierId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<CreatePurchaseOrderItemDto> Items { get; init; } = Array.Empty<CreatePurchaseOrderItemDto>();
}

public sealed class CreatePurchaseOrderItemDto
{
    public int MedicationId { get; init; }
    public int Quantity { get; init; }
}

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicationId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
