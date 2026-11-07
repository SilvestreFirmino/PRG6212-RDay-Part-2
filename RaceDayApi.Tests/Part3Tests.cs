using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Services;
using RaceDayMvc;

namespace RaceDayApi.Tests;

public class Part3Tests
{
    private class ApiHost : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
                services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
                services.RemoveAll<IBannerStorage>();
                services.AddSingleton<IBannerStorage, MemoryBanners>();
            });
        }
    }
    private class WebHost : WebApplicationFactory<MvcProgram>
    {
        private readonly ApiHost _api;
        public WebHost(ApiHost api) { _api = api; }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services => services.AddHttpClient("RaceDay", c => c.BaseAddress = new Uri("https://localhost/"))
                .ConfigurePrimaryHttpMessageHandler(() => _api.Server.CreateHandler()));
        }
    }
    private class MemoryBanners : IBannerStorage
    {
        private byte[] _bytes = Array.Empty<byte>();
        public async Task<string> Upload(int id, Stream stream, string type)
        { using var copy = new MemoryStream(); await stream.CopyToAsync(copy); _bytes = copy.ToArray(); return "https://test.invalid/banner.png"; }
        public Task<(Stream Content,string ContentType)> Download(string url) =>
            Task.FromResult<(Stream,string)>((new MemoryStream(_bytes), "image/png"));
    }
    private static void Seed(ApiHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        db.Users.AddRange(new User { FirstName="Olivia",LastName="Organiser",Email="o@test.co.za",Role="Organiser",PasswordHash=new PasswordService().Hash("Password123!") },
            new User { FirstName="Pat",LastName="Participant",Email="p@test.co.za",Role="Participant",PasswordHash=new PasswordService().Hash("Password123!") });
        db.SaveChanges();
    }
    private static HttpClient Browser(WebHost host) => host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost"), AllowAutoRedirect=false });
    private static async Task<HttpResponseMessage> Post(HttpClient browser,string page,string action, params (string,string)[] fields)
    {
        var html=await browser.GetStringAsync(page);
        var token=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var values=fields.ToDictionary(f=>f.Item1,f=>f.Item2);
        values["__RequestVerificationToken"]=WebUtility.HtmlDecode(token);
        return await browser.PostAsync(action,new FormUrlEncodedContent(values));
    }

    [Fact]
    public async Task Signed_out_users_are_redirected_and_forged_posts_are_rejected()
    {
        using var api=new ApiHost(); using var web=new WebHost(api); using var browser=Browser(web);
        var result=await browser.GetAsync("/Organiser");
        Assert.Equal(HttpStatusCode.Redirect,result.StatusCode);
        Assert.Contains("/Account/Login",result.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.BadRequest,(await browser.PostAsync("/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
    }

    [Fact]
    public async Task Separate_visitors_keep_separate_roles_and_logout_clears_access()
    {
        using var api=new ApiHost(); Seed(api); using var web=new WebHost(api);
        using var organiser=Browser(web); using var participant=Browser(web);
        Assert.Equal(HttpStatusCode.Redirect,(await Post(organiser,"/Account/Login","/Account/Login",("Email","o@test.co.za"),("Password","Password123!"))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await Post(participant,"/Account/Login","/Account/Login",("Email","p@test.co.za"),("Password","Password123!"))).StatusCode);
        Assert.Contains("Organiser workspace",await organiser.GetStringAsync("/Organiser"));
        Assert.Contains("Participant space",await participant.GetStringAsync("/Participant"));
        Assert.Contains("This page is for Organiser",await participant.GetStringAsync("/Organiser"));
        await Post(organiser,"/Organiser","/Account/Logout");
        Assert.Equal(HttpStatusCode.Redirect,(await organiser.GetAsync("/Organiser")).StatusCode);
        Assert.Contains("Participant space",await participant.GetStringAsync("/Participant"));
    }

    [Fact]
    public async Task Registration_uses_API_and_invalid_login_is_friendly()
    {
        using var api=new ApiHost(); using var web=new WebHost(api); using var browser=Browser(web);
        var created=await Post(browser,"/Account/Register","/Account/Register",("FirstName","New"),("LastName","Runner"),
            ("Email","new@test.co.za"),("Password","Password123!"),("Role","Participant"),("DateOfBirth","2000-01-01"),
            ("Gender","Female"),("EmergencyContactName","Contact"),("EmergencyContactPhone","0712345678"));
        Assert.Equal(HttpStatusCode.Redirect,created.StatusCode);
        using var scope=api.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        Assert.Single(db.Users); Assert.Single(db.ParticipantProfiles);
        var rejected=await Post(browser,"/Account/Login","/Account/Login",("Email","new@test.co.za"),("Password","wrong"));
        Assert.Equal(HttpStatusCode.OK,rejected.StatusCode);
        Assert.Contains("Please sign in",await rejected.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Api_rejects_banner_from_non_owner_and_accepts_png_from_owner()
    {
        using var api=new ApiHost(); Seed(api);
        using(var scope=api.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.Events.Add(new RaceEvent { EventId=1,OrganiserId=db.Users.Single(u=>u.Role=="Organiser").UserId,Name="Test",Description="Test",EventType="Running",VenueName="Park",AddressLine1="Street",City="City",Province="Gauteng",Status="Published" }); db.SaveChanges();
        }
        using var client=api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost") });
        using var empty=new MultipartFormDataContent(); empty.Add(new ByteArrayContent(new byte[]{1,2,3}),"file","bad.png");
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsync("/api/events/1/banner",empty)).StatusCode);
        await client.PostAsync("/api/auth/login",new FormUrlEncodedContent(new Dictionary<string,string>{{"email","o@test.co.za"},{"password","Password123!"}}));
        using var bad=new MultipartFormDataContent(); bad.Add(new ByteArrayContent(new byte[]{1,2,3}),"file","bad.png");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/api/events/1/banner",bad)).StatusCode);
        using var png=new MultipartFormDataContent(); png.Add(new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jR9sAAAAASUVORK5CYII=")),"file","image.png");
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsync("/api/events/1/banner",png)).StatusCode);
        Assert.Equal("image/png",(await client.GetAsync("/api/events/1/banner")).Content.Headers.ContentType!.MediaType);
    }
}
