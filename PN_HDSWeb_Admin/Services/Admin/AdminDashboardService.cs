using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminDashboardService
{
    Task<EvDashboardSummary> GetSummaryAsync();
    Task<List<EvDailyRevenue>> GetDailyRevenueAsync(int days = 14);
    Task<List<EvRecentRentalItem>> GetRecentRentalsAsync(int limit = 10);
}

public class AdminDashboardService : IAdminDashboardService
{
    private static readonly string LoginID = PN_LoginService.LoginID_XeDien;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(ILogger<AdminDashboardService> logger)
    {
        _logger = logger;
    }

    public async Task<EvDashboardSummary> GetSummaryAsync()
    {
        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM ev_vehicles WHERE is_deleted = FALSE AND is_active = TRUE)   AS total_vehicles,
                (SELECT COUNT(*) FROM ev_vehicles WHERE tinh_trang = 'available' AND is_deleted = FALSE AND is_active = TRUE) AS xe_trong,
                (SELECT COUNT(*) FROM ev_vehicles WHERE tinh_trang = 'rented')                     AS xe_dang_thue,
                (SELECT COUNT(*) FROM ev_vehicles WHERE tinh_trang = 'maintenance')                AS xe_bao_duong,
                (SELECT COUNT(*) FROM ev_rentals  WHERE trang_thai = 'pending')                    AS don_cho,
                (SELECT COUNT(*) FROM ev_rentals  WHERE trang_thai = 'confirmed')                  AS don_da_xac_nhan,
                (SELECT COUNT(*) FROM ev_rentals  WHERE trang_thai = 'active')                     AS don_dang_thue,
                (SELECT COUNT(*) FROM ev_rentals  WHERE DATE(created_at) = CURRENT_DATE)           AS don_hom_nay,
                (SELECT COUNT(*) FROM ev_rentals  WHERE trang_thai = 'completed'
                    AND DATE(returned_at) = CURRENT_DATE)                                          AS hoan_thanh_hom_nay,
                (SELECT COALESCE(SUM(tong_tien),0) FROM ev_rentals
                    WHERE trang_thai = 'completed'
                    AND DATE_TRUNC('month', returned_at) = DATE_TRUNC('month', CURRENT_DATE))      AS doanh_thu_thang,
                (SELECT COALESCE(SUM(tong_tien),0) FROM ev_rentals
                    WHERE trang_thai = 'completed'
                    AND DATE(returned_at) = CURRENT_DATE)                                          AS doanh_thu_hom_nay,
                (SELECT COUNT(*) FROM ev_customers WHERE is_deleted = FALSE)                        AS total_customers";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0) return new EvDashboardSummary();

            var row = dt.Rows[0];
            return new EvDashboardSummary
            {
                TotalVehicles = ToInt(row["total_vehicles"]),
                XeTrong = ToInt(row["xe_trong"]),
                XeDangThue = ToInt(row["xe_dang_thue"]),
                XeBaoDuong = ToInt(row["xe_bao_duong"]),
                DonCho = ToInt(row["don_cho"]),
                DonDaXacNhan = ToInt(row["don_da_xac_nhan"]),
                DonDangThue = ToInt(row["don_dang_thue"]),
                DonHomNay = ToInt(row["don_hom_nay"]),
                HoanThanhHomNay = ToInt(row["hoan_thanh_hom_nay"]),
                DoanhThuThang = ToDecimal(row["doanh_thu_thang"]),
                DoanhThuHomNay = ToDecimal(row["doanh_thu_hom_nay"]),
                TotalCustomers = ToInt(row["total_customers"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSummaryAsync failed");
            return new EvDashboardSummary();
        }
    }

    public async Task<List<EvDailyRevenue>> GetDailyRevenueAsync(int days = 14)
    {
        var list = new List<EvDailyRevenue>();
        var sql = $@"
            SELECT DATE(returned_at) AS ngay,
                   COUNT(*) AS so_don,
                   COALESCE(SUM(tong_tien), 0) AS doanh_thu
            FROM ev_rentals
            WHERE trang_thai = 'completed'
              AND returned_at >= CURRENT_DATE - INTERVAL '{days} days'
            GROUP BY DATE(returned_at)
            ORDER BY ngay ASC";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new EvDailyRevenue
                {
                    Ngay = Convert.ToDateTime(row["ngay"]),
                    SoDon = ToInt(row["so_don"]),
                    DoanhThu = ToDecimal(row["doanh_thu"])
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDailyRevenueAsync failed");
        }

        return list;
    }

    public async Task<List<EvRecentRentalItem>> GetRecentRentalsAsync(int limit = 10)
    {
        var list = new List<EvRecentRentalItem>();
        var sql = $@"
            SELECT r.id, r.ma_don, v.ten_xe, r.khach_ten, r.khach_sdt,
                   r.tong_tien, r.trang_thai, r.created_at
            FROM ev_rentals r
            JOIN ev_vehicles v ON v.id = r.vehicle_id
            ORDER BY r.created_at DESC
            LIMIT {limit}";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new EvRecentRentalItem
                {
                    Id = ToInt(row["id"]),
                    MaDon = row["ma_don"]?.ToString(),
                    TenXe = row["ten_xe"]?.ToString(),
                    KhachTen = row["khach_ten"]?.ToString(),
                    KhachSdt = row["khach_sdt"]?.ToString(),
                    TongTien = ToDecimal(row["tong_tien"]),
                    TrangThai = row["trang_thai"]?.ToString(),
                    CreatedAt = Convert.ToDateTime(row["created_at"])
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRecentRentalsAsync failed");
        }

        return list;
    }

    private static int ToInt(object v) => v == DBNull.Value || v == null ? 0 : Convert.ToInt32(v);
    private static decimal ToDecimal(object v) => v == DBNull.Value || v == null ? 0 : Convert.ToDecimal(v);
}

public class EvDashboardSummary
{
    public int TotalVehicles { get; set; }
    public int XeTrong { get; set; }
    public int XeDangThue { get; set; }
    public int XeBaoDuong { get; set; }
    public int DonCho { get; set; }
    public int DonDaXacNhan { get; set; }
    public int DonDangThue { get; set; }
    public int DonHomNay { get; set; }
    public int HoanThanhHomNay { get; set; }
    public decimal DoanhThuThang { get; set; }
    public decimal DoanhThuHomNay { get; set; }
    public int TotalCustomers { get; set; }
}

public class EvDailyRevenue
{
    public DateTime Ngay { get; set; }
    public int SoDon { get; set; }
    public decimal DoanhThu { get; set; }
}

public class EvRecentRentalItem
{
    public int Id { get; set; }
    public string? MaDon { get; set; }
    public string? TenXe { get; set; }
    public string? KhachTen { get; set; }
    public string? KhachSdt { get; set; }
    public decimal TongTien { get; set; }
    public string? TrangThai { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TrangThaiLabel => TrangThai switch
    {
        "pending"   => "Chờ duyệt",
        "confirmed" => "Đã xác nhận",
        "active"    => "Đang thuê",
        "completed" => "Hoàn thành",
        "cancelled" => "Đã hủy",
        _ => TrangThai ?? ""
    };
    public string TrangThaiColor => TrangThai switch
    {
        "pending"   => "warning",
        "confirmed" => "info",
        "active"    => "success",
        "completed" => "secondary",
        "cancelled" => "danger",
        _ => "secondary"
    };
}
