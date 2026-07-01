using hDataLibraryN8;
using hUltiLibraryN8;

using Serilog.Events;
using Serilog;
using PN_HDSWeb_Admin.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using PN_HDSWeb_Admin.Authentication;
using MudBlazor.Services;
using PN_HDSWeb_Library;
using PN_HDSWeb_Admin.Hubs;
using Syncfusion.Licensing;

using Radzen;
using PN_HDSWeb_Admin.Services.Auth;
using PN_HDSWeb_Admin.Services.Admin;
using PN_HDSWeb_Admin.Services.Public;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthenticationCore();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpClient();
builder.Services.AddLogging();

builder.Services.AddBlazorBootstrap();
Console.OutputEncoding = System.Text.Encoding.UTF8;
builder.Services.AddMudServices();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    });

// ---- Core / Infrastructure ----
builder.Services.AddScoped<HttpContentService>();
builder.Services.AddScoped<HttpClient>();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<BrowserStorageService>();
builder.Services.AddScoped<UserState>();
builder.Services.AddSingleton<PN_Sessions>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddHttpContextAccessor();

// ---- Auth ----
builder.Services.AddScoped<IAdminLoginService, AdminLoginService>();
builder.Services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();
builder.Services.AddScoped<IAdminAccountService, AdminAccountService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();

// ---- Admin Services (Xe điện) ----
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminVehicleService, AdminVehicleService>();
builder.Services.AddScoped<IAdminRentalService, AdminRentalService>();
builder.Services.AddScoped<IAdminFileStorageService, AdminFileStorageService>();

// ---- Public Services (Luồng khách) ----
builder.Services.AddScoped<IPublicVehicleService, PublicVehicleService>();
builder.Services.AddScoped<IPublicRentalService, PublicRentalService>();
builder.Services.AddScoped<IPublicCustomerService, PublicCustomerService>();
builder.Services.AddScoped<IPublicSiteSettingService, PublicSiteSettingService>();
builder.Services.AddScoped<IPublicNavigationService, PublicNavigationService>();

// ---- Authorization ----
builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorOnly", policy =>
        policy.RequireRole("Administrator"));
});

// ---- 3rd party ----
builder.Services.AddRadzenComponents();

SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBMAY9C3t2U1hhQlJBfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hTX5bdEZjXHxecnZVQGRa");

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

string error_folder = hJsonLib.hgetValueJF(hConstants.PN_CLIENT_CONFIG_FILE, "LOGS", "error_folder");
string info_folder = hJsonLib.hgetValueJF(hConstants.PN_CLIENT_CONFIG_FILE, "LOGS", "info_folder");
Log.Logger = new LoggerConfiguration()
    .WriteTo.File($"{info_folder}\\pn_inf-.txt", LogEventLevel.Information, rollingInterval: RollingInterval.Day)
    .WriteTo.File($"{error_folder}\\pn_err-.txt", LogEventLevel.Error, rollingInterval: RollingInterval.Day)
    .CreateLogger();

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseStaticFiles(new StaticFileOptions()
{
    OnPrepareResponse = r =>
    {
        string path = r.File.PhysicalPath ?? string.Empty;
        if (path.EndsWith(".css") || path.EndsWith(".js") ||
            path.EndsWith(".gif") || path.EndsWith(".jpg") ||
            path.EndsWith(".png") || path.EndsWith(".svg") || path.EndsWith(".webp"))
        {
            TimeSpan maxAge = new TimeSpan(7, 0, 0, 0);
            r.Context.Response.Headers.Append("Cache-Control", $"public, max-age={maxAge.TotalSeconds:0}");
        }
    },
});

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapHub<DataHub>(DataHub.Endpoint);

app.Run();
