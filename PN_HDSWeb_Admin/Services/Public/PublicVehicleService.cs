using hDataLibraryN8;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;
using System.Text.Json;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicVehicleService
{
    Task<List<PublicVehicleItem>> GetAvailableVehiclesAsync(VehicleFilter filter);
    Task<PublicVehicleDetail?> GetVehicleDetailAsync(int vehicleId);
    Task<bool> CheckAvailabilityAsync(int vehicleId, DateTime start, DateTime end, int? excludeRentalId = null);
}

public class PublicVehicleService : IPublicVehicleService
{
    private static readonly string LoginID = PN_LoginService.LoginID_XeDien;
    private readonly ILogger<PublicVehicleService> _logger;
    private readonly IMemoryCache _cache;

    public PublicVehicleService(ILogger<PublicVehicleService> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<PublicVehicleItem>> GetAvailableVehiclesAsync(VehicleFilter filter)
    {
        var list = new List<PublicVehicleItem>();

        var loaiWhere = string.IsNullOrWhiteSpace(filter.LoaiXe) ? "" : $"AND loai_xe = '{Escape(filter.LoaiXe)}'";
        var giaWhere = filter.GiaMax.HasValue
            ? $"AND gia_thue_ngay <= {filter.GiaMax.Value}"
            : "";

        // Loại trừ xe đang có đơn active trong khoảng thời gian yêu cầu
        var thoiGianWhere = "";
        if (filter.BatDauThue.HasValue && filter.KetThucThue.HasValue)
        {
            var start = filter.BatDauThue.Value.ToString("yyyy-MM-dd HH:mm:ss");
            var end = filter.KetThucThue.Value.ToString("yyyy-MM-dd HH:mm:ss");
            thoiGianWhere = $@"AND v.id NOT IN (
                SELECT DISTINCT vehicle_id FROM ev_rentals
                WHERE trang_thai IN ('confirmed','active')
                  AND bat_dau_thue < '{end}'
                  AND ket_thuc_thue > '{start}')";
        }

        var sql = $@"
            SELECT v.id, v.ten_xe, v.loai_xe, v.bien_so, v.hang_xe, v.mau_xe,
                   v.gia_thue_gio, v.gia_thue_ngay, v.dat_coc,
                   v.tinh_trang, v.pin_phan_tram, v.km_hang_lan_sac,
                   v.hinh_anh_chinh, v.dia_chi, v.tinh_nang_json
            FROM ev_vehicles v
            WHERE v.is_active = TRUE
              AND v.is_deleted = FALSE
              AND v.tinh_trang = 'available'
              {loaiWhere}
              {giaWhere}
              {thoiGianWhere}
            ORDER BY v.id ASC
            LIMIT 50";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            foreach (DataRow row in dt.Rows)
                list.Add(MapVehicleItem(row));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAvailableVehiclesAsync failed");
        }

