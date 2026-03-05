using eBolnicaAPI.Controllers;
using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace eBolnicaAPI.Tests.Unit
{
    public class MedicalRecordControllerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private Mock<UserManager<AppUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task GetMedicalRecord_WithValidPatientId_ReturnsOk()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockUserManager = GetMockUserManager();

            var appUser = new AppUser
            {
                Id = "user-1",
                Email = "patient@test.com",
                FirstName = "John",
                LastName = "Doe",
                UserType = "Patient"
            };

            var patient = new Patient
            {
                Id = 1,
                AppUserId = "user-1",
                FirstName = "John",
                LastName = "Doe",
                AppUser = appUser
            };

            var medicalRecord = new MedicalRecord
            {
                Id = 1,
                PatientId = 1,
                RecordNumber = "MR-2024-1",
                Patient = patient
            };

            patient.MedicalRecord = medicalRecord;

            dbContext.AppUsers.Add(appUser);
            dbContext.Patients.Add(patient);
            dbContext.MedicalRecords.Add(medicalRecord);
            await dbContext.SaveChangesAsync();

            var controller = new MedicalRecordController(dbContext, mockUserManager.Object);

            // Act
            var result = await controller.GetMedicalRecordById(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task NewReport_WithoutAuthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockUserManager = GetMockUserManager();

            var controller = new MedicalRecordController(dbContext, mockUserManager.Object);

            // Postavi prazan ClaimsPrincipal (nije ulogovan)
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            var dto = new MedicalReportCreateDto
            {
                MedicalRecordId = 1,
                Symptoms = "Headache",
                Diagnosis = "Migraine",
                Therapy = "Rest"
            };

            // Act
            var result = await controller.NewReport(dto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
