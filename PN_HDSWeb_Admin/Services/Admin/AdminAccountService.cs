using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Admin.Data.Model;
using PN_HDSWeb_Library;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminAccountService
{
    Task<List<AdminAccountListItemDto>> GetAccountsAsync(string maTruongBo, string? keyword = null, bool? isActive = null, bool? isLocked = null, int page = 1, int pageSize = 20);
    Task<int> GetAccountsCountAsync(string maTruongBo, string? keyword = null, bool? isActive = null, bool? isLocked = null);
    Task<AdminAccountDetailDto?> GetAccountByIdAsync(string id);
    Task<AdminAccountDetailDto?> GetAccountByUsernameAsync(string maTruongBo, string username);
    Task<bool> CreateAccountAsync(AdminAccountUpsertDto model);
    Task<bool> UpdateAccountAsync(AdminAccountUpsertDto model);
    Task<bool> UsernameExistsAsync(string maTruongBo, string username, string? excludeId = null);
    Task<bool> SetActiveAsync(string id, bool isActive);
    Task<bool> SetLockedAsync(string id, bool isLocked, string? reason = null);
    Task<bool> UpdateLastLoginAsync(string id, string? ipAddress = null);
    Task<bool> ResetPasswordAsync(string id, string newPassword);
    Task<bool> DeleteAccountAsync(string id);
}

