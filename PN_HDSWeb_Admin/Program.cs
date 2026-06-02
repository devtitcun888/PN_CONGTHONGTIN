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
using PN_HDSWeb_Components.Data;
using Radzen;
using PN_HDSWeb_Admin.Services.Auth;
using PN_HDSWeb_Admin.Services.Schools;
using PN_HDSWeb_Admin.Services.Admin;
using PN_HDSWeb_Admin.Services.Content;

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

builder.Services.AddScoped<HttpContentService>();
builder.Services.AddScoped<HttpClient>();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<IAdminLoginService, AdminLoginService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();
builder.Services.AddScoped<IAdminAccountService, AdminAccountService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminPostService, AdminPostService>();
builder.Services.AddScoped<IAdminPostCategoryService, AdminPostCategoryService>();
builder.Services.AddScoped<IAdminPostTagService, AdminPostTagService>();
builder.Services.AddScoped<IAdminPostMediaService, AdminPostMediaService>();
builder.Services.AddScoped<IAdminPostTagMapService, AdminPostTagMapService>();
builder.Services.AddScoped<IAdminDocumentService, AdminDocumentService>();
builder.Services.AddScoped<IAdminDocumentTypeService, AdminDocumentTypeService>();
builder.Services.AddScoped<IAdminDocumentVersionService, AdminDocumentVersionService>();
builder.Services.AddScoped<IAdminBannerService, AdminBannerService>();
builder.Services.AddScoped<IAdminMenuService, AdminMenuService>();
builder.Services.AddScoped<IAdminSiteSettingService, AdminSiteSettingService>();
builder.Services.AddScoped<IAdminFileStorageService, AdminFileStorageService>();
builder.Services.AddScoped<IAdminStaticPageService, AdminStaticPageService>();
builder.Services.AddScoped<IAdminStaffProfileService, AdminStaffProfileService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicSiteSettingService, PN_HDSWeb_Admin.Services.Public.PublicSiteSettingService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicNavigationService, PN_HDSWeb_Admin.Services.Public.PublicNavigationService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicHomepageService, PN_HDSWeb_Admin.Services.Public.PublicHomepageService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicStaticPageService, PN_HDSWeb_Admin.Services.Public.PublicStaticPageService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicPostCategoryService, PN_HDSWeb_Admin.Services.Public.PublicPostCategoryService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicPostTagService, PN_HDSWeb_Admin.Services.Public.PublicPostTagService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicPostMediaService, PN_HDSWeb_Admin.Services.Public.PublicPostMediaService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicPostService, PN_HDSWeb_Admin.Services.Public.PublicPostService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicDocumentService, PN_HDSWeb_Admin.Services.Public.PublicDocumentService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicDocumentVersionService, PN_HDSWeb_Admin.Services.Public.PublicDocumentVersionService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicDocumentTypeService, PN_HDSWeb_Admin.Services.Public.PublicDocumentTypeService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicContactService, PN_HDSWeb_Admin.Services.Public.PublicContactService>();
builder.Services.AddScoped<PN_HDSWeb_Admin.Services.Public.IPublicSearchService, PN_HDSWeb_Admin.Services.Public.PublicSearchService>();
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<BrowserStorageService>();
builder.Services.AddScoped<TabSessionService>();
builder.Services.AddScoped<UserState>();
builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<PN_Sessions>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorOnly", policy =>
        policy.RequireRole("Administrator"));
});

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
