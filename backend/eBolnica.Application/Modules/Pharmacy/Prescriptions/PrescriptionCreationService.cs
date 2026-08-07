using eBolnica.Application.Abstractions;
using eBolnica.Application.Common;
using eBolnica.Application.Modules.Pharmacy.Activities;
using eBolnica.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace eBolnica.Application.Modules.Pharmacy.Prescriptions;

public sealed class PrescriptionCreationService(
    IAppDbContext ctx,
    IAppCurrentUser currentUser,
    IPrescriptionNumberGenerator prescriptionNumberGenerator) : IPrescriptionCreationService
{
    public async Task<PrescriptionDto> CreateAsync(PrescriptionCreationRequest request, CancellationToken ct = default)
    {
        var medicalReport = await ctx.MedicalReports
            .Include(mr => mr.MedicalRecord)
            .FirstOrDefaultAsync(mr => mr.Id == request.MedicalReportId, ct);

        if (medicalReport is null)
            throw new eBolnicaNotFoundException("Medical report not found.");

        if (medicalReport.MedicalRecord.PatientId != request.PatientId)
            throw new eBolnicaBusinessRuleException(
                "prescription.report_patient_mismatch",
                "Medical report does not belong to the selected patient.");

        var patient = await ctx.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, ct);

        if (patient is null)
            throw new eBolnicaNotFoundException("Patient not found.");

        var itemCommands = request.PrescriptionItems;
        var medicationIds = itemCommands.Select(i => i.MedicationId).Distinct().ToList();
        var medications = await ctx.Medications
            .Where(m => medicationIds.Contains(m.Id))
            .ToListAsync(ct);

        if (medications.Count != medicationIds.Count)
            throw new eBolnicaBusinessRuleException("prescription.medication_missing", "One or more medications were not found.");

        foreach (var medication in medications.Where(m => !m.IsActive))
            throw new eBolnicaBusinessRuleException("prescription.medication_inactive", $"Medication {medication.Name} is inactive.");

        foreach (var medication in medications.Where(m => !m.RequiresPrescription))
            throw new eBolnicaBusinessRuleException(
                "prescription.medication_otc",
                $"Medication {medication.Name} does not require a prescription and cannot be added to a prescription.");

        var medicationsById = medications.ToDictionary(m => m.Id);
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var transaction = await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var now = DateTime.UtcNow;
                var prescriptionNumber = await prescriptionNumberGenerator.ReserveNextAsync(now.Year, ct);

                var prescription = new PrescriptionEntity
                {
                    PrescriptionNumber = prescriptionNumber,
                    MedicalReportId = request.MedicalReportId,
                    PatientId = request.PatientId,
                    DoctorId = medicalReport.DoctorId,
                    Status = PrescriptionStatuses.Pending,
                    PrescribedDate = now,
                    Notes = request.Notes?.Trim(),
                    CreatedAtUtc = now
                };

                decimal totalAmount = 0;
                foreach (var item in itemCommands)
                {
                    var medication = medicationsById[item.MedicationId];
                    var unitPrice = medication.Price;
                    var itemTotal = unitPrice * item.Quantity;
                    totalAmount += itemTotal;

                    prescription.Items.Add(new PrescriptionItemEntity
                    {
                        MedicationId = item.MedicationId,
                        Quantity = item.Quantity,
                        Instructions = item.Instructions?.Trim(),
                        UnitPrice = unitPrice,
                        TotalPrice = itemTotal,
                        CreatedAtUtc = now
                    });
                }

                prescription.TotalAmount = totalAmount;
                ctx.Prescriptions.Add(prescription);
                await ctx.SaveChangesAsync(ct);

                PharmacyActivityWriter.Record(
                    ctx,
                    PharmacyActivityEventTypes.PrescriptionCreated,
                    PharmacyActivityCategories.Prescription,
                    PharmacyActivitySeverities.Info,
                    $"Novi recept {prescriptionNumber} — {patient.FirstName} {patient.LastName}",
                    currentUser.UserId,
                    prescription.Id);
                await ctx.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                var created = await ctx.Prescriptions
                    .AsNoTracking()
                    .WithDetails()
                    .FirstAsync(p => p.Id == prescription.Id, ct);

                return PrescriptionMapping.MapToDto(created);
            }
            catch (DbUpdateException ex) when (attempt < maxAttempts && DbUpdateExceptionHelper.IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync(ct);
                ctx.ClearChangeTracker();
            }
            catch (DbUpdateException ex) when (DbUpdateExceptionHelper.IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync(ct);
                throw new eBolnicaConflictException("Could not generate a unique prescription number. Please retry.");
            }
        }

        throw new eBolnicaConflictException("Could not generate a unique prescription number. Please retry.");
    }
}
