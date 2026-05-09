using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PN_HDSWeb_Admin.Data.Model;
using PN_HDSWeb_Library;
using Serilog;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Schools;

public interface ISchoolService
{
    Task<List<UserAccountData_>> GetAccountDataAsync();
    Task hSaveSessionAsync(string token, string userId, string ma_truong_bo);
    Task<SsoSessionTimeDTO?> hGetSsoSessionTime(string ma_truong_bo, string user_id);
    Task<ThongTinTruongV2?> hThongTinTruongByID(string ma_truong_bo);
    Task<bool> hUpsertThongTinTruong(ThongTinTruongV2 model);
}

public class SchoolService : ISchoolService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;

    public async Task<List<UserAccountData_>> GetAccountDataAsync()
    {
        var users = new List<UserAccountData_>();
        const string sql = @"
            SELECT id, username, password_hash, ma_truong_bo, role_code, device_name, auth_type, sso_user_id, sso_username
            FROM l_user_account
            WHERE is_deleted = false";

        try
        {
            DataTable dataTable = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            foreach (DataRow row in dataTable.Rows)
            {
                users.Add(new UserAccountData_
                {
                    Id = row["id"]?.ToString(),
                    UserName = row["username"]?.ToString(),
                    Password = row["password_hash"]?.ToString(),
                    MaTruongBo = row["ma_truong_bo"]?.ToString(),
                    Roles = row["role_code"]?.ToString(),
                    DeviceName = row["device_name"]?.ToString(),
                    AuthType = row["auth_type"]?.ToString(),
                    SsoUserId = row["sso_user_id"]?.ToString(),
                    SsoUserName = row["sso_username"]?.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetAccountDataAsync error");
            throw;
        }

        return users;
    }

    public async Task hSaveSessionAsync(string token, string userId, string ma_truong_bo)
    {
        try
        {
            string sql = $@"
                SELECT *
                FROM l_ssosession
                WHERE matruongbo = '{ma_truong_bo}' and user_id = '{userId}'
                LIMIT 1;";

            DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            Dictionary<string, hTbInfo> sessionData = new();
            DateTime nowVN = NowVN();

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                sessionData.Add("session_id", new hTbInfo(hKieuDL.Text, row["session_id"].ToString(), true, false, false));
                sessionData.Add("start_time", new hTbInfo(hKieuDL.Datetime, nowVN, false, false, false));
                sessionData.Add("end_time", new hTbInfo(hKieuDL.Datetime, nowVN.AddMinutes(60), false, false, false));
                sessionData.Add("trang_thai", new hTbInfo(hKieuDL.Boolean, true, false, false, false));
                sessionData.Add("user_id", new hTbInfo(hKieuDL.Text, userId, false, false, false));
                sessionData.Add("token", new hTbInfo(hKieuDL.Text, token, false, false, false));
                sessionData.Add("matruongbo", new hTbInfo(hKieuDL.Text, ma_truong_bo, false, false, false));
            }
            else
            {
                sessionData.Add("session_id", new hTbInfo(hKieuDL.Text, hdataLib.hgetCodeTime_14(LoginID_Index), true, false, true));
                sessionData.Add("start_time", new hTbInfo(hKieuDL.Datetime, nowVN, false, false, false));
                sessionData.Add("end_time", new hTbInfo(hKieuDL.Datetime, nowVN.AddMinutes(60), false, false, false));
                sessionData.Add("trang_thai", new hTbInfo(hKieuDL.Boolean, true, false, false, false));
                sessionData.Add("user_id", new hTbInfo(hKieuDL.Text, userId, false, false, false));
                sessionData.Add("token", new hTbInfo(hKieuDL.Text, token, false, false, false));
                sessionData.Add("matruongbo", new hTbInfo(hKieuDL.Text, ma_truong_bo, false, false, false));
            }

            string jsonData = JsonConvert.SerializeObject(sessionData, Formatting.Indented);
            hdataLib.hsaveData(LoginID_Index, "l_ssosession", jsonData);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "hSaveSessionAsync error");
            throw;
        }
    }

    public static DateTime NowVN()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
    }

    public async Task<SsoSessionTimeDTO?> hGetSsoSessionTime(string ma_truong_bo, string user_id)
    {
        if (user_id == "")
            user_id = ma_truong_bo;

        string query = $@"
            SELECT start_time, end_time
            FROM public.l_ssosession
            WHERE user_id = '{user_id}'
              AND matruongbo = '{ma_truong_bo}'
            ORDER BY session_id ASC
            LIMIT 1;";

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

    public async Task<ThongTinTruongV2?> hThongTinTruongByID(string ma_truong_bo)
    {
        string query = $@"
            select ma_truong_bo, tentruong, cap ,id_truong_so,
            phuongxa
      
            from l_truong  
            where ma_truong_bo = '{ma_truong_bo}'";

        try
        {
            DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, query);
            if (dt.Rows.Count == 0)
                return null;

            DataRow row = dt.Rows[0];
            return new ThongTinTruongV2
            {
                MaTruongBo = row["ma_truong_bo"]?.ToString(),
                TenTruong = row["tentruong"]?.ToString(),
                Cap = row["cap"] == DBNull.Value ? Array.Empty<string>() : (string[])row["cap"],
                IdTruongSo = row["id_truong_so"] != DBNull.Value ? Convert.ToInt32(row["id_truong_so"]) : 0,
                DiaChiTruong = row["phuongxa"]?.ToString(),
                
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "hThongTinTruongByID error");
            throw;
        }
    }

    public async Task<bool> hUpsertThongTinTruong(ThongTinTruongV2 model)
    {
        string maTruongBo = model.MaTruongBo?.Replace("'", "''");
        string tenTruong = model.TenTruong?.Replace("'", "''");
        string diaChi = model.DiaChiTruong?.Replace("'", "''");
        string logo = model.LogoTruong?.Replace("'", "''");
        string hoTen = model.HoTenHieuTruong?.Replace("'", "''");
        string sdt = model.SoDienThoaiHieuTruong?.Replace("'", "''");

        string capHocSql = model.Cap != null && model.Cap.Length > 0
            ? $"ARRAY[{string.Join(",", model.Cap.Select(x => $"'{x.Replace("'", "''")}'"))}]"
            : "NULL";

        string hieuTruongJson = $@"
            jsonb_build_object(
                'ho_ten', '{hoTen}',
                'so_dien_thoai', '{sdt}'
            )";

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
                logo_truong = EXCLUDED.logo_truong;";

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
}
