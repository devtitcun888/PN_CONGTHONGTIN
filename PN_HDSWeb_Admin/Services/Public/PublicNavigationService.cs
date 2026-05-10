using hDataLibraryN8;
using Microsoft.Extensions.Logging;
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

    public PublicNavigationService(ILogger<PublicNavigationService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicNavItem>> GetMenusAsync(string maTruongBo)
    {
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
        var sql = $@"
            SELECT tentruong, thongtin
            FROM l_truong
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0)
        {
            return new PublicFooterInfo();
        }

        var row = dt.Rows[0];
        var thongTinJson = row["thongtin"]?.ToString();
        var footer = ParseFooterInfo(thongTinJson);
        footer.SchoolName = row["tentruong"]?.ToString();
        return footer;
    }

    public async Task<Dictionary<string, string>> GetSiteSettingsAsync(string maTruongBo)
    {
        var sql = $@"
            SELECT setting_key, setting_value
            FROM site_settings
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_active = TRUE
            ORDER BY setting_group, setting_key";

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            var key = row["setting_key"]?.ToString();
            var value = row["setting_value"]?.ToString();
            if (!string.IsNullOrWhiteSpace(key))
                result[key] = value ?? string.Empty;
        }

        return result;
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

    private static string? GetString(System.Text.Json.JsonElement root, string key)
    {
        return root.TryGetProperty(key, out var value) ? value.ToString() : null;
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
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? LeaderName { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? ZaloUrl { get; set; }
}
