using eBolnicaAPI.Controllers;
using eBolnicaAPI.Data;
using eBolnicaAPI.Models.DTOs;
using eBolnicaAPI.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace eBolnicaAPI.Tests.Unit
{
    public class AdminControllerTests
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
        public async Task CreateUser_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var mockUserManager = GetMockUserManager();
            var dbContext = GetInMemoryDbContext();

            mockUserManager
                .Setup(um => um.FindByEmailAsync("existing@email.com"))
                .ReturnsAsync(new AppUser { Email = "existing@email.com" });

            var controller = new AdminController(dbContext, mockUserManager.Object);

            var dto = new CreateUserDto
            {
                Email = "existing@email.com",
                FirstName = "Test",
                LastName = "User",
                Password = "Test123!",
                UserType = "Patient"
            };

            // Act
            var result = await controller.CreateUser(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest);
        }

        [Fact]
        public async Task DeleteUser_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var mockUserManager = GetMockUserManager();
            var dbContext = GetInMemoryDbContext();

            mockUserManager
                .Setup(um => um.FindByIdAsync("invalid-id"))
                .ReturnsAsync((AppUser?)null);

            var controller = new AdminController(dbContext, mockUserManager.Object);

            // Act
            var result = await controller.DeleteUser("invalid-id");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
