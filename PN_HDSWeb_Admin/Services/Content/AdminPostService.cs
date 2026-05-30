using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminPostService
{
    Task<List<AdminPostItem>> GetPostsAsync(string maTruongBo, string? keyword = null, string? status = null, int page = 1, int pageSize = 20);
    Task<int> GetPostsCountAsync(string maTruongBo, string? keyword = null, string? status = null);
    Task<long> GetPostsViewCountTotalAsync(string maTruongBo, string? keyword = null, string? status = null);
    Task<AdminPostDetail?> GetPostByIdAsync(string id);
    Task<bool> CreatePostAsync(AdminPostDetail model);
    Task<bool> UpdatePostAsync(AdminPostDetail model);
    Task<bool> DeletePostAsync(string id);
}

public class AdminPostService : IAdminPostService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminPostService> _logger;

    public AdminPostService(ILogger<AdminPostService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminPostItem>> GetPostsAsync(string maTruongBo, string? keyword = null, string? status = null, int page = 1, int pageSize = 20)
    {
        var result = new List<AdminPostItem>();
        var offset = Math.Max(page - 1, 0) * pageSize;
        var where = BuildWhere(maTruongBo, keyword, status);

        var sql = $@"
            SELECT p.id, p.title, p.slug, p.post_type, p.is_featured, p.status, p.publish_at, p.created_at, p.view_count,
                   p.category_id, c.category_name
            FROM posts p
            LEFT JOIN post_categories c ON c.id = p.category_id AND c.is_deleted = FALSE
            {where}
            ORDER BY p.created_at DESC
            LIMIT {pageSize} OFFSET {offset}";

        try
        {
            DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new AdminPostItem
                {
                    Id = row["id"]?.ToString(),
                    Title = row["title"]?.ToString(),
                    Slug = row["slug"]?.ToString(),
                    CategoryId = row["category_id"]?.ToString(),
                    CategoryName = row["category_name"]?.ToString(),
                    PostType = row["post_type"]?.ToString(),
                    Status = row["status"]?.ToString(),
                    IsFeatured = row["is_featured"] != DBNull.Value && Convert.ToBoolean(row["is_featured"]),
                    PublishAt = row["publish_at"] == DBNull.Value ? null : Convert.ToDateTime(row["publish_at"]),
                    ViewCount = row["view_count"] == DBNull.Value ? 0 : Convert.ToInt64(row["view_count"]),
                    CreatedAt = row["created_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["created_at"])
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPostsAsync failed");
            throw;
        }

        return result;
    }

    public async Task<int> GetPostsCountAsync(string maTruongBo, string? keyword = null, string? status = null)
    {
        var where = BuildWhere(maTruongBo, keyword, status);
        var sql = $"SELECT COUNT(*) AS total FROM posts p {where}";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return 0;
        return dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
    }

    public async Task<long> GetPostsViewCountTotalAsync(string maTruongBo, string? keyword = null, string? status = null)
    {
        var where = BuildWhere(maTruongBo, keyword, status);
        var sql = $"SELECT COALESCE(SUM(COALESCE(p.view_count, 0)), 0) AS total_views FROM posts p {where}";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return 0;
        return dt.Rows[0]["total_views"] == DBNull.Value ? 0 : Convert.ToInt64(dt.Rows[0]["total_views"]);
    }

    public async Task<AdminPostDetail?> GetPostByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, category_id, title, slug, summary, content, cover_image_url,
                   post_type, status, is_featured, sort_order, publish_at, expire_at, view_count,
                   approved_by, approved_at, rejected_by, rejected_at, reject_reason
            FROM posts
            WHERE is_deleted = FALSE AND id = '{Escape(id)}'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 ? null : MapDetail(dt.Rows[0]);
    }

    public async Task<bool> CreatePostAsync(AdminPostDetail model)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = PN_HDSWeb_Admin.Services.Admin.AdminMenuService.ToSlug(model.Title);
        }

        var sql = $@"
            INSERT INTO posts
            (ma_truong_bo, category_id, title, slug, summary, content, cover_image_url, post_type,
             status, is_featured, sort_order, publish_at, expire_at, view_count, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.CategoryId)}', '{Escape(model.Title)}', '{Escape(model.Slug)}',
             '{Escape(model.Summary)}', '{Escape(model.Content)}', '{Escape(model.CoverImageUrl)}', '{Escape(model.PostType)}',
             '{Escape(model.Status)}', {(model.IsFeatured ? "TRUE" : "FALSE")}, {model.SortOrder},
             {(model.PublishAt.HasValue ? $"'{model.PublishAt:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
             {(model.ExpireAt.HasValue ? $"'{model.ExpireAt:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
             {model.ViewCount}, NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreatePostAsync");
    }

    public async Task<bool> UpdatePostAsync(AdminPostDetail model)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = PN_HDSWeb_Admin.Services.Admin.AdminMenuService.ToSlug(model.Title);
        }

        var sql = $@"
            UPDATE posts
               SET category_id = '{Escape(model.CategoryId)}',
                   title = '{Escape(model.Title)}',
                   slug = '{Escape(model.Slug)}',
                   summary = '{Escape(model.Summary)}',
                   content = '{Escape(model.Content)}',
                   cover_image_url = '{Escape(model.CoverImageUrl)}',
                   post_type = '{Escape(model.PostType)}',
                   status = '{Escape(model.Status)}',
                   is_featured = {(model.IsFeatured ? "TRUE" : "FALSE")},
                   sort_order = {model.SortOrder},
                   publish_at = {(model.PublishAt.HasValue ? $"'{model.PublishAt:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                   expire_at = {(model.ExpireAt.HasValue ? $"'{model.ExpireAt:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                   view_count = {model.ViewCount},
                   approved_by = '{Escape(model.ApprovedBy)}',
                   approved_at = {(model.ApprovedAt.HasValue ? $"'{model.ApprovedAt:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                   rejected_by = '{Escape(model.RejectedBy)}',
                   rejected_at = {(model.RejectedAt.HasValue ? $"'{model.RejectedAt:yyyy-MM-dd HH:mm:ss}'" : "NULL")},
                   reject_reason = '{Escape(model.RejectReason)}',
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdatePostAsync");
    }

    public async Task<bool> DeletePostAsync(string id)
    {
        var sql = $@"
            UPDATE posts
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeletePostAsync");
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

    private static string BuildWhere(string maTruongBo, string? keyword, string? status)
    {
        var clauses = new List<string>
        {
            "p.is_deleted = FALSE",
            $"p.ma_truong_bo = '{Escape(maTruongBo)}'"
        };
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Escape(keyword);
            clauses.Add($"(p.title ILIKE '%{k}%' OR p.slug ILIKE '%{k}%' OR p.summary ILIKE '%{k}%')");
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            clauses.Add($"p.status = '{Escape(status)}'");
        }
        return "WHERE " + string.Join(" AND ", clauses);
    }

    private static AdminPostDetail MapDetail(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        MaTruongBo = row["ma_truong_bo"]?.ToString(),
        CategoryId = row["category_id"]?.ToString(),
        Title = row["title"]?.ToString(),
        Slug = row["slug"]?.ToString(),
        Summary = row["summary"]?.ToString(),
        Content = row["content"]?.ToString(),
        CoverImageUrl = row["cover_image_url"]?.ToString(),
        PostType = row["post_type"]?.ToString(),
        Status = row["status"]?.ToString(),
        IsFeatured = row["is_featured"] != DBNull.Value && Convert.ToBoolean(row["is_featured"]),
        SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
        PublishAt = row["publish_at"] == DBNull.Value ? null : Convert.ToDateTime(row["publish_at"]),
        ExpireAt = row["expire_at"] == DBNull.Value ? null : Convert.ToDateTime(row["expire_at"]),
        ViewCount = row["view_count"] == DBNull.Value ? 0 : Convert.ToInt64(row["view_count"]),
        ApprovedBy = row["approved_by"]?.ToString(),
        ApprovedAt = row["approved_at"] == DBNull.Value ? null : Convert.ToDateTime(row["approved_at"]),
        RejectedBy = row["rejected_by"]?.ToString(),
        RejectedAt = row["rejected_at"] == DBNull.Value ? null : Convert.ToDateTime(row["rejected_at"]),
        RejectReason = row["reject_reason"]?.ToString()
    };

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminPostItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? PostType { get; set; }
    public string? Status { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? PublishAt { get; set; }
    public long ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPostDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? CategoryId { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? PostType { get; set; }
    public string? Status { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public DateTime? PublishAt { get; set; }
    public DateTime? ExpireAt { get; set; }
    public long ViewCount { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectReason { get; set; }
}
