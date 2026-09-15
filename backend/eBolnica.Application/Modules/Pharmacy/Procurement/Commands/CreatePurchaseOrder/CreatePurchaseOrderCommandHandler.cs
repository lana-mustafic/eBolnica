using eBolnica.Application.Modules.Pharmacy.Activities;
using eBolnica.Application.Modules.Pharmacy.Medications.Queries.GetInventory;
using eBolnica.Application.Modules.Pharmacy.Procurement.Commands.CreatePurchaseOrder;
using eBolnica.Domain.Entities.Pharmacy;

namespace eBolnica.Application.Modules.Pharmacy.Procurement.Commands.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandHandler(IAppDbContext ctx, IAppCurrentUser currentUser)
    : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderSummaryDto>
{
    public async Task<PurchaseOrderSummaryDto> Handle(CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            throw new eBolnicaBusinessRuleException("auth.not_authenticated", "User is not authenticated.");

        var pharmacist = await ctx.Pharmacists
            .FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value, ct)
            ?? throw new eBolnicaNotFoundException("Pharmacist profile not found.");

        var supplier = await ctx.MedicationSuppliers
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.IsActive, ct)
            ?? throw new eBolnicaNotFoundException("Supplier not found.");

        var medicationIds = request.Items.Select(i => i.MedicationId).Distinct().ToList();
        var medications = await ctx.Medications
            .Where(m => medicationIds.Contains(m.Id) && m.IsActive)
            .ToListAsync(ct);

        if (medications.Count != medicationIds.Count)
            throw new eBolnicaBusinessRuleException("purchase.medication_missing", "One or more medications could not be found.");

        var now = DateTime.UtcNow;
        var yearCount = await ctx.PurchaseOrders.CountAsync(o => o.OrderedAtUtc.Year == now.Year, ct) + 1;
        var itemsByMedication = medications.ToDictionary(m => m.Id);

        var order = new PurchaseOrderEntity
        {
            OrderNumber = $"PO-{now.Year}-{yearCount:0000}",
            SupplierId = supplier.Id,
            PharmacistId = pharmacist.Id,
            Status = PurchaseOrderStatuses.Ordered,
            OrderedAtUtc = now,
            ExpectedAtUtc = now.AddDays(7),
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = now
        };

        foreach (var item in request.Items)
        {
            var medication = itemsByMedication[item.MedicationId];
            order.Items.Add(new PurchaseOrderItemEntity
            {
                MedicationId = medication.Id,
                Quantity = item.Quantity,
                UnitPrice = medication.Price,
                CreatedAtUtc = now
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        ctx.PurchaseOrders.Add(order);

        PharmacyActivityWriter.Record(
            ctx,
            PharmacyActivityEventTypes.PurchaseOrderCreated,
            PharmacyActivityCategories.Inventory,
            PharmacyActivitySeverities.Info,
            $"Kreirana narudžba {order.OrderNumber}",
            currentUser.UserId);

        await ctx.SaveChangesAsync(ct);

        return new PurchaseOrderSummaryDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            SupplierName = supplier.Name,
            Status = order.Status,
            OrderedAtUtc = order.OrderedAtUtc,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new PurchaseOrderItemDto
            {
                MedicationId = i.MedicationId,
                MedicationName = itemsByMedication[i.MedicationId].Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }
}
