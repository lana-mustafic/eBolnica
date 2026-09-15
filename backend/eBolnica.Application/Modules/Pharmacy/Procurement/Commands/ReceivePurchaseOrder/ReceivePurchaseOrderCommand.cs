using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;

namespace eBolnica.Application.Modules.Pharmacy.Procurement.Commands.ReceivePurchaseOrder;

public sealed class ReceivePurchaseOrderCommand : IRequest<StockReceiptSummaryDto>
{
    public int PurchaseOrderId { get; set; }
}

public sealed class ReceivePurchaseOrderCommandValidator : AbstractValidator<ReceivePurchaseOrderCommand>
{
    public ReceivePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0);
    }
}
