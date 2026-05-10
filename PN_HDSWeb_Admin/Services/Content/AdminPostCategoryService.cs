using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminPostCategoryService
{
    Task<List<AdminPostCategoryItem>> GetCategoriesAsync(string maTruongBo);
    Task<List<AdminPostCategoryItem>> GetActiveCategoriesAsync(string maTruongBo);
    Task<AdminPostCategoryDetail?> GetCategoryByIdAsync(string id);
    Task<bool> CreateCategoryAsync(AdminPostCategoryDetail model);
    Task<bool> UpdateCategoryAsync(AdminPostCategoryDetail model);
    Task<bool> DeleteCategoryAsync(string id);
    Task<bool> SetActiveAsync(string id, bool isActive);
}

public class AdminPostCategoryService : IAdminPostCategoryService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminPostCategoryService> _logger;

    public AdminPostCategoryService(ILogger<AdminPostCategoryService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminPostCategoryItem>> GetCategoriesAsync(string maTruongBo)
    {
        var result = new List<AdminPostCategoryItem>();
        var sql = $@"
            SELECT id, category_code, category_name, slug, parent_id, description, sort_order, is_active
            FROM post_categories
            WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapItem(row));
        }
        return result;
    }

    public async Task<List<AdminPostCategoryItem>> GetActiveCategoriesAsync(string maTruongBo)
    {
        var result = new List<AdminPostCategoryItem>();
        var sql = $@"
            SELECT id, category_code, category_name, slug, parent_id, description, sort_order, is_active
            FROM post_categories
            WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE AND is_active = TRUE
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapItem(row));
        }
        return result;
    }

    public async Task<AdminPostCategoryDetail?> GetCategoryByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, category_code, category_name, slug, parent_id, description, sort_order, is_active
            FROM post_categories
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new AdminPostCategoryDetail
        {
            Id = row["id"]?.ToString(),
            MaTruongBo = row["ma_truong_bo"]?.ToString(),
            CategoryCode = row["category_code"]?.ToString(),
            CategoryName = row["category_name"]?.ToString(),
            Slug = row["slug"]?.ToString(),
            ParentId = row["parent_id"]?.ToString(),
            Description = row["description"]?.ToString(),
            SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
            IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<bool> CreateCategoryAsync(AdminPostCategoryDetail model)
    {
        var sql = $@"
            INSERT INTO post_categories
            (ma_truong_bo, category_code, category_name, slug, parent_id, description, sort_order, is_active, created_by, updated_by, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.CategoryCode)}', '{Escape(model.CategoryName)}', '{Escape(model.Slug)}',
             {ToNullableBigIntSql(model.ParentId)}, '{Escape(model.Description)}', {model.SortOrder}, {(model.IsActive ? "TRUE" : "FALSE")},
             '{Escape(model.CreatedBy)}', '{Escape(model.UpdatedBy)}', NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreateCategoryAsync");
    }

    public async Task<bool> UpdateCategoryAsync(AdminPostCategoryDetail model)
    {
        var sql = $@"
            UPDATE post_categories
               SET category_code = '{Escape(model.CategoryCode)}',
                   category_name = '{Escape(model.CategoryName)}',
                   slug = '{Escape(model.Slug)}',
                   parent_id = {ToNullableBigIntSql(model.ParentId)},
                   description = '{Escape(model.Description)}',
                   sort_order = {model.SortOrder},
                   is_active = {(model.IsActive ? "TRUE" : "FALSE")},
                   updated_by = '{Escape(model.UpdatedBy)}',
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateCategoryAsync");
    }

    public async Task<bool> DeleteCategoryAsync(string id)
    {
        var sql = $@"
            UPDATE post_categories
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteCategoryAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE post_categories
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

    private static AdminPostCategoryItem MapItem(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        CategoryCode = row["category_code"]?.ToString(),
        CategoryName = row["category_name"]?.ToString(),
        Slug = row["slug"]?.ToString(),
        ParentId = row["parent_id"]?.ToString(),
        Description = row["description"]?.ToString(),
        SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
        IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
    };

    private static string ToNullableBigIntSql(string? value)
        => string.IsNullOrWhiteSpace(value) ? "NULL" : $"'{Escape(value)}'";

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminPostCategoryItem
{
    public string? Id { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public string? Slug { get; set; }
    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class AdminPostCategoryDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public string? Slug { get; set; }
    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
