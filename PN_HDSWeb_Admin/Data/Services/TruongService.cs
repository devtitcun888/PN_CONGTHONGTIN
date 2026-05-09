using DocumentFormat.OpenXml.Spreadsheet;
using hDataLibraryN8;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PN_HDSWeb_Admin.Data.Model;
using PN_HDSWeb_Library;
using Serilog;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;


namespace PN_HDSWeb_Admin.Data.Services
{
    public class TruongService
    {
        private static readonly string LoginID_Index = PN_LoginService.LoginID_Index;
        private static readonly string LoginID_School_Dev = PN_LoginService.LoginID_School_Dev;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;

        #region TÀI KHOẢN & PHÂN QUYỀN
        //Get Features of School


        public async Task<LoginViewModel> GenerateToken(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var login = new LoginViewModel()
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.UserName,
                DisplayName = user.DisplayName,
                RoleName = roles != null && roles.Count > 0 ? roles[0] : null,
            };
            login = await MapToLoginModel(user, login);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(3);
            var token = new JwtSecurityToken(
                    _config["Tokens:Issuer"],
                    expires: expires,
                    signingCredentials: creds
                );
            login.Expires = expires;
            login.Token = new JwtSecurityTokenHandler().WriteToken(token);
            return login;
        }

        //Hàm check và trả về loginmodel
        private async Task<LoginViewModel> MapToLoginModel(AppUser user, LoginViewModel login)
        {
            if (login.RoleName == "ADMIN")
            {
                login.RoleName = "ADMIN";
            }
            return login;
        }

        //getaccount
        public static async Task<List<UserAccountData>> GetAccountDataAsync()
        {
            List<UserAccountData> users = new List<UserAccountData>();
            string sql = "SELECT username,password,ma_truong_bo,role,device_name FROM l_user";
            try
            {

                DataTable dataTable = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
                foreach (DataRow row in dataTable.Rows)
                {
                    UserAccountData userAccount = new UserAccountData
                    {
                        UserName = row["username"].ToString(),
                        Password = row["password"].ToString(),
                        MaTruongBo = row["ma_truong_bo"].ToString(),
                        Roles = row["role"].ToString(),
                        DeviceName = row["device_name"].ToString()
                    };
                    users.Add(userAccount);
                }

            }
            catch (Exception ex)
            {
                string error_mess = ex.Message;
                Log.Error(error_mess);
                throw new Exception(error_mess);
            }
            return users;
        }
        #endregion

