using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminStaffProfileService
{
    Task<List<AdminStaffProfileItem>> GetStaffProfilesAsync(string maTruongBo, string? keyword = null, bool? isPublic = null, bool? isActive = null);
    Task<AdminStaffProfileDetail?> GetStaffProfileByIdAsync(string id);
    Task<bool> CreateStaffProfileAsync(AdminStaffProfileDetail model);
    Task<bool> UpdateStaffProfileAsync(AdminStaffProfileDetail model);
    Task<bool> DeleteStaffProfileAsync(string id);
    Task<bool> SetPublicAsync(string id, bool isPublic);
    Task<bool> SetActiveAsync(string id, bool isActive);
}

public class AdminStaffProfileService : IAdminStaffProfileService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminStaffProfileService> _logger;

    public AdminStaffProfileService(ILogger<AdminStaffProfileService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminStaffProfileItem>> GetStaffProfilesAsync(string maTruongBo, string? keyword = null, bool? isPublic = null, bool? isActive = null)
    {
        var result = new List<AdminStaffProfileItem>();
        var sql = $@"
            SELECT id, group_name, full_name, position_name, qualification, avatar_url, email, phone, sort_order, is_public, is_active, created_at
            FROM staff_profiles
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              {(isPublic.HasValue ? $" AND is_public = {(isPublic.Value ? "TRUE" : "FALSE")}" : string.Empty)}
              {(isActive.HasValue ? $" AND is_active = {(isActive.Value ? "TRUE" : "FALSE")}" : string.Empty)}
              {(string.IsNullOrWhiteSpace(keyword) ? string.Empty : $" AND (full_name ILIKE '%{Escape(keyword)}%' OR position_name ILIKE '%{Escape(keyword)}%' OR qualification ILIKE '%{Escape(keyword)}%' OR group_name ILIKE '%{Escape(keyword)}%')")}
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapItem(row));
        }

        return result;
    }

    public async Task<AdminStaffProfileDetail?> GetStaffProfileByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, group_name, full_name, position_name, qualification, certificate_info, bio, avatar_url, email, phone, sort_order, is_public, is_active
            FROM staff_profiles
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new AdminStaffProfileDetail
        {
            Id = row["id"]?.ToString(),
            MaTruongBo = row["ma_truong_bo"]?.ToString(),
            GroupName = row["group_name"]?.ToString(),
            FullName = row["full_name"]?.ToString(),
            PositionName = row["position_name"]?.ToString(),
            Qualification = row["qualification"]?.ToString(),
            CertificateInfo = row["certificate_info"]?.ToString(),
            Bio = row["bio"]?.ToString(),
            AvatarUrl = row["avatar_url"]?.ToString(),
            Email = row["email"]?.ToString(),
            Phone = row["phone"]?.ToString(),
            SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
            IsPublic = row["is_public"] != DBNull.Value && Convert.ToBoolean(row["is_public"]),
            IsActive = row["is_active"] == DBNull.Value || Convert.ToBoolean(row["is_active"])
        };
    }

    public async Task<bool> CreateStaffProfileAsync(AdminStaffProfileDetail model)
    {
        var sql = $@"
            INSERT INTO staff_profiles
            (ma_truong_bo, group_name, full_name, position_name, qualification, certificate_info, bio, avatar_url, email, phone, sort_order, is_public, is_active, created_by, updated_by, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.GroupName)}', '{Escape(model.FullName)}', '{Escape(model.PositionName)}', '{Escape(model.Qualification)}', '{Escape(model.CertificateInfo)}', '{Escape(model.Bio)}',
             '{Escape(model.AvatarUrl)}', '{Escape(model.Email)}', '{Escape(model.Phone)}', {model.SortOrder}, {(model.IsPublic ? "TRUE" : "FALSE")}, {(model.IsActive ? "TRUE" : "FALSE")},
             '{Escape(model.CreatedBy)}', '{Escape(model.UpdatedBy)}', NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreateStaffProfileAsync");
    }

    public async Task<bool> UpdateStaffProfileAsync(AdminStaffProfileDetail model)
    {
        var sql = $@"
            UPDATE staff_profiles
               SET group_name = '{Escape(model.GroupName)}',
                   full_name = '{Escape(model.FullName)}',
                   position_name = '{Escape(model.PositionName)}',
                   qualification = '{Escape(model.Qualification)}',
                   certificate_info = '{Escape(model.CertificateInfo)}',
                   bio = '{Escape(model.Bio)}',
                   avatar_url = '{Escape(model.AvatarUrl)}',
                   email = '{Escape(model.Email)}',
                   phone = '{Escape(model.Phone)}',
                   sort_order = {model.SortOrder},
                   is_public = {(model.IsPublic ? "TRUE" : "FALSE")},
                   is_active = {(model.IsActive ? "TRUE" : "FALSE")},
                   updated_by = '{Escape(model.UpdatedBy)}',
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateStaffProfileAsync");
    }

    public async Task<bool> DeleteStaffProfileAsync(string id)
    {
        var sql = $@"
            UPDATE staff_profiles
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteStaffProfileAsync");
    }

    public async Task<bool> SetPublicAsync(string id, bool isPublic)
    {
        var sql = $@"
            UPDATE staff_profiles
               SET is_public = {(isPublic ? "TRUE" : "FALSE")},
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "SetPublicAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE staff_profiles
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

    private static AdminStaffProfileItem MapItem(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        GroupName = row.Table.Columns.Contains("group_name") ? row["group_name"]?.ToString() : null,
        FullName = row["full_name"]?.ToString(),
        PositionName = row["position_name"]?.ToString(),
        Qualification = row["qualification"]?.ToString(),
        AvatarUrl = row["avatar_url"]?.ToString(),
        Email = row["email"]?.ToString(),
        Phone = row["phone"]?.ToString(),
        SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
        IsPublic = row["is_public"] != DBNull.Value && Convert.ToBoolean(row["is_public"]),
        IsActive = row["is_active"] == DBNull.Value || Convert.ToBoolean(row["is_active"]),
        CreatedAt = row["created_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["created_at"])
    };

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminStaffProfileItem
{
    public string? Id { get; set; }
    public string? GroupName { get; set; }
    public string? FullName { get; set; }
    public string? PositionName { get; set; }
    public string? Qualification { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublic { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminStaffProfileDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? GroupName { get; set; }
    public string? FullName { get; set; }
    public string? PositionName { get; set; }
    public string? Qualification { get; set; }
    public string? CertificateInfo { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
