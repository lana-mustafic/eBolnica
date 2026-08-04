using Market.Domain.Entities.Clinical;
using Market.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Market.Application.Abstractions;

// Application layer
public interface IAppDbContext
{
    DbSet<MarketUserEntity> Users { get; }
    DbSet<RefreshTokenEntity> RefreshTokens { get; }

    DbSet<DoctorEntity> Doctors { get; }
    DbSet<PatientEntity> Patients { get; }
    DbSet<MedicalRecordEntity> MedicalRecords { get; }
    DbSet<PharmacistEntity> Pharmacists { get; }
    DbSet<MedicalReportEntity> MedicalReports { get; }
    DbSet<MedicationEntity> Medications { get; }
    DbSet<MedicationImageEntity> MedicationImages { get; }
    DbSet<PrescriptionEntity> Prescriptions { get; }
    DbSet<PrescriptionItemEntity> PrescriptionItems { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken ct);
}
