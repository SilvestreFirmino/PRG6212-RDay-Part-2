using Azure;
using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Data;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[Route("api/events/{eventId}/banner")]
public class BannersController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBannerStorage _storage;
    public BannersController(RaceDayDbContext db, IBannerStorage storage) { _db = db; _storage = storage; }

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(int eventId, IFormFile file)
    {
        if (!IsOrganiser()) return Unauthorized("Only organisers may upload banners.");
        var race = _db.Events.Find(eventId);
        if (race == null || race.OrganiserId != CurrentUserId) return NotFound("Your event was not found.");
        if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024)
            return BadRequest("Choose a JPEG or PNG image up to 5 MB.");
        using var bytes = new MemoryStream();
        await file.CopyToAsync(bytes);
        var data = bytes.ToArray();
        bool png = data.Length >= 8 && data.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
        bool jpeg = data.Length >= 3 && data[0] == 255 && data[1] == 216 && data[2] == 255;
        if (!png && !jpeg) return BadRequest("Only JPEG and PNG images are accepted.");
        bytes.Position = 0;
        try
        {
            race.BannerUrl = await _storage.Upload(eventId, bytes, png ? "image/png" : "image/jpeg");
            _db.SaveChanges();
            return Ok("Banner uploaded successfully.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or RequestFailedException)
        {
            return StatusCode(503, "Banner storage is unavailable. Check the API Azure Storage settings.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(int eventId)
    {
        var race = _db.Events.Find(eventId);
        if (race == null || race.BannerUrl == null) return NotFound();
        if (race.Status != "Published" && race.Status != "Completed" && race.OrganiserId != CurrentUserId)
            return NotFound();
        try
        {
            var image = await _storage.Download(race.BannerUrl);
            return File(image.Content, image.ContentType);
        }
        catch (Exception ex) when (ex is InvalidOperationException or RequestFailedException)
        { return NotFound(); }
    }
}
