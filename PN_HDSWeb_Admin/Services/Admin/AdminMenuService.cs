using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminMenuService
{
    Task<List<AdminMenuItem>> GetMenusAsync(string maTruongBo);
    Task<AdminMenuDetail?> GetMenuByIdAsync(string id);
    Task<bool> CreateMenuAsync(AdminMenuDetail model);
    Task<bool> UpdateMenuAsync(AdminMenuDetail model);
    Task<bool> DeleteMenuAsync(string id);
    Task<bool> SetActiveAsync(string id, bool isActive);
}

public class AdminMenuService : IAdminMenuService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminMenuService> _logger;

    public AdminMenuService(ILogger<AdminMenuService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminMenuItem>> GetMenusAsync(string maTruongBo)
    {
        var result = new List<AdminMenuItem>();
        var sql = $@"
            SELECT id, menu_name, parent_id, url, target, sort_order, is_active
            FROM menus
            WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE
            ORDER BY sort_order ASC, created_at DESC";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new AdminMenuItem
                {
                    Id = row["id"]?.ToString(),
                    MenuName = row["menu_name"]?.ToString(),
                    ParentId = row["parent_id"]?.ToString(),
                    Url = row["url"]?.ToString(),
                    Target = row["target"]?.ToString(),
                    SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
                    IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMenusAsync failed");
            throw;
        }

        return result;
    }

    public async Task<AdminMenuDetail?> GetMenuByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, menu_name, parent_id, url, target, sort_order, is_active
            FROM menus
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new AdminMenuDetail
        {
            Id = row["id"]?.ToString(),
            MaTruongBo = row["ma_truong_bo"]?.ToString(),
            MenuName = row["menu_name"]?.ToString(),
            ParentId = row["parent_id"]?.ToString(),
            Url = row["url"]?.ToString(),
            Target = row["target"]?.ToString(),
            SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
            IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<bool> CreateMenuAsync(AdminMenuDetail model)
    {
        var parentIdSql = ToNullableBigIntSql(model.ParentId);
        var sql = $@"
            INSERT INTO menus
            (ma_truong_bo, menu_name, parent_id, url, target, sort_order, is_active, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.MenuName)}', {parentIdSql}, '{Escape(model.Url)}',
             '{Escape(model.Target)}', {model.SortOrder}, {(model.IsActive ? "TRUE" : "FALSE")}, NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreateMenuAsync");
    }

    public async Task<bool> UpdateMenuAsync(AdminMenuDetail model)
    {
        var parentIdSql = ToNullableBigIntSql(model.ParentId);
        var sql = $@"
            UPDATE menus
               SET menu_name = '{Escape(model.MenuName)}',
                   parent_id = {parentIdSql},
                   url = '{Escape(model.Url)}',
                   target = '{Escape(model.Target)}',
                   sort_order = {model.SortOrder},
                   is_active = {(model.IsActive ? "TRUE" : "FALSE")},
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateMenuAsync");
    }

    public async Task<bool> DeleteMenuAsync(string id)
    {
        var sql = $@"
            UPDATE menus
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteMenuAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE menus
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

    private static string ToNullableBigIntSql(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "NULL" : $"'{Escape(value)}'";
    }
}

public class AdminMenuItem
{
    public string? Id { get; set; }
    public string? MenuName { get; set; }
    public string? ParentId { get; set; }
    public string? Url { get; set; }
    public string? Target { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class AdminMenuDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? MenuName { get; set; }
    public string? ParentId { get; set; }
    public string? Url { get; set; }
    public string? Target { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
