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
//using PN_HDSWeb_HocTap.Data;
using Syncfusion.Licensing;
using PN_HDSWeb_Components.Data;
using Radzen;
using PN_HDSWeb_Admin.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthenticationCore();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
// Thêm Authentication & Authorization
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpClient();
// Thêm logging
builder.Services.AddLogging();

builder.Services.AddBlazorBootstrap();

Console.OutputEncoding = System.Text.Encoding.UTF8;
builder.Services.AddMudServices();
builder.Services.AddDistributedMemoryCache();
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
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);  // Adjust timeout
    });
//builder.Services.AddScoped<SessionService>();
//builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<HttpContentService>();
builder.Services.AddScoped<HttpClient>();

builder.Services.AddScoped<TokenProvider>();

builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddSingleton<UserAccountService>();
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

    options.AddPolicy("TeacherOnly", policy =>
        policy.RequireRole("GiaoVien"));

    options.AddPolicy("AdminOrTeacher", policy =>
        policy.RequireRole("Administrator", "GiaoVien"));
});

SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBMAY9C3t2U1hhQlJBfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hTX5bdEZjXHxecnZVQGRa");

//anthen
//Signal R
builder.Services.AddSignalR();
//builder.Services.AddHostedService<NotificationServices>();

var app = builder.Build();

// ── HTTP PIPELINE ────────────────────────────────────────────────────────────
// Thứ tự QUAN TRỌNG: Response Compression phải đứng đầu để compress mọi response

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Khởi tạo logger
string error_folder = hJsonLib.hgetValueJF(hConstants.PN_CLIENT_CONFIG_FILE, "LOGS", "error_folder");
string info_folder = hJsonLib.hgetValueJF(hConstants.PN_CLIENT_CONFIG_FILE, "LOGS", "info_folder");
Log.Logger = new LoggerConfiguration()
    .WriteTo.File($"{info_folder}\\pn_inf-.txt", LogEventLevel.Information, rollingInterval: RollingInterval.Day)
    .WriteTo.File($"{error_folder}\\pn_err-.txt", LogEventLevel.Error, rollingInterval: RollingInterval.Day)
    .CreateLogger();

// ✅ THÊM: Response Compression (Brotli/Gzip) — phải đứng trước StaticFiles
app.UseResponseCompression();

app.UseHttpsRedirection();

// ✅ UseRouting đặt TRƯỚC MapX (đúng thứ tự chuẩn)
app.UseRouting();

app.UseSession();

// Static files với cache headers (chỉ 1 lần — đã xóa duplicate)
app.UseStaticFiles(new StaticFileOptions()
{
    OnPrepareResponse = r =>
    {
        string path = r.File.PhysicalPath ?? string.Empty;
        if (path.EndsWith(".css") || path.EndsWith(".js") ||
            path.EndsWith(".gif") || path.EndsWith(".jpg") ||
            path.EndsWith(".png") || path.EndsWith(".svg") || path.EndsWith(".webp"))
        {
            TimeSpan maxAge = new TimeSpan(7, 0, 0, 0); // 7 ngày (giảm từ 370 ngày)
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
