using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Admin.Data.Model;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Authentication;

public interface IUserAccountService
{
    Task<ThongTinTruongV2?> GetThongTinTruong(string maTruongBo);
    Task<ThongTinNguoiDung?> GetThongTinNguoiDung(string userId);
    Task<UserAccountData_?> GetLocalAccountAsync(string maTruongBo, string username);
    Task<UserAccountData_?> GetLocalAccountBySsoUserIdAsync(string ssoUserId);
}

public class UserAccountService : IUserAccountService
{
    private readonly string _loginID_Index;
    private readonly string _loginID_TruongData;
    private readonly ILogger<UserAccountService> _logger;

    public UserAccountService(ILogger<UserAccountService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrEmpty(PN_LoginService.LoginID_CongThongTin))
            throw new InvalidOperationException("LoginID_Index chưa được khởi tạo");

        if (string.IsNullOrEmpty(PN_LoginService.LoginID_School_Dev))
            throw new InvalidOperationException("LoginID_School_Dev chưa được khởi tạo");

        _loginID_Index = PN_LoginService.LoginID_CongThongTin;
        _loginID_TruongData = PN_LoginService.LoginID_School_Dev;
    }

    public async Task<ThongTinTruongV2?> GetThongTinTruong(string maTruongBo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(maTruongBo))
            {
                _logger.LogWarning("MaTruongBo is null or empty");
                return null;
            }

            string safeMaTruong = maTruongBo.Replace("'", "''");

            string query = $@"
                SELECT ma_truong_bo, tentruong, caphoc, trangthai
                FROM public.l_truong
                WHERE ma_truong_bo = '{safeMaTruong}'
                LIMIT 1";

            DataTable dt = await hdataLib.hgetDataTableAsync(_loginID_Index, query);
            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];
            return new ThongTinTruongV2
            {
                MaTruongBo = row["ma_truong_bo"]?.ToString(),
                TenTruong = row["tentruong"]?.ToString(),
                Cap = ParsePostgresArray(row["caphoc"]),
                TrangThai = row["trangthai"] == DBNull.Value ? 0 : Convert.ToInt32(row["trangthai"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting school info for {MaTruongBo}", maTruongBo);
            throw;
        }
    }

    public async Task<ThongTinNguoiDung?> GetThongTinNguoiDung(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("UserId is null or empty");
                return null;
            }

            string safeUser = userId.Replace("'", "''");

            string query = $@"
                SELECT ma_so, ho_ten, ma_truong_bo, ma_chuc_vu
                FROM l_giaovien
                WHERE ma_so = '{safeUser}'
                LIMIT 1";

            DataTable dt = await hdataLib.hgetDataTableAsync(_loginID_TruongData, query);
            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];
            return new ThongTinNguoiDung
            {
                MaSo = row["ma_so"]?.ToString(),
                HoTen = row["ho_ten"]?.ToString(),
                MaTruongBo = row["ma_truong_bo"]?.ToString(),
                MaChucVu = row["ma_chuc_vu"] == DBNull.Value ? 0 : Convert.ToInt32(row["ma_chuc_vu"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info for {UserId}", userId);
            throw;
        }
    }

    public async Task<UserAccountData_?> GetLocalAccountAsync(string maTruongBo, string username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(maTruongBo) || string.IsNullOrWhiteSpace(username))
                return null;

            string sql = $@"
                SELECT id, ma_truong_bo, username, password_hash, full_name, display_name, email, phone,
                       role_code, auth_type, sso_username, sso_user_id, device_name, last_login_at,
                       last_login_ip, is_active, is_locked, lock_reason, created_at, updated_at, is_deleted
                FROM l_user_account
                WHERE ma_truong_bo = '{maTruongBo.Replace("'", "''")}'
                  AND username = '{username.Replace("'", "''")}'
                  AND is_deleted = FALSE
                LIMIT 1";

            var dt = await hdataLib.hgetDataTableAsync(_loginID_Index, sql);
            return MapLocalAccount(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting local account for {MaTruongBo}/{Username}", maTruongBo, username);
            throw;
        }
    }

    public async Task<UserAccountData_?> GetLocalAccountBySsoUserIdAsync(string ssoUserId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ssoUserId))
                return null;

            string sql = $@"
                SELECT id, ma_truong_bo, username, password_hash, full_name, display_name, email, phone,
                       role_code, auth_type, sso_username, sso_user_id, device_name, last_login_at,
                       last_login_ip, is_active, is_locked, lock_reason, created_at, updated_at, is_deleted
                FROM l_user_account
                WHERE sso_user_id = '{ssoUserId.Replace("'", "''")}'
                  AND is_deleted = FALSE
                LIMIT 1";

            var dt = await hdataLib.hgetDataTableAsync(_loginID_Index, sql);
            return MapLocalAccount(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting local account by SSO user id {SsoUserId}", ssoUserId);
            throw;
        }
    }

    private static UserAccountData_? MapLocalAccount(DataTable dt)
    {
        if (dt.Rows.Count == 0)
            return null;

        var row = dt.Rows[0];
        return new UserAccountData_
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
            IsActive = row["is_active"] == DBNull.Value || Convert.ToBoolean(row["is_active"]),
            IsLocked = row["is_locked"] != DBNull.Value && Convert.ToBoolean(row["is_locked"])
        };
    }

    private static string[] ParsePostgresArray(object value)
    {
        if (value == null || value == DBNull.Value)
            return Array.Empty<string>();

        if (value is string[] arr)
            return arr;

        var raw = value.ToString();
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        if (raw.StartsWith("{") && raw.EndsWith("}"))
        {
            return raw.Trim('{', '}').Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        }

        return new[] { raw };
    }
}
