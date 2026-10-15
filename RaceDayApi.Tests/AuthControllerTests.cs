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
        var result = controller.Register(firstName: "Test", lastName: "Organiser", email: " TEST@example.com ", password: "Password123!", role: RegistrationRole.Organiser);
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
        Assert.IsType<BadRequestObjectResult>(controller.Register(firstName: "Test", lastName: "Participant", email: "test@example.com", password: "Password123!", role: RegistrationRole.Participant));
    }

    [Fact]
    public void Invalid_role_is_rejected()
    {
        Assert.IsType<BadRequestObjectResult>(Controller().Register(firstName: "", lastName: "", email: "", password: "", role: (RegistrationRole)99));
    }

    [Fact]
    public void Duplicate_email_is_rejected()
    {
        var controller = Controller();

        Assert.IsType<OkObjectResult>(controller.Register(firstName: "Test", lastName: "Organiser", email: "test@example.com", password: "Password123!", role: RegistrationRole.Organiser));
        Assert.IsType<ConflictObjectResult>(controller.Register(firstName: "Test", lastName: "Organiser", email: "test@example.com", password: "Password123!", role: RegistrationRole.Organiser));
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

        var register = controller.Register(firstName: "Nomsa", lastName: "Mthembu", email: "participant@test.co.za", password: "Password123!", role: RegistrationRole.Participant, dateOfBirth: new DateTime(1995, 1, 1), gender: "Female", emergencyContactName: "Sipho", emergencyContactPhone: "0712345678");
        Assert.IsType<OkObjectResult>(register);
        var login = controller.Login(email: "participant@test.co.za", password: "Password123!");
        Assert.NotNull(Assert.IsType<OkObjectResult>(login).Value);
    }

    [Fact]
    public void Incorrect_password_is_rejected()
    {
        var controller = Controller();
        controller.Register(firstName: "Lerato", lastName: "Mokoena", email: "organiser@test.co.za", password: "Password123!", role: RegistrationRole.Organiser);
        var result = controller.Login(email: "organiser@test.co.za", password: "wrong-password");
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
