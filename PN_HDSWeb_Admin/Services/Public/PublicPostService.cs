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
    Task<List<PublicPostListItem>> GetMostViewedPostsAsync(string maTruongBo, int take = 8, string? excludePostId = null);
    Task<List<PublicPostListItem>> GetPostsByTagIdAsync(string maTruongBo, string tagId, int page = 1, int pageSize = 30);
    Task<bool> IncrementPostViewAsync(string maTruongBo, string postId);
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
            SELECT p.id, p.title, p.slug, p.summary, p.cover_image_url, p.publish_at, p.category_id, p.view_count,
                   c.category_name, c.slug AS category_slug
            FROM posts p
            LEFT JOIN post_categories c ON c.id = p.category_id AND c.is_deleted = FALSE
            {where}
            ORDER BY p.publish_at DESC, p.created_at DESC
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
        var sql = $"SELECT COUNT(*) AS total FROM posts p {BuildWhere(maTruongBo, keyword, categoryId)}";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 || dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
    }

    public async Task<PublicPostDetail?> GetPostBySlugAsync(string maTruongBo, string slug)
    {
        var sql = $@"
            SELECT p.id, p.title, p.slug, p.summary, p.content, p.cover_image_url, p.publish_at, p.category_id, p.view_count,
                   c.category_name, c.slug AS category_slug
            FROM posts p
            LEFT JOIN post_categories c ON c.id = p.category_id AND c.is_deleted = FALSE
            WHERE p.ma_truong_bo = '{Escape(maTruongBo)}'
              AND p.slug = '{Escape(slug)}'
              AND p.is_deleted = FALSE
              AND p.status = 'Published'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        return MapDetail(dt.Rows[0]);
    }

    public async Task<List<PublicPostListItem>> GetRelatedPostsAsync(string maTruongBo, string? categoryId, string currentPostId, int take = 4)
    {
        var categoryFilter = string.IsNullOrWhiteSpace(categoryId) ? string.Empty : $"AND p.category_id = '{Escape(categoryId)}'";
        var sql = $@"
            SELECT p.id, p.title, p.slug, p.summary, p.cover_image_url, p.publish_at, p.category_id, p.view_count,
                   c.category_name, c.slug AS category_slug
            FROM posts p
            LEFT JOIN post_categories c ON c.id = p.category_id AND c.is_deleted = FALSE
            WHERE p.ma_truong_bo = '{Escape(maTruongBo)}'
              AND p.is_deleted = FALSE
              AND p.status = 'Published'
              AND p.id <> '{Escape(currentPostId)}'
              {categoryFilter}
            ORDER BY p.publish_at DESC, p.created_at DESC
            LIMIT {take}";

        var result = new List<PublicPostListItem>();
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapListItem(row));
        }
        return result;
    }

    public async Task<List<PublicPostListItem>> GetMostViewedPostsAsync(string maTruongBo, int take = 8, string? excludePostId = null)
    {
        var result = new List<PublicPostListItem>();
        var safeTake = Math.Clamp(take, 1, 20);
        var excludeFilter = string.IsNullOrWhiteSpace(excludePostId) ? string.Empty : $"AND p.id <> '{Escape(excludePostId)}'";
        var sql = $@"
            SELECT p.id, p.title, p.slug, p.summary, p.cover_image_url, p.publish_at, p.category_id, p.view_count,
                   c.category_name, c.slug AS category_slug
            FROM posts p
            LEFT JOIN post_categories c ON c.id = p.category_id AND c.is_deleted = FALSE
            WHERE p.ma_truong_bo = '{Escape(maTruongBo)}'
              AND p.is_deleted = FALSE
              AND p.status = 'Published'
              {excludeFilter}
            ORDER BY COALESCE(p.view_count, 0) DESC, p.publish_at DESC, p.created_at DESC
            LIMIT {safeTake}";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapListItem(row));
        }
        return result;
    }

    public async Task<List<PublicPostListItem>> GetPostsByTagIdAsync(string maTruongBo, string tagId, int page = 1, int pageSize = 30)
    {
        var offset = Math.Max(page - 1, 0) * pageSize;
        var sql = $@"
            SELECT p.id, p.title, p.slug, p.summary, p.cover_image_url, p.publish_at, p.category_id, p.view_count,
                   c.category_name, c.slug AS category_slug
            FROM posts p
            INNER JOIN post_tag_map m ON m.post_id = p.id
            LEFT JOIN post_categories c ON c.id = p.category_id AND c.is_deleted = FALSE
            WHERE p.ma_truong_bo = '{Escape(maTruongBo)}'
              AND p.is_deleted = FALSE
              AND p.status = 'Published'
              AND m.tag_id = '{Escape(tagId)}'
            ORDER BY p.publish_at DESC, p.created_at DESC
            LIMIT {pageSize} OFFSET {offset}";

        var result = new List<PublicPostListItem>();
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapListItem(row));
        }
        return result;
    }

    public async Task<bool> IncrementPostViewAsync(string maTruongBo, string postId)
    {
        if (string.IsNullOrWhiteSpace(maTruongBo) || string.IsNullOrWhiteSpace(postId))
            return false;

        var sql = $@"
            UPDATE posts
               SET view_count = COALESCE(view_count, 0) + 1,
                   updated_at = NOW()
             WHERE ma_truong_bo = '{Escape(maTruongBo)}'
               AND id = '{Escape(postId)}'
               AND is_deleted = FALSE
               AND status = 'Published'";

        try
        {
            await hdataLib.hrunQueryAsync(LoginID_Index, sql);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IncrementPostViewAsync failed for post {PostId}", postId);
            return false;
        }
    }

    private static string BuildWhere(string maTruongBo, string? keyword, string? categoryId)
    {
        var clauses = new List<string>
        {
            "p.is_deleted = FALSE",
            "p.status = 'Published'",
            $"p.ma_truong_bo = '{Escape(maTruongBo)}'"
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Escape(keyword);
            clauses.Add($"(p.title ILIKE '%{k}%' OR p.summary ILIKE '%{k}%')");
        }

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            clauses.Add($"p.category_id = '{Escape(categoryId)}'");
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
        PublishAt = row["publish_at"] == DBNull.Value ? null : Convert.ToDateTime(row["publish_at"]),
        ViewCount = row.Table.Columns.Contains("view_count") && row["view_count"] != DBNull.Value ? Convert.ToInt64(row["view_count"]) : 0,
        CategoryId = row.Table.Columns.Contains("category_id") ? row["category_id"]?.ToString() : null,
        CategoryName = row.Table.Columns.Contains("category_name") ? row["category_name"]?.ToString() : null,
        CategorySlug = row.Table.Columns.Contains("category_slug") ? row["category_slug"]?.ToString() : null
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
        ViewCount = row.Table.Columns.Contains("view_count") && row["view_count"] != DBNull.Value ? Convert.ToInt64(row["view_count"]) : 0,
        CategoryId = row["category_id"]?.ToString(),
        CategoryName = row.Table.Columns.Contains("category_name") ? row["category_name"]?.ToString() : null,
        CategorySlug = row.Table.Columns.Contains("category_slug") ? row["category_slug"]?.ToString() : null
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
    public long ViewCount { get; set; }
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public List<PublicPostTagItem> Tags { get; set; } = [];
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
    public long ViewCount { get; set; }
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
}
