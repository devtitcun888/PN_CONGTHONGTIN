using Microsoft.AspNetCore.Components.Forms;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminFileStorageService
{
    Task<string?> SaveImageAsync(IBrowserFile file, string subFolder);
    Task<string?> SaveFileAsync(IBrowserFile file, string subFolder);
}

public class AdminFileStorageService : IAdminFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public AdminFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string?> SaveImageAsync(IBrowserFile file, string subFolder)
        => await SaveAsync(file, subFolder);

    public async Task<string?> SaveFileAsync(IBrowserFile file, string subFolder)
        => await SaveAsync(file, subFolder);

    private async Task<string?> SaveAsync(IBrowserFile file, string subFolder)
    {
        if (file == null) return null;

        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsRoot);

        var safeName = Path.GetFileNameWithoutExtension(file.Name);
        var ext = Path.GetExtension(file.Name);
        var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using var stream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024);
        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs);

        return $"/uploads/{subFolder}/{fileName}";
    }
}
