using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;
using System.Text.Json;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminVehicleService
{
    Task<(List<AdminVehicleItem> Items, int Total)> GetAllAsync(AdminVehicleFilter filter);
    Task<AdminVehicleDetail?> GetByIdAsync(int id);
    Task<SaveVehicleResult> CreateAsync(AdminVehicleDto dto);
    Task<SaveVehicleResult> UpdateAsync(int id, AdminVehicleDto dto);
    Task<bool> UpdateStatusAsync(int id, string tinhTrang);
    Task<bool> UpdatePinAsync(int id, int pinPhanTram);
    Task<bool> DeleteAsync(int id);
}

public class AdminVehicleService : IAdminVehicleService
{
    private static readonly string LoginID = PN_LoginService.LoginID_XeDien;
    private readonly ILogger<AdminVehicleService> _logger;

    public AdminVehicleService(ILogger<AdminVehicleService> logger)
    {
        _logger = logger;
    }

    public async Task<(List<AdminVehicleItem> Items, int Total)> GetAllAsync(AdminVehicleFilter filter)
    {
        var list = new List<AdminVehicleItem>();

        var loaiWhere = string.IsNullOrWhiteSpace(filter.LoaiXe) ? "" : $"AND loai_xe = '{Escape(filter.LoaiXe)}'";
        var ttWhere = string.IsNullOrWhiteSpace(filter.TinhTrang) ? "" : $"AND tinh_trang = '{Escape(filter.TinhTrang)}'";
        var searchWhere = string.IsNullOrWhiteSpace(filter.Search)
            ? ""
            : $"AND (LOWER(ten_xe) LIKE LOWER('%{Escape(filter.Search)}%') OR LOWER(bien_so) LIKE LOWER('%{Escape(filter.Search)}%'))";

        var offset = (filter.Page - 1) * filter.PageSize;

        var countSql = $@"
            SELECT COUNT(*) FROM ev_vehicles
            WHERE is_deleted = FALSE {loaiWhere} {ttWhere} {searchWhere}";

        var dataSql = $@"
            SELECT id, ten_xe, loai_xe, bien_so, hang_xe, mau_xe,
                   gia_thue_gio, gia_thue_ngay, dat_coc,
                   tinh_trang, pin_phan_tram, km_tong,
                   hinh_anh_chinh, dia_chi, is_active, created_at
            FROM ev_vehicles
            WHERE is_deleted = FALSE {loaiWhere} {ttWhere} {searchWhere}
            ORDER BY id DESC
            LIMIT {filter.PageSize} OFFSET {offset}";

        try
        {
            var countDt = await hdataLib.hgetDataTableAsync(LoginID, countSql);
            var total = countDt.Rows.Count > 0 ? Convert.ToInt32(countDt.Rows[0][0]) : 0;

            var dt = await hdataLib.hgetDataTableAsync(LoginID, dataSql);
            foreach (DataRow row in dt.Rows)
                list.Add(MapVehicleItem(row));

            return (list, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminVehicleService.GetAllAsync failed");
            return (list, 0);
        }
    }

    public async Task<AdminVehicleDetail?> GetByIdAsync(int id)
    {
        var sql = $@"
            SELECT id, ten_xe, loai_xe, bien_so, hang_xe, mau_xe, nam_san_xuat,
                   mo_ta, gia_thue_gio, gia_thue_ngay, dat_coc,
                   tinh_trang, pin_phan_tram, km_tong, km_hang_lan_sac,
                   hinh_anh_json, hinh_anh_chinh, dia_chi, vi_tri_lat, vi_tri_lng,
                   tinh_nang_json, is_active, created_at, updated_at
            FROM ev_vehicles WHERE id = {id} AND is_deleted = FALSE LIMIT 1";
        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];

            var detail = new AdminVehicleDetail
            {
                Id = Convert.ToInt32(row["id"]),
                TenXe = row["ten_xe"]?.ToString(),
                LoaiXe = row["loai_xe"]?.ToString(),
                BienSo = row["bien_so"]?.ToString(),
                HangXe = row["hang_xe"]?.ToString(),
                MauXe = row["mau_xe"]?.ToString(),
                NamSanXuat = row["nam_san_xuat"] == DBNull.Value ? null : Convert.ToInt32(row["nam_san_xuat"]),
                MoTa = row["mo_ta"]?.ToString(),
                GiaThueGio = ToDecimal(row["gia_thue_gio"]),
                GiaThueNgay = ToDecimal(row["gia_thue_ngay"]),
                DatCoc = ToDecimal(row["dat_coc"]),
                TinhTrang = row["tinh_trang"]?.ToString(),
                PinPhanTram = ToInt(row["pin_phan_tram"]),
                KmTong = ToInt(row["km_tong"]),
                KmHangLanSac = ToInt(row["km_hang_lan_sac"]),
                HinhAnhChinh = row["hinh_anh_chinh"]?.ToString(),
                HinhAnhJson = row["hinh_anh_json"]?.ToString(),
                DiaChi = row["dia_chi"]?.ToString(),
                ViTriLat = row["vi_tri_lat"] == DBNull.Value ? null : Convert.ToDecimal(row["vi_tri_lat"]),
                ViTriLng = row["vi_tri_lng"] == DBNull.Value ? null : Convert.ToDecimal(row["vi_tri_lng"]),
                TinhNangJson = row["tinh_nang_json"]?.ToString(),
                IsActive = row["is_active"] == DBNull.Value || Convert.ToBoolean(row["is_active"]),
                CreatedAt = row["created_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["created_at"]),
                UpdatedAt = row["updated_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["updated_at"])
            };
            return detail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminVehicleService.GetByIdAsync failed id={Id}", id);
            return null;
        }
    }

    public async Task<SaveVehicleResult> CreateAsync(AdminVehicleDto dto)
    {
        try
        {
            var sql = $@"
                INSERT INTO ev_vehicles
                    (ten_xe, loai_xe, bien_so, hang_xe, mau_xe, nam_san_xuat,
                     mo_ta, gia_thue_gio, gia_thue_ngay, dat_coc,
                     pin_phan_tram, km_hang_lan_sac, hinh_anh_chinh, hinh_anh_json,
                     dia_chi, tinh_nang_json, is_active)
                VALUES (
                    '{Escape(dto.TenXe)}', '{Escape(dto.LoaiXe)}',
                    {SqlStr(dto.BienSo)}, {SqlStr(dto.HangXe)}, {SqlStr(dto.MauXe)},
                    {(dto.NamSanXuat.HasValue ? dto.NamSanXuat.Value.ToString() : "NULL")},
                    {SqlStr(dto.MoTa)},
                    0, {dto.GiaThueNgay}, {dto.DatCoc},
                    {dto.PinPhanTram}, {dto.KmHangLanSac},
                    {SqlStr(dto.HinhAnhChinh)}, {SqlStr(dto.HinhAnhJson)},
                    {SqlStr(dto.DiaChi)}, {SqlStr(dto.TinhNangJson)}, TRUE
                )
                RETURNING id";

            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0)
                return SaveVehicleResult.Fail("Lỗi tạo xe.");

            return SaveVehicleResult.Ok(Convert.ToInt32(dt.Rows[0][0]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminVehicleService.CreateAsync failed");
            return SaveVehicleResult.Fail("Lỗi hệ thống.");
        }
    }

    public async Task<SaveVehicleResult> UpdateAsync(int id, AdminVehicleDto dto)
    {
        try
        {
            var sql = $@"
                UPDATE ev_vehicles SET
                    ten_xe = '{Escape(dto.TenXe)}',
                    loai_xe = '{Escape(dto.LoaiXe)}',
                    bien_so = {SqlStr(dto.BienSo)},
                    hang_xe = {SqlStr(dto.HangXe)},
                    mau_xe = {SqlStr(dto.MauXe)},
                    nam_san_xuat = {(dto.NamSanXuat.HasValue ? dto.NamSanXuat.Value.ToString() : "NULL")},
                    mo_ta = {SqlStr(dto.MoTa)},
                    gia_thue_gio = 0,
                    gia_thue_ngay = {dto.GiaThueNgay},
                    dat_coc = {dto.DatCoc},
                    pin_phan_tram = {dto.PinPhanTram},
                    km_hang_lan_sac = {dto.KmHangLanSac},
                    hinh_anh_chinh = {SqlStr(dto.HinhAnhChinh)},
                    hinh_anh_json = {SqlStr(dto.HinhAnhJson)},
                    dia_chi = {SqlStr(dto.DiaChi)},
                    tinh_nang_json = {SqlStr(dto.TinhNangJson)},
                    updated_at = NOW()
                WHERE id = {id} AND is_deleted = FALSE";

            await hdataLib.hrunQueryAsync(LoginID, sql);
            return SaveVehicleResult.Ok(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminVehicleService.UpdateAsync failed id={Id}", id);
            return SaveVehicleResult.Fail("Lỗi hệ thống.");
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, string tinhTrang)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID,
                $"UPDATE ev_vehicles SET tinh_trang = '{Escape(tinhTrang)}', updated_at = NOW() WHERE id = {id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateStatusAsync failed id={Id}", id);
            return false;
        }
    }

    public async Task<bool> UpdatePinAsync(int id, int pinPhanTram)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID,
                $"UPDATE ev_vehicles SET pin_phan_tram = {pinPhanTram}, updated_at = NOW() WHERE id = {id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdatePinAsync failed id={Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID,
                $"UPDATE ev_vehicles SET is_deleted = TRUE, updated_at = NOW() WHERE id = {id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAsync failed id={Id}", id);
            return false;
        }
    }

    private static AdminVehicleItem MapVehicleItem(DataRow row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        TenXe = row["ten_xe"]?.ToString(),
        LoaiXe = row["loai_xe"]?.ToString(),
        BienSo = row["bien_so"]?.ToString(),
        HangXe = row["hang_xe"]?.ToString(),
        MauXe = row["mau_xe"]?.ToString(),
        GiaThueGio = ToDecimal(row["gia_thue_gio"]),
        GiaThueNgay = ToDecimal(row["gia_thue_ngay"]),
        DatCoc = ToDecimal(row["dat_coc"]),
        TinhTrang = row["tinh_trang"]?.ToString(),
        PinPhanTram = ToInt(row["pin_phan_tram"]),
        KmTong = ToInt(row["km_tong"]),
        HinhAnhChinh = row["hinh_anh_chinh"]?.ToString(),
        DiaChi = row["dia_chi"]?.ToString(),
        IsActive = row["is_active"] == DBNull.Value || Convert.ToBoolean(row["is_active"]),
        CreatedAt = row["created_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["created_at"])
    };

    private static decimal ToDecimal(object v) => v == DBNull.Value || v == null ? 0 : Convert.ToDecimal(v);
    private static int ToInt(object v) => v == DBNull.Value || v == null ? 0 : Convert.ToInt32(v);
    private static string Escape(string? v) => string.IsNullOrWhiteSpace(v) ? "" : v.Replace("'", "''");
    private static string SqlStr(string? v) => string.IsNullOrWhiteSpace(v) ? "NULL" : $"'{Escape(v)}'";
}

