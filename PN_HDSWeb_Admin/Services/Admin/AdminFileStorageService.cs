using Microsoft.AspNetCore.Components.Forms;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminFileStorageService
{
    Task<string?> SaveImageAsync(IBrowserFile file, string subFolder);
    Task<string?> SaveImageAsync(byte[] fileBytes, string fileName, string contentType, string subFolder);
    Task<string?> SaveFileAsync(IBrowserFile file, string subFolder);
    Task<bool> DeleteFileAsync(string? fileUrl);
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

    public async Task<string?> SaveImageAsync(byte[] fileBytes, string fileName, string contentType, string subFolder)
        => await SaveAsync(new MemoryFileUpload(fileBytes, fileName, contentType), subFolder);

    public async Task<string?> SaveFileAsync(IBrowserFile file, string subFolder)
        => await SaveAsync(file, subFolder);

    public Task<bool> DeleteFileAsync(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.FromResult(false);

        var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath, relativePath);
        if (!File.Exists(fullPath)) return Task.FromResult(false);

        try
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

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

    private sealed class MemoryFileUpload : IBrowserFile
    {
        private readonly byte[] _bytes;

        public MemoryFileUpload(byte[] bytes, string name, string contentType)
        {
            _bytes = bytes;
            Name = name;
            ContentType = contentType;
            LastModified = DateTimeOffset.UtcNow;
        }

        public string Name { get; }
        public string ContentType { get; }
        public DateTimeOffset LastModified { get; }
        public long Size => _bytes.LongLength;

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(_bytes, writable: false);
    }
}
