using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationImportDuplicateCheckerUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MedicationImportDuplicateChecker _checker;

        public MedicationImportDuplicateCheckerUnitTests()
        {
            _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            _context.Medications.Add(new Medication
            {
                Name = "Aspirin",
                Category = "painkiller",
                Price = 5m,
                StockQuantity = 10,
                MinimumStockLevel = 5,
                IsActive = true,
                RequiresPrescription = false,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            _checker = new MedicationImportDuplicateChecker(_context);
        }

        [Fact]
        public void TryRegisterName_ExistingDatabaseName_ReturnsError()
        {
            var existing = new HashSet<string>(StringComparer.Ordinal) { "aspirin" };
            var import = new HashSet<string>(StringComparer.Ordinal);

            var error = _checker.TryRegisterName("Aspirin", 2, existing, import);

            Assert.NotNull(error);
            Assert.Equal("Name", error!.Field);
            Assert.Contains("already exists", error.Reason);
        }

        [Fact]
        public void TryRegisterName_DuplicateWithinImport_ReturnsFileDuplicateError()
        {
            var existing = new HashSet<string>(StringComparer.Ordinal);
            var import = new HashSet<string>(StringComparer.Ordinal) { "new med" };

            var error = _checker.TryRegisterName("New Med", 3, existing, import);

            Assert.NotNull(error);
            Assert.Contains("Duplicate name in this import file", error!.Reason);
        }

        [Fact]
        public async Task FindConflictingNamesAsync_ReturnsNormalizedMatches()
        {
            var conflicts = await _checker.FindConflictingNamesAsync(new[] { " aspirin ", "Unique" });

            Assert.Single(conflicts);
            Assert.Equal("aspirin", conflicts[0]);
        }

        [Fact]
        public async Task IsNameAvailableAsync_ExistingName_ReturnsFalse()
        {
            var isAvailable = await _checker.IsNameAvailableAsync("Aspirin");

            Assert.False(isAvailable);
        }

        [Fact]
        public async Task IsNameAvailableAsync_UniqueName_ReturnsTrue()
        {
            var isAvailable = await _checker.IsNameAvailableAsync("Paracetamol");

            Assert.True(isAvailable);
        }

        [Fact]
        public async Task IsNameAvailableAsync_CaseInsensitiveAndTrimmed_ReturnsFalse()
        {
            var isAvailable = await _checker.IsNameAvailableAsync("  ASPIRIN  ");

            Assert.False(isAvailable);
        }

        [Fact]
        public async Task IsNameAvailableAsync_WithExcludeId_ReturnsTrueForSameName()
        {
            var medication = _context.Medications.Single(m => m.Name == "Aspirin");

            var isAvailable = await _checker.IsNameAvailableAsync("Aspirin", medication.Id);

            Assert.True(isAvailable);
        }

        [Fact]
        public async Task IsNameAvailableAsync_EmptyName_Throws()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _checker.IsNameAvailableAsync("   "));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