// ===== Models & DTOs =====

public class AdminVehicleFilter
{
    public string? LoaiXe { get; set; }
    public string? TinhTrang { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminVehicleItem
{
    public int Id { get; set; }
    public string? TenXe { get; set; }
    public string? LoaiXe { get; set; }
    public string? BienSo { get; set; }
    public string? HangXe { get; set; }
    public string? MauXe { get; set; }
    public decimal GiaThueGio { get; set; }
    public decimal GiaThueNgay { get; set; }
    public decimal DatCoc { get; set; }
    public string? TinhTrang { get; set; }
    public int PinPhanTram { get; set; }
    public int KmTong { get; set; }
    public string? HinhAnhChinh { get; set; }
    public string? DiaChi { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TinhTrangLabel => TinhTrang switch
    {
        "available"   => "Sẵn sàng",
        "rented"      => "Đang thuê",
        "maintenance" => "Bảo dưỡng",
        "inactive"    => "Ngừng hoạt động",
        _ => TinhTrang ?? ""
    };

    public string TinhTrangColor => TinhTrang switch
    {
        "available"   => "success",
        "rented"      => "warning",
        "maintenance" => "info",
        "inactive"    => "secondary",
        _ => "secondary"
    };
}

public class AdminVehicleDetail : AdminVehicleItem
{
    public int? NamSanXuat { get; set; }
    public string? MoTa { get; set; }
    public int KmHangLanSac { get; set; }
    public string? HinhAnhJson { get; set; }
    public decimal? ViTriLat { get; set; }
    public decimal? ViTriLng { get; set; }
    public string? TinhNangJson { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdminVehicleDto
{
    public string TenXe { get; set; } = "";
    public string LoaiXe { get; set; } = "scooter";
    public string? BienSo { get; set; }
    public string? HangXe { get; set; }
    public string? MauXe { get; set; }
    public int? NamSanXuat { get; set; }
    public string? MoTa { get; set; }
    public decimal GiaThueGio { get; set; }
    public decimal GiaThueNgay { get; set; }
    public decimal DatCoc { get; set; }
    public int PinPhanTram { get; set; } = 100;
    public int KmHangLanSac { get; set; }
    public string? HinhAnhChinh { get; set; }
    public string? HinhAnhJson { get; set; }
    public string? DiaChi { get; set; }
    public string? TinhNangJson { get; set; }
}

public class SaveVehicleResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int? VehicleId { get; set; }
    public static SaveVehicleResult Ok(int id) => new() { IsSuccess = true, VehicleId = id };
    public static SaveVehicleResult Fail(string msg) => new() { IsSuccess = false, Message = msg };
}
