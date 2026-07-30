using eBolnicaAPI.Data;
using eBolnicaAPI.Models.Entities;
using eBolnicaAPI.Services.Pharmacy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace eBolnicaAPI.Tests.Unit.Services
{
    public class MedicationAutocompleteServiceUnitTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly MedicationAutocompleteService _service;

        public MedicationAutocompleteServiceUnitTests()
        {
            _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            _context.Medications.AddRange(
                new Medication
                {
                    Name = "Aspirin",
                    GenericName = "Acetylsalicylic acid",
                    Category = "painkiller",
                    Manufacturer = "PharmaCorp",
                    Price = 8.5m,
                    StockQuantity = 100,
                    MinimumStockLevel = 20,
                    IsActive = true,
                    RequiresPrescription = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Medication
                {
                    Name = "Amoxicillin",
                    GenericName = "Amoxicillin",
                    Category = "antibiotics",
                    Manufacturer = "AntibioPharm",
                    Price = 12m,
                    StockQuantity = 50,
                    MinimumStockLevel = 15,
                    IsActive = true,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Medication
                {
                    Name = "Discontinued Drug",
                    Category = "antibiotics",
                    Price = 20m,
                    StockQuantity = 30,
                    MinimumStockLevel = 10,
                    IsActive = false,
                    RequiresPrescription = true,
                    CreatedAt = DateTime.UtcNow
                });
            _context.SaveChanges();

            _service = new MedicationAutocompleteService(_context);
        }

        [Fact]
        public async Task GetSuggestionsAsync_ShortQuery_ReturnsEmpty()
        {
            var suggestions = await _service.GetSuggestionsAsync("a");

            Assert.Empty(suggestions);
        }

        [Fact]
        public async Task GetSuggestionsAsync_MatchesNameGenericNameAndManufacturer()
        {
            var byName = await _service.GetSuggestionsAsync("asp");
            var byGeneric = await _service.GetSuggestionsAsync("acetyl");
            var byManufacturer = await _service.GetSuggestionsAsync("antibio");
            var byNameCaseInsensitive = await _service.GetSuggestionsAsync("ASPI");

            Assert.Single(byName);
            Assert.Equal("Aspirin", byName[0].Name);
            Assert.Equal("painkiller", byName[0].Category);
            Assert.Equal("PharmaCorp", byName[0].Manufacturer);
            Assert.Single(byGeneric);
            Assert.Equal("Aspirin", byGeneric[0].Name);
            Assert.Single(byManufacturer);
            Assert.Equal("Amoxicillin", byManufacturer[0].Name);
            Assert.Single(byNameCaseInsensitive);
            Assert.Equal("Aspirin", byNameCaseInsensitive[0].Name);
        }

        [Fact]
        public async Task GetSuggestionsAsync_ExcludesInactiveMedications()
        {
            var suggestions = await _service.GetSuggestionsAsync("discontinued");

            Assert.Empty(suggestions);
        }

        [Fact]
        public async Task GetSuggestionsAsync_OrdersByNameAndCapsLimit()
        {
            var suggestions = await _service.GetSuggestionsAsync("in", limit: 1);

            Assert.Single(suggestions);
            Assert.Equal("Amoxicillin", suggestions[0].Name);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
