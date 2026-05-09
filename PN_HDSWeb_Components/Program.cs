using hDataLibraryN8;
using hUltiLibraryN8;

using Serilog.Events;
using Serilog;
using PN_HDSWeb_Components.Data;

namespace PN_HDSWeb_Components
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<WeatherForecastService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            // Khởi tạo logger
            string error_folder = hJsonLib.hgetValueJF(hConstants.PN_CLIENT_CONFIG_FILE, "LOGS", "error_folder");
            string info_folder = hJsonLib.hgetValueJF(hConstants.PN_CLIENT_CONFIG_FILE, "LOGS", "info_folder");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File($"{info_folder}\\pn_inf-.txt", LogEventLevel.Information, rollingInterval: RollingInterval.Day)
                .WriteTo.File($"{error_folder}\\pn_err-.txt", LogEventLevel.Error, rollingInterval: RollingInterval.Day)
                .CreateLogger();


            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}
