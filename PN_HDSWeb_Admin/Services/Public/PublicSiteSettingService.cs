using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicSiteSettingService
{
    Task<Dictionary<string, string>> GetSettingsAsync(string maTruongBo);
}

public class PublicSiteSettingService : IPublicSiteSettingService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicSiteSettingService> _logger;

    public PublicSiteSettingService(ILogger<PublicSiteSettingService> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> GetSettingsAsync(string maTruongBo)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sql = $@"
            SELECT setting_key, setting_value
            FROM site_settings
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_active = TRUE
            ORDER BY setting_group, setting_key";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            foreach (DataRow row in dt.Rows)
            {
                var key = row["setting_key"]?.ToString();
                var value = row["setting_value"]?.ToString();

                if (!string.IsNullOrWhiteSpace(key))
                    result[key] = value ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetSettingsAsync failed. Public site will use fallback school data.");
        }

        return result;
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

internal static class PublicSiteSettingReader
{
    public static string? First(IReadOnlyDictionary<string, string> settings, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    public static int Int(IReadOnlyDictionary<string, string> settings, int fallback, int min, int max, params string[] keys)
    {
        var rawValue = First(settings, keys);
        if (!int.TryParse(rawValue, out var value))
            return fallback;

        return Math.Clamp(value, min, max);
    }

    public static bool Bool(IReadOnlyDictionary<string, string> settings, bool fallback, params string[] keys)
    {
        var rawValue = First(settings, keys);
        if (string.IsNullOrWhiteSpace(rawValue))
            return fallback;

        if (bool.TryParse(rawValue, out var value))
            return value;

        return rawValue.Trim() switch
        {
            "1" => true,
            "0" => false,
            var text when text.Equals("yes", StringComparison.OrdinalIgnoreCase) => true,
            var text when text.Equals("no", StringComparison.OrdinalIgnoreCase) => false,
            var text when text.Equals("on", StringComparison.OrdinalIgnoreCase) => true,
            var text when text.Equals("off", StringComparison.OrdinalIgnoreCase) => false,
            _ => fallback
        };
    }
}
