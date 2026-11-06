using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

public class AccountController : Controller
{
    private readonly HttpClient _api;
    public AccountController(IHttpClientFactory clients) => _api = clients.CreateClient("RaceDay");

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);
        HttpContext.Session.Clear();
        try
        {
            // Send the same two form fields that Swagger sends.
            using var fields = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = form.Email, ["password"] = form.Password
            });
            using var login = await _api.PostAsync("api/auth/login", fields);
            if (login.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return LoginError("Incorrect email or password.");

            login.EnsureSuccessStatusCode();
            if (!login.Headers.TryGetValues("Set-Cookie", out var headers))
                return LoginError("The API did not start a session. Please try again.");
            var session = SetCookieHeaderValue.ParseList(headers.ToList())
                .FirstOrDefault(c => c.Name.ToString() == ".AspNetCore.Session");
            if (session == null) return LoginError("The API did not start a session. Please try again.");

            string cookie = session.Name + "=" + session.Value;
            var profile = await ReadProfile(cookie);
            if (profile == null || (profile.Role != "Organiser" && profile.Role != "Participant"))
                return LoginError("We could not open your account.");

            HttpContext.Session.SetString("ApiCookie", cookie);
            HttpContext.Session.SetInt32("UserId", profile.UserId);
            HttpContext.Session.SetString("Role", profile.Role);
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return LoginError("RaceDay could not reach your account. Check that the API is running and try again.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        string? cookie = HttpContext.Session.GetString("ApiCookie");
        if (cookie == null) return RedirectToAction(nameof(Login));
        try
        {
            // Recheck the API session so an expired login cannot open a protected page.
            var profile = await ReadProfile(cookie);
            if (profile != null && profile.Role == HttpContext.Session.GetString("Role"))
                return View(profile);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return LoginError("Your account could not be loaded. Please try signing in again.");
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var cookie = HttpContext.Session.GetString("ApiCookie");
        HttpContext.Session.Clear();
        if (cookie != null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
            request.Headers.Add("Cookie", cookie);
            try { using var response = await _api.SendAsync(request); }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            { /* Local sign-out still takes effect when the API is offline. */ }
        }
        return RedirectToAction(nameof(Login));
    }

    private async Task<Profile?> ReadProfile(string cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/profile");
        request.Headers.Add("Cookie", cookie);
        using var response = await _api.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Profile>();
    }

    private IActionResult LoginError(string message)
    {
        ModelState.AddModelError("", message);
        return View("Login");
    }
}
