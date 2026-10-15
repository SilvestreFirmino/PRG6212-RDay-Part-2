using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Controllers;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Tests;

public class AuthControllerTests
{
    [Fact]
    public void Organiser_registration_saves_names_and_hashed_password()
    {
        using var db = TestHelpers.Database();
        var controller = new AuthController(db, new PasswordService());
        var result = controller.Register(new RegistrationDetails { FirstName = "Test", LastName = "Organiser", Email = " TEST@example.com ", Password = "Password123!", Role = RegistrationRole.Organiser });
        Assert.IsType<OkObjectResult>(result);
        var saved = Assert.Single(db.Users);
        Assert.Equal("Test", saved.FirstName);
        Assert.Equal("Organiser", saved.LastName);
        Assert.Equal("test@example.com", saved.Email);
        Assert.Equal(Roles.Organiser, saved.Role);
        Assert.True(new PasswordService().Verify("Password123!", saved.PasswordHash));
        Assert.Empty(saved.Password);
    }

    [Fact]
    public void Missing_participant_details_are_rejected()
    {
        var controller = Controller();
        Assert.IsType<BadRequestObjectResult>(controller.Register(new RegistrationDetails { FirstName = "Test", LastName = "Participant", Email = "test@example.com", Password = "Password123!", Role = RegistrationRole.Participant }));
    }

    [Fact]
    public void Invalid_role_is_rejected()
    {
        Assert.IsType<BadRequestObjectResult>(Controller().Register(new RegistrationDetails { Role = (RegistrationRole)99 }));
    }

    [Fact]
    public void Duplicate_email_is_rejected()
    {
        var controller = Controller();
        var details = new RegistrationDetails { FirstName = "Test", LastName = "Organiser", Email = "test@example.com", Password = "Password123!", Role = RegistrationRole.Organiser };
        Assert.IsType<OkObjectResult>(controller.Register(details));
        Assert.IsType<ConflictObjectResult>(controller.Register(details));
    }

    private static AuthController Controller()
    {
        var controller = new AuthController(TestHelpers.Database(), new PasswordService());
        TestHelpers.SetUser(controller, 0, string.Empty);
        return controller;
    }

    [Fact]
    public void Participant_registration_and_login_work()
    {
        var controller = Controller();
        var request = new RegistrationDetails { Email = "participant@test.co.za", Password = "Password123!", FirstName = "Nomsa", LastName = "Mthembu", Role = RegistrationRole.Participant, DateOfBirth = new DateTime(1995, 1, 1), Gender = "Female", EmergencyContactName = "Sipho", EmergencyContactPhone = "0712345678" };
        var register = controller.Register(request);
        Assert.IsType<OkObjectResult>(register);
        var login = controller.Login(new LoginDetails { Email = request.Email, Password = "Password123!" });
        Assert.NotNull(Assert.IsType<OkObjectResult>(login).Value);
    }

    [Fact]
    public void Incorrect_password_is_rejected()
    {
        var controller = Controller();
        controller.Register(new RegistrationDetails { Email = "organiser@test.co.za", Password = "Password123!", FirstName = "Lerato", LastName = "Mokoena", Role = RegistrationRole.Organiser });
        var result = controller.Login(new LoginDetails { Email = "organiser@test.co.za", Password = "wrong-password" });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
