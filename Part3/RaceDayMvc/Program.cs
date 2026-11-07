using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Services;
namespace RaceDayMvc;

public class MvcProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllersWithViews(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = "RaceDay.Web.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.IdleTimeout = TimeSpan.FromMinutes(20);
        });
        builder.Services.AddScoped<RaceDayClient>();
        builder.Services.AddHttpClient("RaceDay", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });
        var app = builder.Build();
        app.UseExceptionHandler("/Home/Error");
        if (!builder.Configuration.GetValue<bool>("AllowHttp")) app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        app.Run();
    }
}
