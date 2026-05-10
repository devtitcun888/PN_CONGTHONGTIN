using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicHomepageService
{
    Task<PublicHomepageViewModel> GetHomepageAsync(string maTruongBo);
}

public class PublicHomepageService : IPublicHomepageService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicHomepageService> _logger;

    public PublicHomepageService(ILogger<PublicHomepageService> logger)
    {
        _logger = logger;
    }

    public async Task<PublicHomepageViewModel> GetHomepageAsync(string maTruongBo)
    {
        var model = new PublicHomepageViewModel();
        try
        {
            model.Banners = await GetBannersAsync(maTruongBo);
            model.FeaturedPosts = await GetFeaturedPostsAsync(maTruongBo);
            model.LatestPosts = await GetLatestPostsAsync(maTruongBo);
            model.PublishedDocuments = await GetPublishedDocumentsAsync(maTruongBo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHomepageAsync failed");
            throw;
        }

        return model;
    }

    private async Task<List<PublicBannerItem>> GetBannersAsync(string maTruongBo)
    {
        var list = new List<PublicBannerItem>();
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
            list.Add(new PublicBannerItem
            {
                Id = row["id"]?.ToString(),
                Title = row["title"]?.ToString(),
                ImageUrl = row["image_url"]?.ToString(),
                LinkUrl = row["link_url"]?.ToString(),
                Position = row["position"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"])
            });
        }
        return list;
    }

    private async Task<List<PublicPostItem>> GetFeaturedPostsAsync(string maTruongBo)
    {
        var list = new List<PublicPostItem>();
        var sql = $@"
            SELECT id, title, slug, summary, cover_image_url, publish_at
            FROM posts
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
              AND is_featured = TRUE
            ORDER BY publish_at DESC, created_at DESC
            LIMIT 6";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(MapPost(row));
        }
        return list;
    }

    private async Task<List<PublicPostItem>> GetLatestPostsAsync(string maTruongBo)
    {
        var list = new List<PublicPostItem>();
        var sql = $@"
            SELECT id, title, slug, summary, cover_image_url, publish_at
            FROM posts
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
            ORDER BY publish_at DESC, created_at DESC
            LIMIT 10";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(MapPost(row));
        }
        return list;
    }

    private async Task<List<PublicDocumentItem>>    GetPublishedDocumentsAsync(string maTruongBo)
    {
        var list = new List<PublicDocumentItem>();
        var sql = $@"
            SELECT id, doc_title, doc_number, file_url, issued_date
            FROM documents
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
            ORDER BY issued_date DESC, created_at DESC
            LIMIT 10";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(new PublicDocumentItem
            {
                Id = row["id"]?.ToString(),
                DocTitle = row["doc_title"]?.ToString(),
                DocNumber = row["doc_number"]?.ToString(),
                FileUrl = row["file_url"]?.ToString(),
                IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"])
            });
        }
        return list;
    }

    private static PublicPostItem MapPost(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        Title = row["title"]?.ToString(),
        Slug = row["slug"]?.ToString(),
        Summary = row["summary"]?.ToString(),
        CoverImageUrl = row["cover_image_url"]?.ToString(),
        PublishAt = row["publish_at"] == DBNull.Value ? null : Convert.ToDateTime(row["publish_at"])
    };

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicHomepageViewModel
{
    public List<PublicBannerItem> Banners { get; set; } = [];
    public List<PublicPostItem> FeaturedPosts { get; set; } = [];
    public List<PublicPostItem> LatestPosts { get; set; } = [];
    public List<PublicDocumentItem> PublishedDocuments { get; set; } = [];

    public string? SiteName { get; set; }
    public string? SiteTitle { get; set; }
    public string? SiteSlogan { get; set; }
    public string? SiteLogo { get; set; }
    public string? ContactHotline { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactAddress { get; set; }
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

public class PublicPostItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime? PublishAt { get; set; }
}

public class PublicDocumentItem
{
    public string? Id { get; set; }
    public string? DocTitle { get; set; }
    public string? DocNumber { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? IssuedDate { get; set; }
}
