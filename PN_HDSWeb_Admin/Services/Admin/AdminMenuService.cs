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

    public static string ToSlug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.ToLowerInvariant().Trim();

        string[] arr1 = new string[] { 
            "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
            "đ",
            "é", "è", "ẻ", "ẽ", "ẹ", "ê", "ế", "ề", "ể", "ễ", "ệ",
            "í", "ì", "ỉ", "ĩ", "ị",
            "ó", "ò", "ỏ", "õ", "ọ", "ô", "ố", "ồ", "ổ", "ỗ", "ộ", "ơ", "ớ", "ờ", "ở", "ỡ", "ợ",
            "ú", "ù", "ủ", "ũ", "ụ", "ư", "ứ", "ừ", "ử", "ữ", "ự",
            "ý", "ỳ", "ỷ", "ỹ", "ỵ"
        };
        string[] arr2 = new string[] { 
            "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
            "d",
            "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e",
            "i", "i", "i", "i", "i",
            "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o",
            "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u",
            "y", "y", "y", "y", "y"
        };
        for (int i = 0; i < arr1.Length; i++)
        {
            text = text.Replace(arr1[i], arr2[i]);
        }

        var sb = new System.Text.StringBuilder();
        foreach (char c in text)
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == ' ' || c == '-')
            {
                sb.Append(c);
            }
        }
        text = sb.ToString();

        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "-");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"-+", "-");
        text = text.Trim('-');

        return text;
    }

    public static string GenerateCategoryCode(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var slug = ToSlug(name);
        var words = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return string.Empty;
        if (words.Length == 1) return words[0].ToUpperInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (var w in words)
        {
            if (w.Length > 0) sb.Append(w[0]);
        }
        return sb.ToString().ToUpperInvariant();
    }

    public static string GenerateTypeCode(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var slug = ToSlug(name);
        var words = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return string.Empty;
        if (words.Length == 1) return words[0].ToUpperInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (var w in words)
        {
            if (w.Length > 0) sb.Append(w[0]);
        }
        return sb.ToString().ToUpperInvariant();
    }

    public static string GeneratePageCode(string? title)
    {
        return ToSlug(title);
    }

    private async Task SyncPostCategoryAsync(string menuId, string maTruongBo, string menuName, string? parentId, int sortOrder, bool isActive)
    {
        var categoryCode = $"MENU_{menuId}";
        if (!string.IsNullOrWhiteSpace(parentId))
        {
            // If it becomes a child menu, we soft-delete any previously synced root category
            var deleteSql = $@"
                UPDATE post_categories
                   SET is_deleted = TRUE,
                       updated_at = NOW()
                 WHERE category_code = '{Escape(categoryCode)}'";
            await hdataLib.hrunQueryAsync(LoginID_Index, deleteSql);
            return;
        }

        // Otherwise sync it as a root category
        var slug = ToSlug(menuName);
        var checkSql = $"SELECT id FROM post_categories WHERE category_code = '{Escape(categoryCode)}' AND is_deleted = FALSE LIMIT 1";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, checkSql);
        
        if (dt.Rows.Count > 0)
        {
            var updateSql = $@"
                UPDATE post_categories
                   SET category_name = '{Escape(menuName)}',
                       slug = '{Escape(slug)}',
                       sort_order = {sortOrder},
                       is_active = {(isActive ? "TRUE" : "FALSE")},
                       updated_by = 'System',
                       updated_at = NOW()
                 WHERE category_code = '{Escape(categoryCode)}'";
            await hdataLib.hrunQueryAsync(LoginID_Index, updateSql);
        }
        else
        {
            var insertSql = $@"
                INSERT INTO post_categories
                (ma_truong_bo, category_code, category_name, slug, parent_id, description, sort_order, is_active, created_by, updated_by, created_at, updated_at, is_deleted)
                VALUES
                ('{Escape(maTruongBo)}', '{Escape(categoryCode)}', '{Escape(menuName)}', '{Escape(slug)}',
                 NULL, 'Tự động tạo từ Menu: {Escape(menuName)}', {sortOrder}, {(isActive ? "TRUE" : "FALSE")},
                 'System', 'System', NOW(), NOW(), FALSE)";
            await hdataLib.hrunQueryAsync(LoginID_Index, insertSql);
        }
    }

    public async Task<bool> CreateMenuAsync(AdminMenuDetail model)
    {
        var parentIdSql = ToNullableBigIntSql(model.ParentId);
        
        // Enforce the URL based on the slug generated from MenuName
        if (string.IsNullOrWhiteSpace(model.ParentId))
        {
            var slug = ToSlug(model.MenuName);
            model.Url = $"/posts/category/{slug}";
        }

        var sql = $@"
            INSERT INTO menus
            (ma_truong_bo, menu_name, parent_id, url, target, sort_order, is_active, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.MenuName)}', {parentIdSql}, '{Escape(model.Url)}',
             '{Escape(model.Target)}', {model.SortOrder}, {(model.IsActive ? "TRUE" : "FALSE")}, NOW(), NOW(), FALSE)
            RETURNING id";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            if (dt.Rows.Count > 0)
            {
                var id = dt.Rows[0]["id"]?.ToString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    model.Id = id;
                    await SyncPostCategoryAsync(id, model.MaTruongBo ?? string.Empty, model.MenuName ?? string.Empty, model.ParentId, model.SortOrder, model.IsActive);
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateMenuAsync failed");
            throw;
        }
    }

    public async Task<bool> UpdateMenuAsync(AdminMenuDetail model)
    {
        var parentIdSql = ToNullableBigIntSql(model.ParentId);
        
        // Enforce the URL based on the slug generated from MenuName
        if (string.IsNullOrWhiteSpace(model.ParentId))
        {
            var slug = ToSlug(model.MenuName);
            model.Url = $"/posts/category/{slug}";
        }

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

        var ok = await RunAsync(sql, "UpdateMenuAsync");
        if (ok && !string.IsNullOrWhiteSpace(model.Id))
        {
            await SyncPostCategoryAsync(model.Id, model.MaTruongBo ?? string.Empty, model.MenuName ?? string.Empty, model.ParentId, model.SortOrder, model.IsActive);
        }
        return ok;
    }

    public async Task<bool> DeleteMenuAsync(string id)
    {
        var sql = $@"
            UPDATE menus
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        var ok = await RunAsync(sql, "DeleteMenuAsync");
        if (ok)
        {
            var categoryCode = $"MENU_{id}";
            var categorySql = $@"
                UPDATE post_categories
                   SET is_deleted = TRUE,
                       updated_at = NOW()
                 WHERE category_code = '{Escape(categoryCode)}'";
            await hdataLib.hrunQueryAsync(LoginID_Index, categorySql);
        }
        return ok;
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE menus
               SET is_active = {(isActive ? "TRUE" : "FALSE")},
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        var ok = await RunAsync(sql, "SetActiveAsync");
        if (ok)
        {
            var categoryCode = $"MENU_{id}";
            var categorySql = $@"
                UPDATE post_categories
                   SET is_active = {(isActive ? "TRUE" : "FALSE")},
                       updated_at = NOW()
                 WHERE category_code = '{Escape(categoryCode)}'";
            await hdataLib.hrunQueryAsync(LoginID_Index, categorySql);
        }
        return ok;
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
