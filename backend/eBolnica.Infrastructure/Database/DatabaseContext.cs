using eBolnica.Application.Abstractions;
using eBolnica.Domain.Entities.Clinical;
using eBolnica.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace eBolnica.Infrastructure.Database;

public partial class DatabaseContext : DbContext, IAppDbContext
{
    public DbSet<eBolnicaUserEntity> Users => Set<eBolnicaUserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<DoctorEntity> Doctors => Set<DoctorEntity>();
    public DbSet<PatientEntity> Patients => Set<PatientEntity>();
    public DbSet<MedicalRecordEntity> MedicalRecords => Set<MedicalRecordEntity>();
    public DbSet<PharmacistEntity> Pharmacists => Set<PharmacistEntity>();
    public DbSet<MedicalReportEntity> MedicalReports => Set<MedicalReportEntity>();
    public DbSet<MedicationEntity> Medications => Set<MedicationEntity>();
    public DbSet<MedicationImageEntity> MedicationImages => Set<MedicationImageEntity>();
    public DbSet<MedicationStockHistoryEntity> MedicationStockHistory => Set<MedicationStockHistoryEntity>();
    public DbSet<PrescriptionEntity> Prescriptions => Set<PrescriptionEntity>();
    public DbSet<PrescriptionItemEntity> PrescriptionItems => Set<PrescriptionItemEntity>();
    public DbSet<PrescriptionNumberSequenceEntity> PrescriptionNumberSequences =>
        Set<PrescriptionNumberSequenceEntity>();

    DatabaseFacade IAppDbContext.Database => Database;

    public void ClearChangeTracker() => ChangeTracker.Clear();

    private readonly TimeProvider _clock;
    public DatabaseContext(DbContextOptions<DatabaseContext> options, TimeProvider clock) : base(options)
    {
        _clock = clock;
    }
}
