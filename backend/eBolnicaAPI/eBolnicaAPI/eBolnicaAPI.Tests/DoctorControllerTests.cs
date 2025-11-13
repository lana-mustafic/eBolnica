using eBolnicaAPI.Controllers;
using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace eBolnicaAPI.Tests;

public class DoctorControllerTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private DoctorController CreateControllerWithUser(AppDbContext dbContext, string userId, string userEmail)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, userEmail),
            new Claim(ClaimTypes.Role, "Doctor")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(),
            null, null, null, null, null, null, null, null);

        var controller = new DoctorController(dbContext, userManagerMock.Object)
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return controller;
    }

    [Fact]
    public async Task SearchPatients_ReturnsOnlyUnassignedOrOtherDoctorPatients()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();

        // Create doctors
        var doctor1User = new AppUser
        {
            Id = "doctor1",
            Email = "doctor1@test.com",
            UserName = "doctor1@test.com",
            FirstName = "Doctor",
            LastName = "One",
            UserType = "Doctor"
        };

        var doctor2User = new AppUser
        {
            Id = "doctor2",
            Email = "doctor2@test.com",
            UserName = "doctor2@test.com",
            FirstName = "Doctor",
            LastName = "Two",
            UserType = "Doctor"
        };

        var doctor1 = new Doctor
        {
            Id = 1,
            AppUserId = "doctor1",
            FirstName = "Doctor",
            LastName = "One",
            RegistrationStatus = "Approved",
            LicenseNumber = "LIC001"
        };

        var doctor2 = new Doctor
        {
            Id = 2,
            AppUserId = "doctor2",
            FirstName = "Doctor",
            LastName = "Two",
            RegistrationStatus = "Approved",
            LicenseNumber = "LIC002"
        };

        // Create patients
        var patient1User = new AppUser
        {
            Id = "patient1",
            Email = "patient1@test.com",
            UserName = "patient1@test.com",
            FirstName = "Patient",
            LastName = "One",
            UserType = "Patient"
        };

        var patient2User = new AppUser
        {
            Id = "patient2",
            Email = "patient2@test.com",
            UserName = "patient2@test.com",
            FirstName = "Patient",
            LastName = "Two",
            UserType = "Patient"
        };

        var patient3User = new AppUser
        {
            Id = "patient3",
            Email = "patient3@test.com",
            UserName = "patient3@test.com",
            FirstName = "Patient",
            LastName = "Three",
            UserType = "Patient"
        };

        var patient1 = new Patient
        {
            Id = 1,
            AppUserId = "patient1",
            FirstName = "Patient",
            LastName = "One",
            DoctorId = null // Unassigned
        };

        var patient2 = new Patient
        {
            Id = 2,
            AppUserId = "patient2",
            FirstName = "Patient",
            LastName = "Two",
            DoctorId = 2 // Assigned to doctor2
        };

        var patient3 = new Patient
        {
            Id = 3,
            AppUserId = "patient3",
            FirstName = "Patient",
            LastName = "Three",
            DoctorId = 1 // Assigned to doctor1 (should not appear)
        };

        dbContext.Users.AddRange(doctor1User, doctor2User, patient1User, patient2User, patient3User);
        dbContext.Doctors.AddRange(doctor1, doctor2);
        dbContext.Patients.AddRange(patient1, patient2, patient3);
        await dbContext.SaveChangesAsync();

        var controller = CreateControllerWithUser(dbContext, "doctor1", "doctor1@test.com");

        // Act
        var result = await controller.SearchPatients(null);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var patients = Assert.IsType<List<PatientSearchDto>>(okResult.Value);
        
        Assert.Equal(2, patients.Count);
        Assert.Contains(patients, p => p.Id == 1); // Unassigned patient
        Assert.Contains(patients, p => p.Id == 2); // Patient assigned to doctor2
        Assert.DoesNotContain(patients, p => p.Id == 3); // Patient assigned to doctor1 should not appear
    }

    [Fact]
    public async Task SearchPatients_FiltersBySearchTerm()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();

        var doctor1User = new AppUser
        {
            Id = "doctor1",
            Email = "doctor1@test.com",
            UserName = "doctor1@test.com",
            FirstName = "Doctor",
            LastName = "One",
            UserType = "Doctor"
        };

        var doctor1 = new Doctor
        {
            Id = 1,
            AppUserId = "doctor1",
            FirstName = "Doctor",
            LastName = "One",
            RegistrationStatus = "Approved",
            LicenseNumber = "LIC001"
        };

        var patient1User = new AppUser
        {
            Id = "patient1",
            Email = "john.doe@test.com",
            UserName = "john.doe@test.com",
            FirstName = "John",
            LastName = "Doe",
            UserType = "Patient"
        };

        var patient2User = new AppUser
        {
            Id = "patient2",
            Email = "jane.smith@test.com",
            UserName = "jane.smith@test.com",
            FirstName = "Jane",
            LastName = "Smith",
            UserType = "Patient"
        };

        var patient1 = new Patient
        {
            Id = 1,
            AppUserId = "patient1",
            FirstName = "John",
            LastName = "Doe",
            DoctorId = null
        };

        var patient2 = new Patient
        {
            Id = 2,
            AppUserId = "patient2",
            FirstName = "Jane",
            LastName = "Smith",
            DoctorId = null
        };

        dbContext.Users.AddRange(doctor1User, patient1User, patient2User);
        dbContext.Doctors.Add(doctor1);
        dbContext.Patients.AddRange(patient1, patient2);
        await dbContext.SaveChangesAsync();

        var controller = CreateControllerWithUser(dbContext, "doctor1", "doctor1@test.com");

        // Act - Search by first name
        var result1 = await controller.SearchPatients("John");
        var okResult1 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result1);
        var patients1 = Assert.IsType<List<PatientSearchDto>>(okResult1.Value);
        Assert.Single(patients1);
        Assert.Equal("John", patients1[0].FirstName);

        // Act - Search by last name
        var result2 = await controller.SearchPatients("Smith");
        var okResult2 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result2);
        var patients2 = Assert.IsType<List<PatientSearchDto>>(okResult2.Value);
        Assert.Single(patients2);
        Assert.Equal("Smith", patients2[0].LastName);

        // Act - Search by email
        var result3 = await controller.SearchPatients("john.doe");
        var okResult3 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result3);
        var patients3 = Assert.IsType<List<PatientSearchDto>>(okResult3.Value);
        Assert.Single(patients3);
        Assert.Equal("john.doe@test.com", patients3[0].Email);
    }

    [Fact]
    public async Task AssignPatient_AssignsPatientToDoctorAndUpdatesDetails()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();

        var doctor1User = new AppUser
        {
            Id = "doctor1",
            Email = "doctor1@test.com",
            UserName = "doctor1@test.com",
            FirstName = "Doctor",
            LastName = "One",
            UserType = "Doctor"
        };

        var doctor1 = new Doctor
        {
            Id = 1,
            AppUserId = "doctor1",
            FirstName = "Doctor",
            LastName = "One",
            RegistrationStatus = "Approved",
            LicenseNumber = "LIC001"
        };

        var patient1User = new AppUser
        {
            Id = "patient1",
            Email = "patient1@test.com",
            UserName = "patient1@test.com",
            FirstName = "Patient",
            LastName = "One",
            UserType = "Patient"
        };

        var patient1 = new Patient
        {
            Id = 1,
            AppUserId = "patient1",
            FirstName = "Patient",
            LastName = "One",
            DoctorId = null // Unassigned
        };

        dbContext.Users.AddRange(doctor1User, patient1User);
        dbContext.Doctors.Add(doctor1);
        dbContext.Patients.Add(patient1);
        await dbContext.SaveChangesAsync();

        var controller = CreateControllerWithUser(dbContext, "doctor1", "doctor1@test.com");

        var assignDto = new AssignPatientDto
        {
            PatientId = 1,
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "Male",
            PhoneNumber = "+1234567890",
            Address = "123 Main St",
            BloodType = "O+",
            MedicalRecordId = "MR001"
        };

        // Act
        var result = await controller.AssignPatient(assignDto);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        var responseDto = Assert.IsType<DoctorAssignedPatientDto>(okResult.Value);
        
        Assert.Equal(1, responseDto.Id);
        Assert.Equal(1, responseDto.DoctorId);
        Assert.Equal(new DateTime(1990, 1, 1), responseDto.DateOfBirth);
        Assert.Equal("Male", responseDto.Gender);
        Assert.Equal("+1234567890", responseDto.PhoneNumber);
        Assert.Equal("123 Main St", responseDto.Address);
        Assert.Equal("O+", responseDto.BloodType);
        Assert.Equal("MR001", responseDto.MedicalRecordId);

        // Verify in database
        var updatedPatient = await dbContext.Patients.FindAsync(1);
        Assert.NotNull(updatedPatient);
        Assert.Equal(1, updatedPatient.DoctorId);
        Assert.Equal(new DateTime(1990, 1, 1), updatedPatient.DateOfBirth);
        Assert.Equal("Male", updatedPatient.Gender);
    }

    [Fact]
    public async Task AssignPatient_ReturnsErrorWhenPatientAlreadyAssignedToAnotherDoctor()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();

        var doctor1User = new AppUser
        {
            Id = "doctor1",
            Email = "doctor1@test.com",
            UserName = "doctor1@test.com",
            FirstName = "Doctor",
            LastName = "One",
            UserType = "Doctor"
        };

        var doctor2User = new AppUser
        {
            Id = "doctor2",
            Email = "doctor2@test.com",
            UserName = "doctor2@test.com",
            FirstName = "Doctor",
            LastName = "Two",
            UserType = "Doctor"
        };

        var doctor1 = new Doctor
        {
            Id = 1,
            AppUserId = "doctor1",
            FirstName = "Doctor",
            LastName = "One",
            RegistrationStatus = "Approved",
            LicenseNumber = "LIC001"
        };

        var doctor2 = new Doctor
        {
            Id = 2,
            AppUserId = "doctor2",
            FirstName = "Doctor",
            LastName = "Two",
            RegistrationStatus = "Approved",
            LicenseNumber = "LIC002"
        };

        var patient1User = new AppUser
        {
            Id = "patient1",
            Email = "patient1@test.com",
            UserName = "patient1@test.com",
            FirstName = "Patient",
            LastName = "One",
            UserType = "Patient"
        };

        var patient1 = new Patient
        {
            Id = 1,
            AppUserId = "patient1",
            FirstName = "Patient",
            LastName = "One",
            DoctorId = 2 // Already assigned to doctor2
        };

        dbContext.Users.AddRange(doctor1User, doctor2User, patient1User);
        dbContext.Doctors.AddRange(doctor1, doctor2);
        dbContext.Patients.Add(patient1);
        await dbContext.SaveChangesAsync();

        var controller = CreateControllerWithUser(dbContext, "doctor1", "doctor1@test.com");

        var assignDto = new AssignPatientDto
        {
            PatientId = 1
        };

        // Act
        var result = await controller.AssignPatient(assignDto);

        // Assert
        var badRequestResult = Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        var errorResponse = badRequestResult.Value;
        var messageProperty = errorResponse?.GetType().GetProperty("message");
        var message = messageProperty?.GetValue(errorResponse)?.ToString();
        
        Assert.Contains("already assigned to another doctor", message, StringComparison.OrdinalIgnoreCase);
    }
}

