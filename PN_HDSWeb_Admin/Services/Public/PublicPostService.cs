using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicPostService
{
    Task<List<PublicPostListItem>> GetPostsAsync(string maTruongBo, string? keyword = null, string? categoryId = null, int page = 1, int pageSize = 10);
    Task<int> GetPostsCountAsync(string maTruongBo, string? keyword = null, string? categoryId = null);
    Task<PublicPostDetail?> GetPostBySlugAsync(string maTruongBo, string slug);
    Task<List<PublicPostListItem>> GetRelatedPostsAsync(string maTruongBo, string? categoryId, string currentPostId, int take = 4);
}

public class PublicPostService : IPublicPostService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicPostService> _logger;

    public PublicPostService(ILogger<PublicPostService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicPostListItem>> GetPostsAsync(string maTruongBo, string? keyword = null, string? categoryId = null, int page = 1, int pageSize = 10)
    {
        var result = new List<PublicPostListItem>();
        var offset = Math.Max(page - 1, 0) * pageSize;
        var where = BuildWhere(maTruongBo, keyword, categoryId);
        var sql = $@"
            SELECT id, title, slug, summary, cover_image_url, publish_at, category_id
            FROM posts
            {where}
            ORDER BY publish_at DESC, created_at DESC
            LIMIT {pageSize} OFFSET {offset}";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapListItem(row));
        }
        return result;
    }

    public async Task<int> GetPostsCountAsync(string maTruongBo, string? keyword = null, string? categoryId = null)
    {
        var sql = $"SELECT COUNT(*) AS total FROM posts {BuildWhere(maTruongBo, keyword, categoryId)}";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 || dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
    }

    public async Task<PublicPostDetail?> GetPostBySlugAsync(string maTruongBo, string slug)
    {
        var sql = $@"
            SELECT id, title, slug, summary, content, cover_image_url, publish_at, category_id
            FROM posts
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND slug = '{Escape(slug)}'
              AND is_deleted = FALSE
              AND status = 'Published'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        return MapDetail(dt.Rows[0]);
    }

    public async Task<List<PublicPostListItem>> GetRelatedPostsAsync(string maTruongBo, string? categoryId, string currentPostId, int take = 4)
    {
        var categoryFilter = string.IsNullOrWhiteSpace(categoryId) ? string.Empty : $"AND category_id = '{Escape(categoryId)}'";
        var sql = $@"
            SELECT id, title, slug, summary, cover_image_url, publish_at, category_id
            FROM posts
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
              AND id <> '{Escape(currentPostId)}'
              {categoryFilter}
            ORDER BY publish_at DESC, created_at DESC
            LIMIT {take}";

        var result = new List<PublicPostListItem>();
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapListItem(row));
        }
        return result;
    }

    private static string BuildWhere(string maTruongBo, string? keyword, string? categoryId)
    {
        var clauses = new List<string>
        {
            "is_deleted = FALSE",
            "status = 'Published'",
            $"ma_truong_bo = '{Escape(maTruongBo)}'"
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Escape(keyword);
            clauses.Add($"(title ILIKE '%{k}%' OR summary ILIKE '%{k}%')");
        }

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            clauses.Add($"category_id = '{Escape(categoryId)}'");
        }

        return "WHERE " + string.Join(" AND ", clauses);
    }

    private static PublicPostListItem MapListItem(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        Title = row["title"]?.ToString(),
        Slug = row["slug"]?.ToString(),
        Summary = row["summary"]?.ToString(),
        CoverImageUrl = row["cover_image_url"]?.ToString(),
        PublishAt = row["publish_at"] == DBNull.Value ? null : Convert.ToDateTime(row["publish_at"])
    };

    private static PublicPostDetail MapDetail(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        Title = row["title"]?.ToString(),
        Slug = row["slug"]?.ToString(),
        Summary = row["summary"]?.ToString(),
        Content = row["content"]?.ToString(),
        CoverImageUrl = row["cover_image_url"]?.ToString(),
        PublishAt = row["publish_at"] == DBNull.Value ? null : Convert.ToDateTime(row["publish_at"]),
        CategoryId = row["category_id"]?.ToString()
    };

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicPostListItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime? PublishAt { get; set; }
}

public class PublicPostDetail
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime? PublishAt { get; set; }
    public string? CategoryId { get; set; }
}
