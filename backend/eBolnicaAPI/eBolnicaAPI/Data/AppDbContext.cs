    using eBolnicaAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
namespace eBolnicaAPI.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient>()
               .HasOne(p => p.AppUser)
               .WithOne()
               .HasForeignKey<Patient>(p => p.AppUserId)
               .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.AppUser)
                .WithOne()
                .HasForeignKey<Doctor>(d => d.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);
        }

    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    }
}

