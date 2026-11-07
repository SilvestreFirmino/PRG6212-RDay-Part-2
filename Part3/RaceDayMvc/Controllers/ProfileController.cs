using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;
using RaceDayMvc.Services;
namespace RaceDayMvc.Controllers;

public class ProfileController : SiteController
{
    protected override string RequiredRole => "Any";
    public ProfileController(RaceDayClient api) : base(api) { }
    [HttpGet] public async Task<IActionResult> Index() => View(await Api.Get<ProfilePage>("api/profile"));
    [HttpPost]
    public async Task<IActionResult> Index(ProfilePage form)
    {
        // Map the nested display fields to the API's individual input names.
        var fields = Fields().Select(f => new KeyValuePair<string,string>(
            f.Key.Replace("ParticipantProfile.", ""), f.Value));
        try
        {
            if (!ModelState.IsValid) return View(form);
            await Api.Form(HttpMethod.Put, "api/profile", fields);
            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException error) { ModelState.AddModelError("", error.Message); return View(form); }
    }
}
