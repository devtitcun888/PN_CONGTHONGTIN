using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminDocumentTypeService
{
    Task<List<AdminDocumentTypeItem>> GetDocumentTypesAsync(string maTruongBo);
    Task<AdminDocumentTypeDetail?> GetDocumentTypeByIdAsync(string id);
    Task<bool> CreateDocumentTypeAsync(AdminDocumentTypeDetail model);
    Task<bool> UpdateDocumentTypeAsync(AdminDocumentTypeDetail model);
    Task<bool> DeleteDocumentTypeAsync(string id);
    Task<bool> SetActiveAsync(string id, bool isActive);
}

public class AdminDocumentTypeService : IAdminDocumentTypeService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminDocumentTypeService> _logger;

    public AdminDocumentTypeService(ILogger<AdminDocumentTypeService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminDocumentTypeItem>> GetDocumentTypesAsync(string maTruongBo)
    {
        var result = new List<AdminDocumentTypeItem>();
        var sql = $@"
            SELECT id, type_code, type_name, slug, description, sort_order, is_active, created_at
            FROM document_types
            WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new AdminDocumentTypeItem
            {
                Id = row["id"]?.ToString(),
                TypeCode = row["type_code"]?.ToString(),
                TypeName = row["type_name"]?.ToString(),
                Slug = row["slug"]?.ToString(),
                Description = row["description"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
                IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"]),
                CreatedAt = row["created_at"] == DBNull.Value ? null : Convert.ToDateTime(row["created_at"])
            });
        }

        return result;
    }

    public async Task<AdminDocumentTypeDetail?> GetDocumentTypeByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, type_code, type_name, slug, description, sort_order, is_active
            FROM document_types
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new AdminDocumentTypeDetail
        {
            Id = row["id"]?.ToString(),
            MaTruongBo = row["ma_truong_bo"]?.ToString(),
            TypeCode = row["type_code"]?.ToString(),
            TypeName = row["type_name"]?.ToString(),
            Slug = row["slug"]?.ToString(),
            Description = row["description"]?.ToString(),
            SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
            IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<bool> CreateDocumentTypeAsync(AdminDocumentTypeDetail model)
    {
        var sql = $@"
            INSERT INTO document_types
            (ma_truong_bo, type_code, type_name, slug, description, sort_order, is_active, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.TypeCode)}', '{Escape(model.TypeName)}', '{Escape(model.Slug)}',
             '{Escape(model.Description)}', {model.SortOrder}, {(model.IsActive ? "TRUE" : "FALSE")}, NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreateDocumentTypeAsync");
    }

    public async Task<bool> UpdateDocumentTypeAsync(AdminDocumentTypeDetail model)
    {
        var sql = $@"
            UPDATE document_types
               SET type_code = '{Escape(model.TypeCode)}',
                   type_name = '{Escape(model.TypeName)}',
                   slug = '{Escape(model.Slug)}',
                   description = '{Escape(model.Description)}',
                   sort_order = {model.SortOrder},
                   is_active = {(model.IsActive ? "TRUE" : "FALSE")},
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateDocumentTypeAsync");
    }

    public async Task<bool> DeleteDocumentTypeAsync(string id)
    {
        var sql = $@"
            UPDATE document_types
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteDocumentTypeAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE document_types
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

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminDocumentTypeItem
{
    public string? Id { get; set; }
    public string? TypeCode { get; set; }
    public string? TypeName { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class AdminDocumentTypeDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? TypeCode { get; set; }
    public string? TypeName { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
