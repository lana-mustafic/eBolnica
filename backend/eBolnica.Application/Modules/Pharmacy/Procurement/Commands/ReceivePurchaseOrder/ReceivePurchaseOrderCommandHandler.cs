using eBolnica.Application.Modules.Pharmacy.Activities;
using eBolnica.Application.Modules.Pharmacy.Medications;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;
using eBolnica.Application.Modules.Pharmacy.Procurement.Commands.ReceivePurchaseOrder;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Procurement.Commands.ReceivePurchaseOrder;

public sealed class ReceivePurchaseOrderCommandHandler(
    IAppDbContext ctx,
    IAppCurrentUser currentUser,
    IPharmacyAnalyticsService analytics)
    : IRequestHandler<ReceivePurchaseOrderCommand, StockReceiptSummaryDto>
{
    public async Task<StockReceiptSummaryDto> Handle(ReceivePurchaseOrderCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var pharmacist = await ctx.Pharmacists
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value, ct)
            ?? throw new eBolnicaNotFoundException("Pharmacist profile not found.");

        var order = await ctx.PurchaseOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, ct)
            ?? throw new eBolnicaNotFoundException("Purchase order not found.");

        if (!string.Equals(order.Status, PurchaseOrderStatuses.Ordered, StringComparison.OrdinalIgnoreCase))
            throw new eBolnicaBusinessRuleException(
                "purchase.already_received",
                $"Purchase order is already {order.Status}.");

        var medicationIds = order.Items.Select(i => i.MedicationId).Distinct().ToList();
        var medications = await ctx.Medications
            .Where(m => medicationIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, ct);

        var now = DateTime.UtcNow;
        var yearCount = await ctx.StockReceipts.CountAsync(r => r.ReceivedAtUtc.Year == now.Year, ct) + 1;

        var receipt = new StockReceiptEntity
        {
            ReceiptNumber = $"GRN-{now.Year}-{yearCount:0000}",
            PurchaseOrderId = order.Id,
            PharmacistId = pharmacist.Id,
            ReceivedAtUtc = now,
            CreatedAtUtc = now
        };

        foreach (var item in order.Items)
        {
            if (!medications.TryGetValue(item.MedicationId, out var medication))
                throw new eBolnicaBusinessRuleException("purchase.medication_missing", "Medication on purchase order was not found.");

            var previous = medication.StockQuantity;
            medication.StockQuantity += item.Quantity;
            medication.ModifiedAtUtc = now;

            receipt.Items.Add(new StockReceiptItemEntity
            {
                MedicationId = medication.Id,
                Quantity = item.Quantity,
                BatchNumber = medication.BatchNumber,
                ExpiryDate = medication.ExpiryDate,
                CreatedAtUtc = now
            });

            MedicationStockHistoryWriter.Record(
                ctx,
                medication.Id,
                previous,
                medication.StockQuantity,
                MedicationStockChangeReasons.PurchaseReceived);
        }

        order.Status = PurchaseOrderStatuses.Received;
        order.ModifiedAtUtc = now;
        ctx.StockReceipts.Add(receipt);

        PharmacyActivityWriter.Record(
            ctx,
            PharmacyActivityEventTypes.StockReceived,
            PharmacyActivityCategories.Inventory,
            PharmacyActivitySeverities.Success,
            $"Zaprimljena narudžba {order.OrderNumber}",
            currentUser.UserId);

        await ctx.SaveChangesAsync(ct);
        analytics.InvalidateAnalyticsCache();

        return new StockReceiptSummaryDto
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            OrderNumber = order.OrderNumber,
            ReceivedAtUtc = receipt.ReceivedAtUtc,
            TotalQuantity = receipt.Items.Sum(i => i.Quantity)
        };
    }
}
