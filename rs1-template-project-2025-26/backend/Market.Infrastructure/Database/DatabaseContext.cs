using Market.Application.Abstractions;
using Market.Domain.Entities.Clinical;
using Market.Domain.Entities.Pharmacy;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Market.Infrastructure.Database;

public partial class DatabaseContext : DbContext, IAppDbContext
{
    public DbSet<MarketUserEntity> Users => Set<MarketUserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<DoctorEntity> Doctors => Set<DoctorEntity>();
    public DbSet<PatientEntity> Patients => Set<PatientEntity>();
    public DbSet<MedicalRecordEntity> MedicalRecords => Set<MedicalRecordEntity>();
    public DbSet<PharmacistEntity> Pharmacists => Set<PharmacistEntity>();
    public DbSet<MedicalReportEntity> MedicalReports => Set<MedicalReportEntity>();
    public DbSet<MedicationEntity> Medications => Set<MedicationEntity>();
    public DbSet<MedicationImageEntity> MedicationImages => Set<MedicationImageEntity>();
    public DbSet<PrescriptionEntity> Prescriptions => Set<PrescriptionEntity>();
    public DbSet<PrescriptionItemEntity> PrescriptionItems => Set<PrescriptionItemEntity>();

    DatabaseFacade IAppDbContext.Database => Database;

    private readonly TimeProvider _clock;
    public DatabaseContext(DbContextOptions<DatabaseContext> options, TimeProvider clock) : base(options)
    {
        _clock = clock;
    }
}
