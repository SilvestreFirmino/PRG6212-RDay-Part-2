using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;
using RaceDayMvc.Services;
namespace RaceDayMvc.Controllers;

public class ParticipantController : SiteController
{
    protected override string RequiredRole => "Participant";
    public ParticipantController(RaceDayClient api) : base(api) { }
    public async Task<IActionResult> Index() => View(await Api.Get<List<EntryPage>>("api/portal/enrolments"));
    public async Task<IActionResult> Results() => View(await Api.Get<List<EntryPage>>("api/portal/results"));
    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId)
    {
        try
        {
            await Api.Form(HttpMethod.Post, $"api/events/{eventId}/enrollments", Fields());
            TempData["Success"] = "Your entry has been recorded.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException error)
        {
            TempData["Error"] = error.Message;
            return RedirectToAction("Details", "Home", new { id = eventId });
        }
    }
}