public class AdminAccountService : IAdminAccountService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminAccountService> _logger;

    public AdminAccountService(ILogger<AdminAccountService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminAccountListItemDto>> GetAccountsAsync(string maTruongBo, string? keyword = null, bool? isActive = null, bool? isLocked = null, int page = 1, int pageSize = 20)
    {
        var result = new List<AdminAccountListItemDto>();
        var offset = Math.Max(page - 1, 0) * pageSize;
        var where = BuildWhere(maTruongBo, keyword, isActive, isLocked);

        var sql = $@"
            SELECT id, ma_truong_bo, username, full_name, display_name, email, phone,
                   role_code, auth_type, is_active, is_locked, lock_reason, last_login_at
            FROM l_user_account
            {where}
            ORDER BY created_at DESC
            LIMIT {pageSize} OFFSET {offset}";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            foreach (DataRow row in dt.Rows)
            {
                result.Add(MapListItem(row));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAccountsAsync failed");
            throw;
        }

        return result;
    }

    public async Task<int> GetAccountsCountAsync(string maTruongBo, string? keyword = null, bool? isActive = null, bool? isLocked = null)
    {
        var where = BuildWhere(maTruongBo, keyword, isActive, isLocked);
        var sql = $@"SELECT COUNT(*) AS total FROM l_user_account {where}";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return 0;
        return dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
    }

    public async Task<AdminAccountDetailDto?> GetAccountByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, username, password_hash, full_name, display_name, email, phone,
                   role_code, auth_type, sso_username, sso_user_id, device_name, last_login_at,
                   last_login_ip, is_active, is_locked, lock_reason
            FROM l_user_account
            WHERE is_deleted = FALSE
              AND id = '{Escape(id)}'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 ? null : MapDetail(dt.Rows[0]);
    }

    public async Task<AdminAccountDetailDto?> GetAccountByUsernameAsync(string maTruongBo, string username)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, username, password_hash, full_name, display_name, email, phone,
                   role_code, auth_type, sso_username, sso_user_id, device_name, last_login_at,
                   last_login_ip, is_active, is_locked, lock_reason
            FROM l_user_account
            WHERE is_deleted = FALSE
              AND ma_truong_bo = '{Escape(maTruongBo)}'
              AND username = '{Escape(username)}'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 ? null : MapDetail(dt.Rows[0]);
    }

    public async Task<bool> UsernameExistsAsync(string maTruongBo, string username, string? excludeId = null)
    {
        var sql = $@"
            SELECT id
            FROM l_user_account
            WHERE is_deleted = FALSE
              AND ma_truong_bo = '{Escape(maTruongBo)}'
              AND username = '{Escape(username)}'
              {(string.IsNullOrWhiteSpace(excludeId) ? string.Empty : $"AND id <> '{Escape(excludeId)}'")}
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count > 0;
    }

    public async Task<bool> CreateAccountAsync(AdminAccountUpsertDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            throw new InvalidOperationException("Password is required when creating an account.");

        var passwordHash = HashPassword(model.Password);
        var sql = $@"
            INSERT INTO l_user_account
            (ma_truong_bo, username, password_hash, full_name, display_name, email, phone,
             role_code, auth_type, sso_username, sso_user_id, device_name, is_active, is_locked, lock_reason, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.UserName)}', '{Escape(passwordHash)}', '{Escape(model.FullName)}',
             '{Escape(model.DisplayName)}', '{Escape(model.Email)}', '{Escape(model.Phone)}',
             '{Escape(model.Roles)}', '{Escape(model.AuthType)}', '{Escape(model.SsoUserName)}', '{Escape(model.SsoUserId)}',
             '{Escape(model.DeviceName)}', {(model.IsActive ? "TRUE" : "FALSE")}, {(model.IsLocked ? "TRUE" : "FALSE")}, '{Escape(model.LockReason)}', NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreateAccountAsync");
    }

    public async Task<bool> UpdateAccountAsync(AdminAccountUpsertDto model)
    {
        var sql = $@"
            UPDATE l_user_account
               SET full_name = '{Escape(model.FullName)}',
                   display_name = '{Escape(model.DisplayName)}',
                   email = '{Escape(model.Email)}',
                   phone = '{Escape(model.Phone)}',
                   role_code = '{Escape(model.Roles)}',
                   auth_type = '{Escape(model.AuthType)}',
                   sso_username = '{Escape(model.SsoUserName)}',
                   sso_user_id = '{Escape(model.SsoUserId)}',
                   device_name = '{Escape(model.DeviceName)}',
                   is_active = {(model.IsActive ? "TRUE" : "FALSE")},
                   is_locked = {(model.IsLocked ? "TRUE" : "FALSE")},
                   lock_reason = '{Escape(model.LockReason)}',
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateAccountAsync");
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive)
    {
        var sql = $@"
            UPDATE l_user_account
               SET is_active = {(isActive ? "TRUE" : "FALSE")},
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "SetActiveAsync");
    }

    public async Task<bool> SetLockedAsync(string id, bool isLocked, string? reason = null)
    {
        var sql = $@"
            UPDATE l_user_account
               SET is_locked = {(isLocked ? "TRUE" : "FALSE")},
                   lock_reason = '{Escape(reason)}',
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "SetLockedAsync");
    }

    public async Task<bool> UpdateLastLoginAsync(string id, string? ipAddress = null)
    {
        var sql = $@"
            UPDATE l_user_account
               SET last_login_at = NOW(),
                   last_login_ip = '{Escape(ipAddress)}',
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "UpdateLastLoginAsync");
    }

    public async Task<bool> ResetPasswordAsync(string id, string newPassword)
    {
        var sql = $@"
            UPDATE l_user_account
               SET password_hash = '{Escape(HashPassword(newPassword))}',
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "ResetPasswordAsync");
    }

    public async Task<bool> DeleteAccountAsync(string id)
    {
        var sql = $@"
            UPDATE l_user_account
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteAccountAsync");
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

    private static string BuildWhere(string maTruongBo, string? keyword, bool? isActive, bool? isLocked)
    {
        var clauses = new List<string>
        {
            $"is_deleted = FALSE",
            $"ma_truong_bo = '{Escape(maTruongBo)}'"
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Escape(keyword);
            clauses.Add($"(username ILIKE '%{k}%' OR full_name ILIKE '%{k}%' OR display_name ILIKE '%{k}%')");
        }

        if (isActive.HasValue)
            clauses.Add($"is_active = {(isActive.Value ? "TRUE" : "FALSE")}");

        if (isLocked.HasValue)
            clauses.Add($"is_locked = {(isLocked.Value ? "TRUE" : "FALSE")}");

        return "WHERE " + string.Join(" AND ", clauses);
    }

    private static AdminAccountListItemDto MapListItem(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        MaTruongBo = row["ma_truong_bo"]?.ToString(),
        UserName = row["username"]?.ToString(),
        FullName = row["full_name"]?.ToString(),
        DisplayName = row["display_name"]?.ToString(),
        Email = row["email"]?.ToString(),
        Phone = row["phone"]?.ToString(),
        Roles = row["role_code"]?.ToString(),
        AuthType = row["auth_type"]?.ToString(),
        IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"]),
        IsLocked = row["is_locked"] != DBNull.Value && Convert.ToBoolean(row["is_locked"]),
        LockReason = row["lock_reason"]?.ToString(),
        LastLoginAt = row["last_login_at"] == DBNull.Value ? null : Convert.ToDateTime(row["last_login_at"])
    };

    private static AdminAccountDetailDto MapDetail(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        MaTruongBo = row["ma_truong_bo"]?.ToString(),
        UserName = row["username"]?.ToString(),
        Password = row["password_hash"]?.ToString(),
        FullName = row["full_name"]?.ToString(),
        DisplayName = row["display_name"]?.ToString(),
        Email = row["email"]?.ToString(),
        Phone = row["phone"]?.ToString(),
        Roles = row["role_code"]?.ToString(),
        AuthType = row["auth_type"]?.ToString(),
        SsoUserName = row["sso_username"]?.ToString(),
        SsoUserId = row["sso_user_id"]?.ToString(),
        DeviceName = row["device_name"]?.ToString(),
        IsActive = row["is_active"] != DBNull.Value && Convert.ToBoolean(row["is_active"]),
        IsLocked = row["is_locked"] != DBNull.Value && Convert.ToBoolean(row["is_locked"]),
        LockReason = row["lock_reason"]?.ToString(),
        LastLoginAt = row["last_login_at"] == DBNull.Value ? null : Convert.ToDateTime(row["last_login_at"]),
        LastLoginIp = row["last_login_ip"]?.ToString()
    };

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}
