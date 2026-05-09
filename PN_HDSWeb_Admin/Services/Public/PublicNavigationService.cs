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
            SELECT tentruong, diachi_truong, logo_truong, hieutruong->>'so_dien_thoai' AS phone, hieutruong->>'ho_ten' AS leader,
                   website_url, facebook_url, youtube_url, zalo_url
            FROM l_truong
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0)
        {
            return new PublicFooterInfo();
        }

        var row = dt.Rows[0];
        return new PublicFooterInfo
        {
            SchoolName = row["tentruong"]?.ToString(),
            Address = row["diachi_truong"]?.ToString(),
            LogoUrl = row["logo_truong"]?.ToString(),
            Phone = row["phone"]?.ToString(),
            LeaderName = row["leader"]?.ToString(),
            WebsiteUrl = row["website_url"]?.ToString(),
            FacebookUrl = row["facebook_url"]?.ToString(),
            YoutubeUrl = row["youtube_url"]?.ToString(),
            ZaloUrl = row["zalo_url"]?.ToString()
        };
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
