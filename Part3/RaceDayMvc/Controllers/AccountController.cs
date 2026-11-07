using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;
using RaceDayMvc.Services;

namespace RaceDayMvc.Controllers;

public class AccountController : SiteController
{
    public AccountController(RaceDayClient api) : base(api) { }
    [HttpGet] public IActionResult Login() => View(new LoginForm());
    [HttpPost]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        HttpContext.Session.Clear();
        try
        {
            await Api.Form(HttpMethod.Post, "api/auth/login", Fields());
            var profile = await Api.Get<Profile>("api/profile");
            if (profile.Role != "Organiser" && profile.Role != "Participant") throw new ApiException("Unknown account role.");
            HttpContext.Session.SetInt32("UserId", profile.UserId);
            HttpContext.Session.SetString("Role", profile.Role);
            HttpContext.Session.SetString("FirstName", profile.FirstName);
            return profile.Role == "Organiser" ? RedirectToAction("Index", "Organiser") : RedirectToAction("Index", "Home");
        }
        catch (ApiException error) { HttpContext.Session.Clear(); ModelState.AddModelError("", error.Message); return View(form); }
    }
    [HttpGet] public IActionResult Register() => View(new RegisterPage());
    [HttpPost]
    public async Task<IActionResult> Register(RegisterPage form)
    {
        if (!ModelState.IsValid || !await Save(HttpMethod.Post, "api/auth/register")) return View(form);
        TempData["Success"] = "Account created. Sign in with your email and password.";
        return RedirectToAction(nameof(Login));
    }
    [HttpGet] public IActionResult Dashboard() => RedirectToAction("Index", HttpContext.Session.GetString("Role") == "Organiser" ? "Organiser" : "Participant");
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try { using var response = await Api.Send(HttpMethod.Post, "api/auth/logout"); }
        catch (ApiException) { /* Always clear the local session even if the API is offline. */ }
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