        #region XỬ LÝ SESSION
        public static async Task hSaveSessionAsync(string token, string userId, string ma_truong_bo)
        {
            try
            {
                string sql = $@"
                SELECT *
                FROM l_ssosession
                WHERE matruongbo = '{ma_truong_bo}' and user_id = '{userId}'
                LIMIT 1;
                ";

                DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);

                Dictionary<string, hTbInfo> sessionData = new Dictionary<string, hTbInfo>();
                DateTime nowVN = NowVN();
                if (dt.Rows.Count > 0)
                {
                    // ===== UPDATE SESSION =====
                    DataRow row = dt.Rows[0];

                    sessionData.Add("session_id",
                        new hTbInfo(hKieuDL.Text, row["session_id"].ToString(), true, false, false));

                    sessionData.Add("start_time",
                        new hTbInfo(hKieuDL.Datetime, nowVN, false, false, false));


                    sessionData.Add("end_time",
                        new hTbInfo(hKieuDL.Datetime, nowVN.AddMinutes(60), false, false, false));

                    sessionData.Add("trang_thai",
                        new hTbInfo(hKieuDL.Boolean, true, false, false, false));

                    sessionData.Add("user_id",
                        new hTbInfo(hKieuDL.Text, userId, false, false, false));

                    sessionData.Add("token",
                        new hTbInfo(hKieuDL.Text, token, false, false, false));

                    sessionData.Add("matruongbo",
                        new hTbInfo(hKieuDL.Text, ma_truong_bo, false, false, false));
                }
                else
                {
                    // ===== INSERT SESSION =====
                    sessionData.Add("session_id",
                        new hTbInfo(hKieuDL.Text, hdataLib.hgetCodeTime_14(LoginID_Index), true, false, true));

                    sessionData.Add("start_time",
                        new hTbInfo(hKieuDL.Datetime, nowVN, false, false, false));

                    sessionData.Add("end_time",
                        new hTbInfo(hKieuDL.Datetime, nowVN.AddMinutes(60), false, false, false));

                    sessionData.Add("trang_thai",
                        new hTbInfo(hKieuDL.Boolean, true, false, false, false));

                    sessionData.Add("user_id",
                        new hTbInfo(hKieuDL.Text, userId, false, false, false));

                    sessionData.Add("token",
                        new hTbInfo(hKieuDL.Text, token, false, false, false));

                    sessionData.Add("matruongbo",
                        new hTbInfo(hKieuDL.Text, ma_truong_bo, false, false, false));
                }

                string jsonData = JsonConvert.SerializeObject(sessionData, Formatting.Indented);

                hdataLib.hsaveData(LoginID_Index, "l_ssosession", jsonData);
            }
            catch (Exception ex)
            {
                Log.Error($"hSaveSessionAsync error: {ex}");
                throw;
            }
        }

        public static DateTime NowVN()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
            );
        }

        public static async Task<SsoSessionTimeDTO> hGetSsoSessionTime(
            string ma_truong_bo,
            string user_id)
        {
            if (user_id == "")
            {
                user_id = ma_truong_bo;
            }

            string query = $@"
            SELECT start_time, end_time
            FROM public.l_ssosession
            WHERE user_id = '{user_id}'
              AND matruongbo = '{ma_truong_bo}'
            ORDER BY session_id ASC
            LIMIT 1;
            ";

            try
            {
                DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, query);

                if (dt.Rows.Count == 0)
                    return null;

                DataRow row = dt.Rows[0];

                return new SsoSessionTimeDTO
                {
                    Start_Time = Convert.ToDateTime(row["start_time"]),
                    End_Time = Convert.ToDateTime(row["end_time"])
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "hGetSsoSessionTime error");
                throw;
            }
        }
        #endregion

        #region THÔNG TIN TRƯỜNG - KHỐI - LỚP
        //Get danh sách trường
        public static async Task<List<ThongTinTruongV2>> hGetDanhSachTruong()
        {

            TruongHocData data_ = new TruongHocData();
            string query = $"select ma_truong_bo, tentruong, id_truong_so, caphoc, phuongxa from l_truong";

            try
            {
                DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_School_Dev, query);

                foreach (DataRow row in dt.Rows)
                {
                    data_.danhSachTruongList.Add(new ThongTinTruongV2
                    {
                        MaTruongBo = row["ma_truong_bo"]?.ToString(),
                        TenTruong = row["tentruong"]?.ToString(),
                        Cap = row["caphoc"] == DBNull.Value
                        ? Array.Empty<string>()
                        : (string[])row["caphoc"],
                        IdTruongSo = row["id_truong_so"] != DBNull.Value
                        ? Convert.ToInt32(row["id_truong_so"])
                        : 0,
                        PhuongXa = row["phuongxa"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                throw;
            }

            return data_.danhSachTruongList;
        }

        //Get thông tin trường
        public static async Task<ThongTinTruongV2?> hThongTinTruongByID(string ma_truong_bo)
        {
            TruongHocData data_ = new();
            string query = $@"
            select ma_truong_bo, tentruong, caphoc ,id_truong_so,
            hieutruong ->>'ho_ten' as hoten_hieutruong ,hieutruong ->>'so_dien_thoai' as sodienthoai_hieutruong, diachi_truong, logo_truong
            from l_truong  
            where ma_truong_bo = '{ma_truong_bo}'
            ";

            try
            {
                DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, query);

                foreach (DataRow row in dt.Rows)
                {
                    data_.danhSachTruongList.Add(new ThongTinTruongV2
                    {
                        MaTruongBo = row["ma_truong_bo"]?.ToString(),
                        TenTruong = row["tentruong"]?.ToString(),
                        Cap = row["caphoc"] == DBNull.Value
                            ? Array.Empty<string>()
                            : (string[])row["caphoc"],
                        IdTruongSo = row["id_truong_so"] != DBNull.Value
                        ? Convert.ToInt32(row["id_truong_so"])
                        : 0,
                        HoTenHieuTruong = row["hoten_hieutruong"]?.ToString(),
                        SoDienThoaiHieuTruong = row["sodienthoai_hieutruong"]?.ToString(),
                        DiaChiTruong = row["diachi_truong"]?.ToString(),
                        LogoTruong = row["logo_truong"]?.ToString(),
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "hThongTinTruongByID error");
                throw;
            }

            return data_.danhSachTruongList.FirstOrDefault();
        }

        // Update thông tin trường
        public static async Task<bool> hUpsertThongTinTruong(ThongTinTruongV2 model)
        {
            // ===== Escape tránh lỗi SQL =====
            string maTruongBo = model.MaTruongBo?.Replace("'", "''");
            string tenTruong = model.TenTruong?.Replace("'", "''");
            string diaChi = model.DiaChiTruong?.Replace("'", "''");
            string logo = model.LogoTruong?.Replace("'", "''");
            string hoTen = model.HoTenHieuTruong?.Replace("'", "''");
            string sdt = model.SoDienThoaiHieuTruong?.Replace("'", "''");

            // ===== caphoc (chỉ dùng khi INSERT) =====
            string capHocSql = model.Cap != null && model.Cap.Length > 0
                ? $"ARRAY[{string.Join(",", model.Cap.Select(x => $"'{x.Replace("'", "''")}'"))}]"
                : "NULL";

            // ===== JSON hiệu trưởng =====
            string hieuTruongJson = $@"
            jsonb_build_object(
                'ho_ten', '{hoTen}',
                'so_dien_thoai', '{sdt}'
            )";

            // ===== IdTruongSo là int → KHÔNG dùng HasValue =====
            string idTruongSoSql = model.IdTruongSo.ToString();

            string sql = $@"
            INSERT INTO l_truong (
                ma_truong_bo,
                tentruong,
                caphoc,
                id_truong_so,
                hieutruong,
                diachi_truong,
                logo_truong
            )
            VALUES (
                '{maTruongBo}',
                '{tenTruong}',
                {capHocSql},
                {idTruongSoSql},
                {hieuTruongJson},
                '{diaChi}',
                '{logo}'
            )
            ON CONFLICT (ma_truong_bo)
            DO UPDATE SET
                tentruong = EXCLUDED.tentruong,
                hieutruong = EXCLUDED.hieutruong,
                diachi_truong = EXCLUDED.diachi_truong,
                logo_truong = EXCLUDED.logo_truong;
            ";

            try
            {
                await hdataLib.hrunQueryAsync(LoginID_Index, sql);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "hUpsertThongTinTruong error");
                throw;
            }
        }
        //Get thống kê trường
        public static async Task<ThongKeThongTinTruongHoc?> hGetThongKeTruong(
        string ma_truong_bo,
        string cap_hoc)
        {
            string query = $@"
            SELECT
            (
                SELECT COUNT(DISTINCT k.ma_khoi_bo)
                FROM l_khoi k
                WHERE k.ma_truong_bo = '{ma_truong_bo}'
                  AND k.caphoc = '{cap_hoc}'
            ) AS so_luong_khoi,

            (
                SELECT COUNT(DISTINCT l.ma_lop)
                FROM l_lophoc l
                JOIN l_khoi k
                    ON l.ma_khoi_bo = k.ma_khoi_bo
                WHERE l.ma_truong_bo = '{ma_truong_bo}'
                  AND l.nam_hoc = '2025-2026'
                  AND k.caphoc = '{cap_hoc}'
            ) AS so_luong_lop,

            (
                SELECT COUNT(DISTINCT hs.ma_hs_so)
                FROM l_hocsinh hs
                JOIN l_khoi k
                    ON LPAD(hs.ma_khoi::text, 2, '0') = k.ma_khoi_bo
                WHERE hs.ma_truong_bo = '{ma_truong_bo}'
                  AND k.caphoc = '{cap_hoc}'
            ) AS so_luong_hoc_sinh,

            (
                SELECT COUNT(DISTINCT gv.ma_so)
                FROM l_giaovien gv
                WHERE gv.ma_truong_bo = '{ma_truong_bo}'
                  AND gv.nam_hoc = '2025-2026'
            ) AS so_luong_giao_vien;
              ";

            try
            {
                DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_School_Dev, query);

                if (dt.Rows.Count == 0)
                    return null;

                DataRow row = dt.Rows[0];

                return new ThongKeThongTinTruongHoc
                {
                    SoLuongKhoi = row["so_luong_khoi"] != DBNull.Value
                        ? Convert.ToInt32(row["so_luong_khoi"])
                        : 0,

                    SoLuongLop = row["so_luong_lop"] != DBNull.Value
                        ? Convert.ToInt32(row["so_luong_lop"])
                        : 0,

                    SoLuongHocSinh = row["so_luong_hoc_sinh"] != DBNull.Value
                        ? Convert.ToInt32(row["so_luong_hoc_sinh"])
                        : 0,

                    SoLuongGiaoVien = row["so_luong_giao_vien"] != DBNull.Value
                        ? Convert.ToInt32(row["so_luong_giao_vien"])
                        : 0
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "hGetThongKeTruong error");
                throw;
            }
        }

        //Get thông tin khối
        public static async Task<List<KhoiHoc_DTO>> hGetDanhSachKhoi(string matruongbo, string caphoc)
        {

            TruongHocData data_ = new TruongHocData();
            string query = $"select ma_khoi, ma_khoi_bo, ten_khoi,caphoc from l_khoi where ma_truong_bo ='{matruongbo}' and caphoc  = '{caphoc}'";

            try
            {
                DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, query);

                foreach (DataRow row in dt.Rows)
                {
                    data_.danhSachKhoiList.Add(new KhoiHoc_DTO
                    {
                        MaKhoi = row["ma_khoi"] != DBNull.Value ? Convert.ToInt32(row["ma_khoi"]) : 0,
                        MaKhoiBo = row["ma_khoi_bo"]?.ToString(),
                        TenKhoi = row["ten_khoi"]?.ToString(),
                        Cap = row["caphoc"]?.ToString(),

                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                throw;
            }

            return data_.danhSachKhoiList;
        }

        //Get danh sách lớp
        public static async Task<List<LopHoc_DTO>> hGetDanhSachLop(
        string matruongbo,
        string caphoc,
        string namhoc)
        {
            TruongHocData data_ = new TruongHocData();

            string query = $@"
            SELECT
                l.id_lop_bo,
                l.ma_lop_bo,
                l.ma_khoi_bo,
                l.ten_lop,
                l.buoi_hoc,
                l.ma_khoi
            FROM l_lophoc l
            WHERE l.ma_truong_bo = '{matruongbo}'
              AND l.nam_hoc = '{namhoc}'
              AND EXISTS (
                  SELECT 1
                  FROM l_khoi k
                  WHERE k.caphoc = '{caphoc}'
                    AND k.ma_khoi_bo = LPAD(l.ma_khoi_bo::text, 2, '0')
              )
            ORDER BY l.ten_lop
             ";

            try
            {
                DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, query);

                foreach (DataRow row in dt.Rows)
                {
                    data_.danhSachLopList.Add(new LopHoc_DTO
                    {
                        IdLopBo = row["id_lop_bo"] != DBNull.Value ? Convert.ToInt32(row["id_lop_bo"]) : 0,
                        MaKhoiBo = row["ma_khoi_bo"]?.ToString(),
                        TenLop = row["ten_lop"]?.ToString(),
                        MaLopBo = row["ma_lop_bo"]?.ToString(),
                        BuoiHoc = row["buoi_hoc"]?.ToString(),
                        MaKhoi = row["ma_khoi"] != DBNull.Value ? Convert.ToInt32(row["ma_khoi"]) : 0,
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi hGetDanhSachLop");
                throw;
            }

            return data_.danhSachLopList;
        }

        public static async Task<DanhSachLopTheoBuoi_DTO> hGetDanhSachMaLopTheoBuoi(
        string matruongbo,
        string caphoc,
        string namhoc)
        {
            var result = new DanhSachLopTheoBuoi_DTO();

            try
            {
                List<LopHoc_DTO> danhSachLop = await hGetDanhSachLop(matruongbo, caphoc, namhoc);

                foreach (var lop in danhSachLop)
                {
                    if (string.IsNullOrWhiteSpace(lop.MaLopBo))
                        continue;

                    int? buoiHoc = int.TryParse(lop.BuoiHoc, out var v) ? v : null;

                    if (buoiHoc == null || buoiHoc == 0)
                    {
                        result.LopSang.Add(lop.MaLopBo);
                        result.LopChieu.Add(lop.MaLopBo);
                    }
                    else if (buoiHoc == 1)
                    {
                        result.LopSang.Add(lop.MaLopBo);
                    }
                    else if (buoiHoc == 2)
                    {
                        result.LopChieu.Add(lop.MaLopBo);
                    }
                    else
                    {
                        result.LopSang.Add(lop.MaLopBo);
                        result.LopChieu.Add(lop.MaLopBo);
                    }
                }

                result.LopSang = result.LopSang
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                result.LopChieu = result.LopChieu
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi hGetDanhSachMaLopTheoBuoi");
                throw;
            }

            return result;
        }
        #endregion


    }
}