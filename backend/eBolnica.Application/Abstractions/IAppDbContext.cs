using eBolnica.Domain.Entities.Clinical;
using eBolnica.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace eBolnica.Application.Abstractions;

// Application layer
public interface IAppDbContext
{
    DbSet<eBolnicaUserEntity> Users { get; }
    DbSet<RefreshTokenEntity> RefreshTokens { get; }

    DbSet<DoctorEntity> Doctors { get; }
    DbSet<PatientEntity> Patients { get; }
    DbSet<MedicalRecordEntity> MedicalRecords { get; }
    DbSet<PharmacistEntity> Pharmacists { get; }
    DbSet<MedicalReportEntity> MedicalReports { get; }
    DbSet<MedicationEntity> Medications { get; }
    DbSet<MedicationImageEntity> MedicationImages { get; }
    DbSet<MedicationStockHistoryEntity> MedicationStockHistory { get; }
    DbSet<PrescriptionEntity> Prescriptions { get; }
    DbSet<PrescriptionItemEntity> PrescriptionItems { get; }
    DbSet<PrescriptionNumberSequenceEntity> PrescriptionNumberSequences { get; }
    DbSet<PharmacyActivityEntity> PharmacyActivities { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);

    void ClearChangeTracker();
}
