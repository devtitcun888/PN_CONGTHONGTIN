using hDataLibraryN8;
using Npgsql;
using PN_HDSWeb_Library;

using PN_HDSWeb_Admin.Data;
using Microsoft.Extensions.Logging;
using System.Data;
using PN_HDSWeb_Admin.Data.Model;
namespace PN_HDSWeb_Admin.Authentication
{
    public interface IUserAccountService
    {
        Task<ThongTinTruongV2?> GetThongTinTruong(string maTruongBo);
        Task<ThongTinNguoiDung?> GetThongTinNguoiDung(string userId);
    }

    public class UserAccountService : IUserAccountService
    {
        private readonly string _loginID_Index;
        private readonly string _loginID_TruongData;
        private readonly ILogger<UserAccountService> _logger;

        // ✅ BẮT BUỘC INJECT LOGGER
        public UserAccountService(ILogger<UserAccountService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(PN_LoginService.LoginID_Index))
                throw new InvalidOperationException("LoginID_Index chưa được khởi tạo");

            if (string.IsNullOrEmpty(PN_LoginService.LoginID_School_Dev))
                throw new InvalidOperationException("LoginID_School_Dev chưa được khởi tạo");

            _loginID_Index = PN_LoginService.LoginID_Index;
            _loginID_TruongData = PN_LoginService.LoginID_School_Dev;
        }

        #region Get Thông tin Trường

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

                _logger.LogInformation("Querying school info for {MaTruongBo}", safeMaTruong);

                DataTable dt = await hdataLib.hgetDataTableAsync(_loginID_Index, query);

                if (dt.Rows.Count == 0)
                {
                    _logger.LogWarning("No school found with ID: {MaTruongBo}", maTruongBo);
                    return null;
                }

                DataRow row = dt.Rows[0];
                return new ThongTinTruongV2
                {
                    MaTruongBo = row["ma_truong_bo"]?.ToString(),
                    TenTruong = row["tentruong"]?.ToString(),
                    Cap = ParsePostgresArray(row["caphoc"]),
                    TrangThai = row["trangthai"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(row["trangthai"])
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting school info for {MaTruongBo}", maTruongBo);
                throw;
            }
        }
        private static string[] ParsePostgresArray(object value)
        {
            if (value == null || value == DBNull.Value)
                return Array.Empty<string>();

            // Nếu driver trả về array thật
            if (value is string[] arr)
                return arr;

            var raw = value.ToString();

            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            // Nếu có dạng {02}
            if (raw.StartsWith("{") && raw.EndsWith("}"))
            {
                return raw
                    .Trim('{', '}')
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();
            }

            // 👉 CASE QUAN TRỌNG: chỉ có "02"
            return new[] { raw };
        }
        #endregion

        #region Get Thông tin Người dùng

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

                _logger.LogInformation("Querying user info for {UserId}", safeUser);

                DataTable dt = await hdataLib.hgetDataTableAsync(_loginID_TruongData, query);

                if (dt.Rows.Count == 0)
                {
                    _logger.LogWarning("No user found with ID: {UserId}", userId);
                    return null;
                }

                DataRow row = dt.Rows[0];

                return new ThongTinNguoiDung
                {
                    MaSo = row["ma_so"]?.ToString(),
                    HoTen = row["ho_ten"]?.ToString(),
                    MaTruongBo = row["ma_truong_bo"]?.ToString(),
                    MaChucVu = row["ma_chuc_vu"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(row["ma_chuc_vu"])
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info for {UserId}", userId);
                throw;
            }
        }

        #endregion
    }
}
