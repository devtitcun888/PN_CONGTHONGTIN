using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicNavigationService
{
    Task<List<PublicNavItem>> GetMenusAsync(string maTruongBo);
    Task<List<PublicBannerItem>> GetBannersAsync(string maTruongBo);
    Task<PublicFooterInfo> GetFooterAsync(string maTruongBo);
    Task<Dictionary<string, string>> GetSiteSettingsAsync(string maTruongBo);
    string ResolveMenuUrl(PublicNavItem item);
}

public class PublicNavigationService : IPublicNavigationService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicNavigationService> _logger;
    private readonly IPublicSiteSettingService _siteSettingService;
    private readonly IMemoryCache _cache;

    public PublicNavigationService(ILogger<PublicNavigationService> logger, IPublicSiteSettingService siteSettingService, IMemoryCache cache)
    {
        _logger = logger;
        _siteSettingService = siteSettingService;
        _cache = cache;
    }

    public async Task<List<PublicNavItem>> GetMenusAsync(string maTruongBo)
    {
        string cacheKey = $"Menus_{maTruongBo}";
        if (_cache.TryGetValue(cacheKey, out List<PublicNavItem>? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var result = new List<PublicNavItem>();
        var sql = $@"
            SELECT id, menu_name, url, target, parent_id, sort_order, page_slug, page_type
            FROM menus
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
            ORDER BY sort_order ASC, created_at ASC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicNavItem
            {
                Id = row["id"]?.ToString(),
                MenuName = row["menu_name"]?.ToString(),
                Url = row["url"]?.ToString(),
                Target = row["target"]?.ToString(),
                ParentId = row["parent_id"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
                PageSlug = row["page_slug"]?.ToString(),
                PageType = row["page_type"]?.ToString()
            });
        }

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(2));
        return result;
    }

    public async Task<List<PublicBannerItem>> GetBannersAsync(string maTruongBo)
    {
        var result = new List<PublicBannerItem>();
        var sql = $@"
            SELECT id, title, image_url, link_url, position, sort_order
            FROM banners
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicBannerItem
            {
                Id = row["id"]?.ToString(),
                Title = row["title"]?.ToString(),
                ImageUrl = row["image_url"]?.ToString(),
                LinkUrl = row["link_url"]?.ToString(),
                Position = row["position"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"])
            });
        }

        return result;
    }

    public async Task<PublicFooterInfo> GetFooterAsync(string maTruongBo)
    {
        string cacheKey = $"Footer_{maTruongBo}";
        if (_cache.TryGetValue(cacheKey, out PublicFooterInfo? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var sql = $@"
            SELECT tentruong, thongtin
            FROM l_truong
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0)
        {
            var fallbackFooter = new PublicFooterInfo();
            var fallbackSettings = await _siteSettingService.GetSettingsAsync(maTruongBo);
            ApplySiteSettings(fallbackFooter, fallbackSettings);
            _cache.Set(cacheKey, fallbackFooter, TimeSpan.FromMinutes(2));
            return fallbackFooter;
        }

        var row = dt.Rows[0];
        var thongTinJson = row["thongtin"]?.ToString();
        var footer = ParseFooterInfo(thongTinJson);
        footer.SchoolName = row["tentruong"]?.ToString();

        var settings = await _siteSettingService.GetSettingsAsync(maTruongBo);
        ApplySiteSettings(footer, settings);

        _cache.Set(cacheKey, footer, TimeSpan.FromMinutes(2));
        return footer;
    }

    public async Task<Dictionary<string, string>> GetSiteSettingsAsync(string maTruongBo)
    {
        return await _siteSettingService.GetSettingsAsync(maTruongBo);
    }

    public string ResolveMenuUrl(PublicNavItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Url))
            return item.Url!;

        if (!string.IsNullOrWhiteSpace(item.MenuName) && item.MenuName.Contains("giới thiệu", StringComparison.OrdinalIgnoreCase))
            return "/about";

        return item.PageType?.ToLowerInvariant() switch
        {
            "static" when !string.IsNullOrWhiteSpace(item.PageSlug) => $"/pages/{item.PageSlug}",
            "post" when !string.IsNullOrWhiteSpace(item.PageSlug) => $"/posts/{item.PageSlug}",
            "document" when !string.IsNullOrWhiteSpace(item.PageSlug) => $"/documents/{item.PageSlug}",
            _ => "/"
        };
    }

    private static PublicFooterInfo ParseFooterInfo(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new PublicFooterInfo();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new PublicFooterInfo
            {
                Address = GetString(root, "diachi_truong"),
                LogoUrl = GetString(root, "logo_truong"),
                Phone = GetString(root, "so_dien_thoai"),
                LeaderName = GetString(root, "ho_ten"),
                WebsiteUrl = GetString(root, "website_url"),
                FacebookUrl = GetString(root, "facebook_url"),
                YoutubeUrl = GetString(root, "youtube_url"),
                ZaloUrl = GetString(root, "zalo_url")
            };
        }
        catch
        {
            return new PublicFooterInfo();
        }
    }

    private static void ApplySiteSettings(PublicFooterInfo footer, IReadOnlyDictionary<string, string> settings)
    {
        if (settings.Count == 0)
            return;

        ApplyFooterJson(footer, PublicSiteSettingReader.First(settings, "footer_json_info"));

        footer.SchoolName = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_name", "site_name"), footer.SchoolName);
        footer.SiteSlogan = FirstNonBlank(PublicSiteSettingReader.First(settings, "site_slogan"), footer.SiteSlogan);
        footer.LogoUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_logo"), footer.LogoUrl);
        footer.Address = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_address"), footer.Address);
        footer.Phone = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_phone", "contact_phone", "contact_hotline"), footer.Phone);
        footer.Email = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_email", "contact_email"), footer.Email);
        footer.WebsiteUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_website"), footer.WebsiteUrl);
        footer.FacebookUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "contact_facebook"), footer.FacebookUrl);
        footer.YoutubeUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "contact_youtube"), footer.YoutubeUrl);
        footer.ZaloUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "contact_zalo"), footer.ZaloUrl);
        footer.FooterText = FirstNonBlank(PublicSiteSettingReader.First(settings, "footer_text"), footer.FooterText);
        footer.ShowLogo = PublicSiteSettingReader.Bool(settings, footer.ShowLogo, "footer_show_logo");
        footer.ShowContact = PublicSiteSettingReader.Bool(settings, footer.ShowContact, "footer_show_contact");
        footer.ShowSocial = PublicSiteSettingReader.Bool(settings, footer.ShowSocial, "footer_show_social");
        footer.FeatureSearchEnabled = PublicSiteSettingReader.Bool(settings, footer.FeatureSearchEnabled, "feature_search_enabled");
    }

    private static void ApplyFooterJson(PublicFooterInfo footer, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            footer.SchoolName = FirstNonBlank(GetString(root, "school_name"), GetString(root, "ten_truong"), footer.SchoolName);
            footer.SiteSlogan = FirstNonBlank(GetString(root, "site_slogan"), GetString(root, "slogan"), footer.SiteSlogan);
            footer.Address = FirstNonBlank(GetString(root, "school_address"), GetString(root, "diachi_truong"), GetString(root, "address"), footer.Address);
            footer.LogoUrl = FirstNonBlank(GetString(root, "school_logo"), GetString(root, "logo_truong"), GetString(root, "logo_url"), footer.LogoUrl);
            footer.Phone = FirstNonBlank(GetString(root, "school_phone"), GetString(root, "contact_phone"), GetString(root, "so_dien_thoai"), footer.Phone);
            footer.Email = FirstNonBlank(GetString(root, "school_email"), GetString(root, "contact_email"), GetString(root, "email"), footer.Email);
            footer.LeaderName = FirstNonBlank(GetString(root, "leader_name"), GetString(root, "ho_ten"), footer.LeaderName);
            footer.WebsiteUrl = FirstNonBlank(GetString(root, "school_website"), GetString(root, "website_url"), footer.WebsiteUrl);
            footer.FacebookUrl = FirstNonBlank(GetString(root, "contact_facebook"), GetString(root, "facebook_url"), footer.FacebookUrl);
            footer.YoutubeUrl = FirstNonBlank(GetString(root, "contact_youtube"), GetString(root, "youtube_url"), footer.YoutubeUrl);
            footer.ZaloUrl = FirstNonBlank(GetString(root, "contact_zalo"), GetString(root, "zalo_url"), footer.ZaloUrl);
            footer.FooterText = FirstNonBlank(GetString(root, "footer_text"), GetString(root, "description"), footer.FooterText);
        }
        catch
        {
            // Invalid JSON should not break the public site; individual setting keys still apply.
        }
    }

    private static string? GetString(System.Text.Json.JsonElement root, string key)
    {
        return root.TryGetProperty(key, out var value) ? value.ToString() : null;
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicNavItem
{
    public string? Id { get; set; }
    public string? MenuName { get; set; }
    public string? Url { get; set; }
    public string? Target { get; set; }
    public string? ParentId { get; set; }
    public int SortOrder { get; set; }
    public string? PageSlug { get; set; }
    public string? PageType { get; set; }
}

public class PublicFooterInfo
{
    public string? SchoolName { get; set; }
    public string? SiteSlogan { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LeaderName { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? ZaloUrl { get; set; }
    public string? FooterText { get; set; }
    public bool ShowLogo { get; set; } = true;
    public bool ShowContact { get; set; } = true;
    public bool ShowSocial { get; set; } = true;
    public bool FeatureSearchEnabled { get; set; } = true;
}

public class PublicBannerItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? Position { get; set; }
    public int SortOrder { get; set; }
}
