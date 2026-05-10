using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminSiteSettingService
{
    Task<List<AdminSiteSettingItem>> GetSettingsAsync(string maTruongBo);
    Task<AdminSiteSettingDetail?> GetSettingByIdAsync(string id);
    Task<bool> CreateSettingAsync(AdminSiteSettingDetail model);
    Task<bool> UpdateSettingAsync(AdminSiteSettingDetail model);
    Task<bool> DeleteSettingAsync(string id);
    Task<bool> SetActiveAsync(string id, bool isActive);
}

public class AdminSiteSettingService : IAdminSiteSettingService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminSiteSettingService> _logger;

    public AdminSiteSettingService(ILogger<AdminSiteSettingService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminSiteSettingItem>> GetSettingsAsync(string maTruongBo)
    {
        var result = new List<AdminSiteSettingItem>();
        var sql = $@"
            SELECT id, setting_key, setting_value, setting_group, is_active
            FROM site_settings
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
            ORDER BY setting_group, setting_key";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new AdminSiteSettingItem
                {
                    Id = row["id"]?.ToString(),
                    SettingKey = row["setting_key"]?.ToString(),
                    SettingValue = row["setting_value"]?.ToString(),
                    SettingGroup = row["setting_group"]?.ToString(),
                    IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSettingsAsync failed");
            throw;
        }

        return result;
    }

    public async Task<AdminSiteSettingDetail?> GetSettingByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, setting_key, setting_value, setting_group, description, is_active
            FROM site_settings
            WHERE id = '{Escape(id)}'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new AdminSiteSettingDetail
        {
            Id = row["id"]?.ToString(),
            MaTruongBo = row["ma_truong_bo"]?.ToString(),
            SettingKey = row["setting_key"]?.ToString(),
            SettingValue = row["setting_value"]?.ToString(),
            SettingGroup = row["setting_group"]?.ToString(),
            Description = row["description"]?.ToString(),
            IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<bool> CreateSettingAsync(AdminSiteSettingDetail model)
    {
        var sql = $@"
            INSERT INTO site_settings
            (ma_truong_bo, setting_key, setting_value, setting_group, description, is_active, updated_at)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.SettingKey)}', '{Escape(model.SettingValue)}', '{Escape(model.SettingGroup)}',
             '{Escape(model.Description)}', {(model.IsActive ? "TRUE" : "FALSE")}, NOW())";

        return await RunAsync(sql, "CreateSettingAsync");
    }

    public async Task<bool> UpdateSettingAsync(AdminSiteSettingDetail model)
    {
        var sql = $@"
            UPDATE site_settings
               SET setting_key = '{Escape(model.SettingKey)}',
                   setting_value = '{Escape(model.SettingValue)}',
                   setting_group = '{Escape(model.SettingGroup)}',
                   description = '{Escape(model.Description)}',
                   is_active = {(model.IsActive ? "TRUE" : "FALSE")},
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateSettingAsync");
    }

    public async Task<bool> DeleteSettingAsync(string id)
    {
        var sql = $@"
            DELETE FROM site_settings WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteSettingAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE site_settings
               SET is_active = {(isActive ? "TRUE" : "FALSE")},
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "SetActiveAsync");
    }

    private async Task<bool> RunAsync(string sql, string action)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID_Index, sql);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Action} failed", action);
            throw;
        }
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminSiteSettingItem
{
    public string? Id { get; set; }
    public string? SettingKey { get; set; }
    public string? SettingValue { get; set; }
    public string? SettingGroup { get; set; }
    public bool IsActive { get; set; }
}

public class AdminSiteSettingDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? SettingKey { get; set; }
    public string? SettingValue { get; set; }
    public string? SettingGroup { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public static class SiteSettingKeys
{
    public const string SiteName = "site_name";
    public const string SiteSlogan = "site_slogan";
    public const string SchoolName = "school_name";
    public const string SchoolLogo = "school_logo";
    public const string SchoolFavicon = "school_favicon";
    public const string SchoolAddress = "school_address";
    public const string SchoolPhone = "school_phone";
    public const string SchoolEmail = "school_email";
    public const string SchoolWebsite = "school_website";
    public const string ContactHotline = "contact_hotline";
    public const string ContactPhone = "contact_phone";
    public const string ContactEmail = "contact_email";
    public const string ContactFacebook = "contact_facebook";
    public const string ContactYoutube = "contact_youtube";
    public const string ContactZalo = "contact_zalo";
    public const string ContactMapUrl = "contact_map_url";
    public const string FooterText = "footer_text";
    public const string FooterShowLogo = "footer_show_logo";
    public const string FooterShowContact = "footer_show_contact";
    public const string FooterShowSocial = "footer_show_social";
    public const string FooterJsonInfo = "footer_json_info";
    public const string BrandPrimaryColor = "brand_primary_color";
    public const string BrandSecondaryColor = "brand_secondary_color";
    public const string HomepageFeaturedPostsLimit = "homepage_featured_posts_limit";
    public const string HomepageLatestPostsLimit = "homepage_latest_posts_limit";
    public const string HomepageDocumentsLimit = "homepage_documents_limit";
    public const string FeatureNewsEnabled = "feature_news_enabled";
    public const string FeatureDocumentsEnabled = "feature_documents_enabled";
    public const string FeatureBannersEnabled = "feature_banners_enabled";
    public const string FeatureSearchEnabled = "feature_search_enabled";
    public const string SeoTitle = "seo_title";
    public const string SeoDescription = "seo_description";
    public const string SeoKeywords = "seo_keywords";
}
