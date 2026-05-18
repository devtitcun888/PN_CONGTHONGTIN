using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminPostTagService
{
    Task<List<AdminPostTagItem>> GetTagsAsync(string maTruongBo);
    Task<List<AdminPostTagItem>> GetActiveTagsAsync(string maTruongBo);
    Task<AdminPostTagDetail?> GetTagByIdAsync(string id);
    Task<AdminPostTagDetail?> GetTagBySlugAsync(string maTruongBo, string slug);
    Task<bool> CreateTagAsync(AdminPostTagDetail model);
    Task<bool> EnsureTagAsync(AdminPostTagDetail model);
    Task<bool> UpdateTagAsync(AdminPostTagDetail model);
    Task<bool> DeleteTagAsync(string id);
    Task<bool> SetActiveAsync(string id, bool isActive);
}

public class AdminPostTagService : IAdminPostTagService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminPostTagService> _logger;

    public AdminPostTagService(ILogger<AdminPostTagService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminPostTagItem>> GetTagsAsync(string maTruongBo)
    {
        var result = new List<AdminPostTagItem>();
        var sql = $@"
            SELECT id, tag_name, slug, is_active, created_at
            FROM post_tags
            WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE
            ORDER BY created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapItem(row));
        }
        return result;
    }

    public async Task<List<AdminPostTagItem>> GetActiveTagsAsync(string maTruongBo)
    {
        var result = new List<AdminPostTagItem>();
        var sql = $@"
            SELECT id, tag_name, slug, is_active, created_at
            FROM post_tags
            WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE AND is_active = TRUE
            ORDER BY created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapItem(row));
        }
        return result;
    }

    public async Task<AdminPostTagDetail?> GetTagByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, tag_name, slug, is_active
            FROM post_tags
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return MapDetail(row);
    }

    public async Task<AdminPostTagDetail?> GetTagBySlugAsync(string maTruongBo, string slug)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, tag_name, slug, is_active
            FROM post_tags
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND slug = '{Escape(slug)}'
              AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        return MapDetail(dt.Rows[0]);
    }

    public async Task<bool> CreateTagAsync(AdminPostTagDetail model)
    {
        var sql = $@"
            INSERT INTO post_tags
            (ma_truong_bo, tag_name, slug, is_active, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.TagName)}', '{Escape(model.Slug)}', {(model.IsActive ? "TRUE" : "FALSE")}, NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreateTagAsync");
    }

    public async Task<bool> EnsureTagAsync(AdminPostTagDetail model)
    {
        var existing = await GetTagBySlugAsync(model.MaTruongBo ?? string.Empty, model.Slug ?? string.Empty);
        if (existing != null)
        {
            model.Id = existing.Id;
            return true;
        }

        return await CreateTagAsync(model);
    }

    public async Task<bool> UpdateTagAsync(AdminPostTagDetail model)
    {
        var sql = $@"
            UPDATE post_tags
               SET tag_name = '{Escape(model.TagName)}',
                   slug = '{Escape(model.Slug)}',
                   is_active = {(model.IsActive ? "TRUE" : "FALSE")},
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateTagAsync");
    }

    public async Task<bool> DeleteTagAsync(string id)
    {
        var sql = $@"
            UPDATE post_tags
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteTagAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE post_tags
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

    private static AdminPostTagItem MapItem(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        TagName = row["tag_name"]?.ToString(),
        Slug = row["slug"]?.ToString(),
        IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
    };

    private static AdminPostTagDetail MapDetail(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        MaTruongBo = row["ma_truong_bo"]?.ToString(),
        TagName = row["tag_name"]?.ToString(),
        Slug = row["slug"]?.ToString(),
        IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
    };

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminPostTagItem
{
    public string? Id { get; set; }
    public string? TagName { get; set; }
    public string? Slug { get; set; }
    public bool IsActive { get; set; }
}

public class AdminPostTagDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? TagName { get; set; }
    public string? Slug { get; set; }
    public bool IsActive { get; set; } = true;
}
