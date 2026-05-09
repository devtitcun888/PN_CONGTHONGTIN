using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminBannerService
{
    Task<List<AdminBannerItem>> GetBannersAsync(string maTruongBo);
    Task<AdminBannerDetail?> GetBannerByIdAsync(string id);
    Task<bool> CreateBannerAsync(AdminBannerDetail model);
    Task<bool> UpdateBannerAsync(AdminBannerDetail model);
    Task<bool> DeleteBannerAsync(string id);
    Task<bool> SetActiveAsync(string id, bool isActive);
}

public class AdminBannerService : IAdminBannerService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminBannerService> _logger;

    public AdminBannerService(ILogger<AdminBannerService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminBannerItem>> GetBannersAsync(string maTruongBo)
    {
        var result = new List<AdminBannerItem>();
        var sql = $@"
            SELECT id, title, image_url, link_url, position, sort_order, is_active
            FROM banners
            WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE
            ORDER BY sort_order ASC, created_at DESC";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new AdminBannerItem
                {
                    Id = row["id"]?.ToString(),
                    Title = row["title"]?.ToString(),
                    ImageUrl = row["image_url"]?.ToString(),
                    LinkUrl = row["link_url"]?.ToString(),
                    Position = row["position"]?.ToString(),
                    SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
                    IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"])
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBannersAsync failed");
            throw;
        }

        return result;
    }

    public async Task<AdminBannerDetail?> GetBannerByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, title, image_url, link_url, position, sort_order, is_active, start_date, end_date
            FROM banners
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new AdminBannerDetail
        {
            Id = row["id"]?.ToString(),
            MaTruongBo = row["ma_truong_bo"]?.ToString(),
            Title = row["title"]?.ToString(),
            ImageUrl = row["image_url"]?.ToString(),
            LinkUrl = row["link_url"]?.ToString(),
            Position = row["position"]?.ToString(),
            SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
            IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"]),
            StartDate = row["start_date"] == DBNull.Value ? null : Convert.ToDateTime(row["start_date"]),
            EndDate = row["end_date"] == DBNull.Value ? null : Convert.ToDateTime(row["end_date"])
        };
    }

    public async Task<bool> CreateBannerAsync(AdminBannerDetail model)
    {
        var sql = $@"
            INSERT INTO banners
            (ma_truong_bo, title, image_url, link_url, position, sort_order, is_active, start_date, end_date, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.Title)}', '{Escape(model.ImageUrl)}', '{Escape(model.LinkUrl)}',
             '{Escape(model.Position)}', {model.SortOrder}, {(model.IsActive ? "TRUE" : "FALSE")},
             {(model.StartDate.HasValue ? $"'{model.StartDate:yyyy-MM-dd}'" : "NULL")},
             {(model.EndDate.HasValue ? $"'{model.EndDate:yyyy-MM-dd}'" : "NULL")}, NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreateBannerAsync");
    }

    public async Task<bool> UpdateBannerAsync(AdminBannerDetail model)
    {
        var sql = $@"
            UPDATE banners
               SET title = '{Escape(model.Title)}',
                   image_url = '{Escape(model.ImageUrl)}',
                   link_url = '{Escape(model.LinkUrl)}',
                   position = '{Escape(model.Position)}',
                   sort_order = {model.SortOrder},
                   is_active = {(model.IsActive ? "TRUE" : "FALSE")},
                   start_date = {(model.StartDate.HasValue ? $"'{model.StartDate:yyyy-MM-dd}'" : "NULL")},
                   end_date = {(model.EndDate.HasValue ? $"'{model.EndDate:yyyy-MM-dd}'" : "NULL")},
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateBannerAsync");
    }

    public async Task<bool> DeleteBannerAsync(string id)
    {
        var sql = $@"
            UPDATE banners
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteBannerAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE banners
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

public class AdminBannerItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? Position { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class AdminBannerDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? Title { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? Position { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