        return list;
    }

    public async Task<PublicVehicleDetail?> GetVehicleDetailAsync(int vehicleId)
    {
        string cacheKey = $"VehicleDetail_{vehicleId}";
        if (_cache.TryGetValue(cacheKey, out PublicVehicleDetail? cached) && cached != null)
            return cached;

        var sql = $@"
            SELECT id, ten_xe, loai_xe, bien_so, hang_xe, mau_xe, nam_san_xuat,
                   mo_ta, gia_thue_gio, gia_thue_ngay, dat_coc,
                   tinh_trang, pin_phan_tram, km_hang_lan_sac, km_tong,
                   hinh_anh_json, hinh_anh_chinh, dia_chi,
                   vi_tri_lat, vi_tri_lng, tinh_nang_json
            FROM ev_vehicles
            WHERE id = {vehicleId}
              AND is_deleted = FALSE
            LIMIT 1";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            var detail = new PublicVehicleDetail
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
                PinPhanTram = row["pin_phan_tram"] == DBNull.Value ? 0 : Convert.ToInt32(row["pin_phan_tram"]),
                KmHangLanSac = row["km_hang_lan_sac"] == DBNull.Value ? 0 : Convert.ToInt32(row["km_hang_lan_sac"]),
                HinhAnhChinh = row["hinh_anh_chinh"]?.ToString(),
                DiaChi = row["dia_chi"]?.ToString(),
                ViTriLat = row["vi_tri_lat"] == DBNull.Value ? null : Convert.ToDecimal(row["vi_tri_lat"]),
                ViTriLng = row["vi_tri_lng"] == DBNull.Value ? null : Convert.ToDecimal(row["vi_tri_lng"])
            };

            // Parse ảnh
            var hinhAnhJson = row["hinh_anh_json"]?.ToString();
            if (!string.IsNullOrWhiteSpace(hinhAnhJson))
            {
                try
                {
                    detail.HinhAnhList = JsonSerializer.Deserialize<List<string>>(hinhAnhJson) ?? [];
                }
                catch { }
            }
            if (detail.HinhAnhList.Count == 0 && !string.IsNullOrWhiteSpace(detail.HinhAnhChinh))
                detail.HinhAnhList.Add(detail.HinhAnhChinh);

            // Parse tính năng
            var tinhNangJson = row["tinh_nang_json"]?.ToString();
            if (!string.IsNullOrWhiteSpace(tinhNangJson))
            {
                try
                {
                    detail.TinhNang = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tinhNangJson) ?? [];
                }
                catch { }
            }

            _cache.Set(cacheKey, detail, TimeSpan.FromMinutes(5));
            return detail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetVehicleDetailAsync failed for id={Id}", vehicleId);
            return null;
        }
    }

    public async Task<bool> CheckAvailabilityAsync(int vehicleId, DateTime start, DateTime end, int? excludeRentalId = null)
    {
        var excludeWhere = excludeRentalId.HasValue ? $"AND id != {excludeRentalId.Value}" : "";
        var sql = $@"
            SELECT COUNT(*) FROM ev_rentals
            WHERE vehicle_id = {vehicleId}
              AND trang_thai IN ('confirmed','active')
              AND bat_dau_thue < '{end:yyyy-MM-dd HH:mm:ss}'
              AND ket_thuc_thue > '{start:yyyy-MM-dd HH:mm:ss}'
              {excludeWhere}";
        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0) return true;
            return Convert.ToInt64(dt.Rows[0][0]) == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckAvailabilityAsync failed");
            return false;
        }
    }

    private static PublicVehicleItem MapVehicleItem(DataRow row) => new()
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
        PinPhanTram = row["pin_phan_tram"] == DBNull.Value ? 0 : Convert.ToInt32(row["pin_phan_tram"]),
        KmHangLanSac = row["km_hang_lan_sac"] == DBNull.Value ? 0 : Convert.ToInt32(row["km_hang_lan_sac"]),
        HinhAnhChinh = row["hinh_anh_chinh"]?.ToString(),
        DiaChi = row["dia_chi"]?.ToString()
    };

    private static decimal ToDecimal(object val) =>
        val == DBNull.Value || val == null ? 0 : Convert.ToDecimal(val);

    private static string Escape(string? v) =>
        string.IsNullOrWhiteSpace(v) ? "" : v.Replace("'", "''");
}

// ===== Models =====

public class VehicleFilter
{
    public string? LoaiXe { get; set; }
    public decimal? GiaMax { get; set; }
    public DateTime? BatDauThue { get; set; }
    public DateTime? KetThucThue { get; set; }
}

public class PublicVehicleItem
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
    public int KmHangLanSac { get; set; }
    public string? HinhAnhChinh { get; set; }
    public string? DiaChi { get; set; }
}

public class PublicVehicleDetail : PublicVehicleItem
{
    public int? NamSanXuat { get; set; }
    public string? MoTa { get; set; }
    public int KmTong { get; set; }
    public List<string> HinhAnhList { get; set; } = [];
    public decimal? ViTriLat { get; set; }
    public decimal? ViTriLng { get; set; }
    public Dictionary<string, JsonElement> TinhNang { get; set; } = [];
}
