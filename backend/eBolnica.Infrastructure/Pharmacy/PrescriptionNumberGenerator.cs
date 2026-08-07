using eBolnica.Application.Abstractions;
using eBolnica.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore;

namespace eBolnica.Infrastructure.Pharmacy;

public sealed class PrescriptionNumberGenerator(IAppDbContext ctx) : IPrescriptionNumberGenerator
{
    public async Task<string> ReserveNextAsync(int year, CancellationToken ct = default)
    {
        var sequence = await ctx.PrescriptionNumberSequences
            .FirstOrDefaultAsync(s => s.Year == year, ct);

        if (sequence is null)
        {
            sequence = new PrescriptionNumberSequenceEntity
            {
                Year = year,
                LastNumber = 1
            };
            ctx.PrescriptionNumberSequences.Add(sequence);
        }
        else
        {
            sequence.LastNumber++;
        }

        await ctx.SaveChangesAsync(ct);
        return $"RX-{year}-{sequence.LastNumber:D4}";
    }
}
