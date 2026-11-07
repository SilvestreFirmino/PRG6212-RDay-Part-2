using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;
using RaceDayMvc.Services;
namespace RaceDayMvc.Controllers;

public class OrganiserController : SiteController
{
    protected override string RequiredRole => "Organiser";
    public OrganiserController(RaceDayClient api) : base(api) { }
    public async Task<IActionResult> Index() => View(await Api.Get<List<EventPage>>("api/portal/organiser"));
    [HttpGet] public IActionResult Create() => View("Edit", new EventPage());
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var race = await Api.Get<EventPage>($"api/events/{id}");
        if (race.OrganiserId != HttpContext.Session.GetInt32("UserId")) return View("Problem", "This event belongs to another organiser.");
        return View(race);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(EventPage form, IFormFile? banner)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            int id = await Api.Form(form.EventId == 0 ? HttpMethod.Post : HttpMethod.Put,
                form.EventId == 0 ? "api/events" : $"api/events/{form.EventId}", Fields());
            if (form.EventId == 0) form.EventId = id;
            if (banner != null && banner.Length > 0)
            {
                try { await Api.Banner(form.EventId, banner); }
                catch (ApiException error)
                {
                    TempData["Error"] = "Event saved, but the banner was not uploaded. " + error.Message;
                    return RedirectToAction(nameof(Edit), new { id = form.EventId });
                }
            }
            TempData["Success"] = "Event saved. Add categories and publish when ready.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException error) { ModelState.AddModelError("", error.Message); return View(form); }
    }
    [HttpPost]
    public async Task<IActionResult> Status(int eventId)
    {
        await Api.Form(HttpMethod.Put, $"api/events/{eventId}/status", Fields());
        TempData["Success"] = "Event status updated.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int eventId)
    {
        using var response = await Api.Send(HttpMethod.Delete, $"api/events/{eventId}");
        TempData["Success"] = "Event deleted.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Categories(int id)
    {
        var race = await Api.Get<EventPage>($"api/events/{id}");
        if (race.OrganiserId != HttpContext.Session.GetInt32("UserId")) return View("Problem", "This event belongs to another organiser.");
        race.Categories = await Api.Get<List<CategoryPage>>($"api/events/{id}/categories");
        return View(race);
    }
    [HttpGet]
    public async Task<IActionResult> Category(int eventId, int categoryId = 0)
    {
        var race = await Api.Get<EventPage>($"api/events/{eventId}");
        if (race.OrganiserId != HttpContext.Session.GetInt32("UserId")) return View("Problem", "This event belongs to another organiser.");
        var items = await Api.Get<List<CategoryPage>>($"api/events/{eventId}/categories");
        var model = categoryId == 0 ? new CategoryPage { EventId = eventId } : items.FirstOrDefault(c => c.CategoryId == categoryId);
        if (model == null) return View("Problem", "Category not found.");
        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Category(CategoryPage form)
    {
        if (!ModelState.IsValid || !await Save(form.CategoryId == 0 ? HttpMethod.Post : HttpMethod.Put,
                form.CategoryId == 0 ? $"api/events/{form.EventId}/categories" : $"api/categories/{form.CategoryId}")) return View(form);
        return RedirectToAction(nameof(Categories), new { id = form.EventId });
    }
    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        using var response = await Api.Send(HttpMethod.Delete, $"api/categories/{categoryId}");
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }
    public async Task<IActionResult> Enrolments(int id)
    {
        ViewBag.EventId = id;
        return View(await Api.Get<List<EntryPage>>($"api/portal/events/{id}/enrolments"));
    }
    [HttpGet]
    public async Task<IActionResult> Result(int eventId, int enrollmentId)
    {
        var entries = await Api.Get<List<EntryPage>>($"api/portal/events/{eventId}/enrolments");
        var entry = entries.FirstOrDefault(e => e.EnrollmentId == enrollmentId);
        if (entry == null) return View("Problem", "Enrolment not found.");
        entry.ResultStatus ??= "Finished";
        return View(entry);
    }
    [HttpPost]
    public async Task<IActionResult> Result(EntryPage form)
    {
        string path = form.ResultId.HasValue ? $"api/results/{form.ResultId}" : $"api/enrollments/{form.EnrollmentId}/result";
        if (!ModelState.IsValid || !await Save(form.ResultId.HasValue ? HttpMethod.Put : HttpMethod.Post, path)) return View(form);
        TempData["Success"] = "Result published.";
        return RedirectToAction(nameof(Enrolments), new { id = form.EventId });
    }
}
