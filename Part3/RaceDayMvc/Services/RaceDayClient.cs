using System.Net;
using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace RaceDayMvc.Services;

public class ApiException : Exception
{
    public int Status { get; }
    public ApiException(string message, int status = 503) : base(message) { Status = status; }
}

// One small HTTP helper: every database operation still happens in the API.
public class RaceDayClient
{
    private readonly HttpClient _http;
    private readonly ISession _session;
    public RaceDayClient(IHttpClientFactory clients, IHttpContextAccessor context)
    {
        _http = clients.CreateClient("RaceDay");
        _session = context.HttpContext!.Session;
    }
    public async Task<HttpResponseMessage> Send(HttpMethod method, string path, HttpContent? body = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = body };
        var cookie = _session.GetString("ApiCookie");
        if (cookie != null) request.Headers.Add("Cookie", cookie);
        HttpResponseMessage response;
        try { response = await _http.SendAsync(request); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        { throw new ApiException("The RaceDay API is unavailable. Check that it is running and try again."); }
        if (response.IsSuccessStatusCode)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var headers))
            {
                var sessionCookie = SetCookieHeaderValue.ParseList(headers.ToList())
                    .FirstOrDefault(c => c.Name.ToString() == ".AspNetCore.Session");
                if (sessionCookie != null) _session.SetString("ApiCookie", sessionCookie.Name + "=" + sessionCookie.Value);
            }
            return response;
        }
        int status = (int)response.StatusCode;
        var message = status >= 500 ? "The service could not complete this request. Please try again." : await response.Content.ReadAsStringAsync();
        if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                message = doc.RootElement.TryGetProperty("errors", out var errors)
                    ? string.Join(" ", errors.EnumerateObject().SelectMany(e => e.Value.EnumerateArray().Select(v => v.GetString())))
                    : "Please check the entered details.";
            }
            catch (JsonException) { message = "Please check the entered details."; }
        }
        response.Dispose();
        if (status == 401) { _session.Clear(); message = "Please sign in with the correct account to continue."; }
        if (status == 404) message = "This record was not found or does not belong to your account.";
        throw new ApiException(string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message, status);
    }
    public async Task<T> Get<T>(string path)
    {
        using var response = await Send(HttpMethod.Get, path);
        try { return (await response.Content.ReadFromJsonAsync<T>())!; }
        catch (JsonException) { throw new ApiException("The API returned an unreadable response."); }
    }
    public async Task<int> Form(HttpMethod method, string path, IEnumerable<KeyValuePair<string,string>> fields)
    {
        using var content = new FormUrlEncodedContent(fields);
        using var response = await Send(method, path, content);
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("eventId", out var id)) return id.GetInt32();
        }
        return 0;
    }
    public async Task Banner(int eventId, IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        form.Add(new StreamContent(stream), "file", Path.GetFileName(file.FileName));
        using var response = await Send(HttpMethod.Post, $"api/events/{eventId}/banner", form);
    }
}
