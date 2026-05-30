using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminStaticPageService
{
    Task<List<AdminStaticPageItem>> GetPagesAsync(string maTruongBo);
    Task<AdminStaticPageDetail?> GetPageByIdAsync(string id);
    Task<bool> CreatePageAsync(AdminStaticPageDetail model);
    Task<bool> UpdatePageAsync(AdminStaticPageDetail model);
    Task<bool> DeletePageAsync(string id);
    Task<bool> SetActiveAsync(string id, bool isActive);
}

public class AdminStaticPageService : IAdminStaticPageService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminStaticPageService> _logger;

    public AdminStaticPageService(ILogger<AdminStaticPageService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminStaticPageItem>> GetPagesAsync(string maTruongBo)
    {
        var result = new List<AdminStaticPageItem>();
        var sql = $@"
            SELECT id, page_code, title, slug, status, sort_order, created_at, updated_at
            FROM static_pages
            WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new AdminStaticPageItem
            {
                Id = row["id"]?.ToString(),
                PageCode = row["page_code"]?.ToString(),
                Title = row["title"]?.ToString(),
                Slug = row["slug"]?.ToString(),
                Status = row["status"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
                CreatedAt = row["created_at"] == DBNull.Value ? null : Convert.ToDateTime(row["created_at"]),
                UpdatedAt = row["updated_at"] == DBNull.Value ? null : Convert.ToDateTime(row["updated_at"])
            });
        }

        return result;
    }

    public async Task<AdminStaticPageDetail?> GetPageByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, page_code, title, slug, content, status, sort_order, meta_title, meta_description
            FROM static_pages
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new AdminStaticPageDetail
        {
            Id = row["id"]?.ToString(),
            MaTruongBo = row["ma_truong_bo"]?.ToString(),
            PageCode = row["page_code"]?.ToString(),
            Title = row["title"]?.ToString(),
            Slug = row["slug"]?.ToString(),
            Content = row["content"]?.ToString(),
            Status = row["status"]?.ToString(),
            SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
            MetaTitle = row["meta_title"]?.ToString(),
            MetaDescription = row["meta_description"]?.ToString()
        };
    }

    public async Task<bool> CreatePageAsync(AdminStaticPageDetail model)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = AdminMenuService.ToSlug(model.Title);
        }
        if (string.IsNullOrWhiteSpace(model.PageCode))
        {
            model.PageCode = AdminMenuService.GeneratePageCode(model.Title);
        }

        var sql = $@"
            INSERT INTO static_pages
            (ma_truong_bo, page_code, title, slug, content, status, sort_order, meta_title, meta_description, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.PageCode)}', '{Escape(model.Title)}', '{Escape(model.Slug)}',
             '{Escape(model.Content)}', '{Escape(model.Status)}', {model.SortOrder}, '{Escape(model.MetaTitle)}', '{Escape(model.MetaDescription)}', NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreatePageAsync");
    }

    public async Task<bool> UpdatePageAsync(AdminStaticPageDetail model)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            model.Slug = AdminMenuService.ToSlug(model.Title);
        }
        if (string.IsNullOrWhiteSpace(model.PageCode))
        {
            model.PageCode = AdminMenuService.GeneratePageCode(model.Title);
        }

        var sql = $@"
            UPDATE static_pages
               SET page_code = '{Escape(model.PageCode)}',
                   title = '{Escape(model.Title)}',
                   slug = '{Escape(model.Slug)}',
                   content = '{Escape(model.Content)}',
                   status = '{Escape(model.Status)}',
                   sort_order = {model.SortOrder},
                   meta_title = '{Escape(model.MetaTitle)}',
                   meta_description = '{Escape(model.MetaDescription)}',
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdatePageAsync");
    }

    public async Task<bool> DeletePageAsync(string id)
    {
        var sql = $@"
            UPDATE static_pages
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeletePageAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE static_pages
               SET status = {(isActive ? "'Published'" : "'Draft'")},
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

public class AdminStaticPageItem
{
    public string? Id { get; set; }
    public string? PageCode { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Status { get; set; }
    public int SortOrder { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdminStaticPageDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? PageCode { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public string? Status { get; set; }
    public int SortOrder { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}
