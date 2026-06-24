using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminRentalService
{
    Task<(List<AdminRentalItem> Items, int Total)> GetAllAsync(AdminRentalFilter filter);
    Task<AdminRentalDetail?> GetByIdAsync(int id);
    Task<bool> ConfirmAsync(int id, string adminUser);
    Task<bool> StartRentalAsync(int id, int startKm, int startPin, string adminUser);
    Task<bool> CompleteAsync(int id, int endKm, int endPin, string adminUser);
    Task<bool> CancelAsync(int id, string lyDo, string adminUser);
}

public class AdminRentalService : IAdminRentalService
{
    private static readonly string LoginID = PN_LoginService.LoginID_XeDien;
    private readonly ILogger<AdminRentalService> _logger;

    public AdminRentalService(ILogger<AdminRentalService> logger)
    {
        _logger = logger;
    }

    public async Task<(List<AdminRentalItem> Items, int Total)> GetAllAsync(AdminRentalFilter filter)
    {
        var list = new List<AdminRentalItem>();

        var ttWhere = string.IsNullOrWhiteSpace(filter.TrangThai) ? "" : $"AND r.trang_thai = '{Escape(filter.TrangThai)}'";
        var searchWhere = string.IsNullOrWhiteSpace(filter.Search)
            ? ""
            : $"AND (LOWER(r.ma_don) LIKE LOWER('%{Escape(filter.Search)}%') OR LOWER(r.khach_ten) LIKE LOWER('%{Escape(filter.Search)}%') OR LOWER(r.khach_sdt) LIKE LOWER('%{Escape(filter.Search)}%'))";
        var dateWhere = "";
        if (filter.TuNgay.HasValue)
            dateWhere += $" AND r.created_at >= '{filter.TuNgay.Value:yyyy-MM-dd}'";
        if (filter.DenNgay.HasValue)
            dateWhere += $" AND r.created_at < '{filter.DenNgay.Value.AddDays(1):yyyy-MM-dd}'";

        var offset = (filter.Page - 1) * filter.PageSize;

        var countSql = $@"
            SELECT COUNT(*) FROM ev_rentals r
            WHERE 1=1 {ttWhere} {searchWhere} {dateWhere}";

        var dataSql = $@"
            SELECT r.id, r.ma_don, r.vehicle_id, v.ten_xe, v.loai_xe, v.bien_so,
                   r.khach_ten, r.khach_sdt, r.khach_email,
                   r.bat_dau_thue, r.ket_thuc_thue, r.so_gio, r.so_ngay,
                   r.tong_tien, r.tien_dat_coc, r.trang_thai, r.created_at, r.confirmed_at
            FROM ev_rentals r
            JOIN ev_vehicles v ON v.id = r.vehicle_id
            WHERE 1=1 {ttWhere} {searchWhere} {dateWhere}
            ORDER BY r.created_at DESC
            LIMIT {filter.PageSize} OFFSET {offset}";

        try
        {
            var countDt = await hdataLib.hgetDataTableAsync(LoginID, countSql);
            var total = countDt.Rows.Count > 0 ? Convert.ToInt32(countDt.Rows[0][0]) : 0;

            var dt = await hdataLib.hgetDataTableAsync(LoginID, dataSql);
            foreach (DataRow row in dt.Rows)
                list.Add(MapRentalItem(row));

            return (list, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminRentalService.GetAllAsync failed");
            return (list, 0);
        }
    }

    public async Task<AdminRentalDetail?> GetByIdAsync(int id)
    {
        var sql = $@"
            SELECT r.id, r.ma_don, r.vehicle_id, v.ten_xe, v.loai_xe, v.bien_so, v.hinh_anh_chinh,
                   r.customer_id, r.khach_ten, r.khach_sdt, r.khach_email, r.khach_cmnd,
                   r.bat_dau_thue, r.ket_thuc_thue, r.so_gio, r.so_ngay,
                   r.don_gia, r.tong_tien, r.tien_dat_coc, r.trang_thai,
                   r.ly_do_huy, r.ghi_chu, r.start_km, r.end_km, r.start_pin, r.end_pin,
                   r.created_at, r.confirmed_at, r.returned_at
            FROM ev_rentals r
            JOIN ev_vehicles v ON v.id = r.vehicle_id
            WHERE r.id = {id} LIMIT 1";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0) return null;
            var row = dt.Rows[0];

            return new AdminRentalDetail
            {
                Id = Convert.ToInt32(row["id"]),
                MaDon = row["ma_don"]?.ToString(),
                VehicleId = Convert.ToInt32(row["vehicle_id"]),
                TenXe = row["ten_xe"]?.ToString(),
                LoaiXe = row["loai_xe"]?.ToString(),
                BienSo = row["bien_so"]?.ToString(),
                HinhAnhXe = row["hinh_anh_chinh"]?.ToString(),
                CustomerId = row["customer_id"] == DBNull.Value ? null : Convert.ToInt32(row["customer_id"]),
                KhachTen = row["khach_ten"]?.ToString(),
                KhachSdt = row["khach_sdt"]?.ToString(),
                KhachEmail = row["khach_email"]?.ToString(),
                KhachCmnd = row["khach_cmnd"]?.ToString(),
                BatDauThue = Convert.ToDateTime(row["bat_dau_thue"]),
                KetThucThue = Convert.ToDateTime(row["ket_thuc_thue"]),
                SoGio = ToInt(row["so_gio"]),
                SoNgay = ToInt(row["so_ngay"]),
                DonGia = ToDecimal(row["don_gia"]),
                TongTien = ToDecimal(row["tong_tien"]),
                TienDatCoc = ToDecimal(row["tien_dat_coc"]),
                TrangThai = row["trang_thai"]?.ToString(),
                LyDoHuy = row["ly_do_huy"]?.ToString(),
                GhiChu = row["ghi_chu"]?.ToString(),
                StartKm = ToInt(row["start_km"]),
                EndKm = ToInt(row["end_km"]),
                StartPin = ToInt(row["start_pin"]),
                EndPin = ToInt(row["end_pin"]),
                CreatedAt = Convert.ToDateTime(row["created_at"]),
                ConfirmedAt = row["confirmed_at"] == DBNull.Value ? null : Convert.ToDateTime(row["confirmed_at"]),
                ReturnedAt = row["returned_at"] == DBNull.Value ? null : Convert.ToDateTime(row["returned_at"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AdminRentalService.GetByIdAsync failed id={Id}", id);
            return null;
        }
    }

    public async Task<bool> ConfirmAsync(int id, string adminUser)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID, $@"
                UPDATE ev_rentals
                SET trang_thai = 'confirmed', confirmed_at = NOW(), updated_at = NOW()
                WHERE id = {id} AND trang_thai = 'pending'");

            // Cập nhật trạng thái xe
            var rentalDt = await hdataLib.hgetDataTableAsync(LoginID,
                $"SELECT vehicle_id FROM ev_rentals WHERE id = {id} LIMIT 1");
            if (rentalDt.Rows.Count > 0)
            {
                var vehicleId = Convert.ToInt32(rentalDt.Rows[0]["vehicle_id"]);
                await hdataLib.hrunQueryAsync(LoginID,
                    $"UPDATE ev_vehicles SET tinh_trang = 'rented', updated_at = NOW() WHERE id = {vehicleId}");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmAsync failed id={Id}", id);
            return false;
        }
    }

    public async Task<bool> StartRentalAsync(int id, int startKm, int startPin, string adminUser)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID, $@"
                UPDATE ev_rentals
                SET trang_thai = 'active', start_km = {startKm}, start_pin = {startPin}, updated_at = NOW()
                WHERE id = {id} AND trang_thai = 'confirmed'");

            // Ghi log
            var rentalDt = await hdataLib.hgetDataTableAsync(LoginID,
                $"SELECT vehicle_id FROM ev_rentals WHERE id = {id} LIMIT 1");
            if (rentalDt.Rows.Count > 0)
            {
                var vehicleId = Convert.ToInt32(rentalDt.Rows[0]["vehicle_id"]);
                await hdataLib.hrunQueryAsync(LoginID, $@"
                    INSERT INTO ev_vehicle_logs (vehicle_id, rental_id, su_kien, pin_truoc, km_truoc, ghi_chu, created_by)
                    VALUES ({vehicleId}, {id}, 'rented', {startPin}, {startKm}, 'Bàn giao xe', '{Escape(adminUser)}')");
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartRentalAsync failed id={Id}", id);
            return false;
        }
    }

    public async Task<bool> CompleteAsync(int id, int endKm, int endPin, string adminUser)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID, $@"
                UPDATE ev_rentals
                SET trang_thai = 'completed', end_km = {endKm}, end_pin = {endPin},
                    returned_at = NOW(), updated_at = NOW()
                WHERE id = {id} AND trang_thai = 'active'");

            var rentalDt = await hdataLib.hgetDataTableAsync(LoginID,
                $"SELECT vehicle_id, start_km, start_pin FROM ev_rentals WHERE id = {id} LIMIT 1");

            if (rentalDt.Rows.Count > 0)
            {
                var vehicleId = Convert.ToInt32(rentalDt.Rows[0]["vehicle_id"]);
                var startKm = ToInt(rentalDt.Rows[0]["start_km"]);
                var startPin = ToInt(rentalDt.Rows[0]["start_pin"]);

                // Trả xe → xe available lại
                await hdataLib.hrunQueryAsync(LoginID, $@"
                    UPDATE ev_vehicles
                    SET tinh_trang = 'available', pin_phan_tram = {endPin},
                        km_tong = km_tong + {Math.Max(0, endKm - startKm)}, updated_at = NOW()
                    WHERE id = {vehicleId}");

                // Ghi log trả xe
                await hdataLib.hrunQueryAsync(LoginID, $@"
                    INSERT INTO ev_vehicle_logs
                        (vehicle_id, rental_id, su_kien, pin_truoc, pin_sau, km_truoc, km_sau, ghi_chu, created_by)
                    VALUES ({vehicleId}, {id}, 'returned', {startPin}, {endPin}, {startKm}, {endKm}, 'Nhận xe trả', '{Escape(adminUser)}')");
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompleteAsync failed id={Id}", id);
            return false;
        }
    }

    public async Task<bool> CancelAsync(int id, string lyDo, string adminUser)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID, $@"
                UPDATE ev_rentals
                SET trang_thai = 'cancelled', ly_do_huy = '{Escape(lyDo)}', updated_at = NOW()
                WHERE id = {id} AND trang_thai IN ('pending','confirmed')");

            // Trả xe về available
            var rentalDt = await hdataLib.hgetDataTableAsync(LoginID,
                $"SELECT vehicle_id, trang_thai FROM ev_rentals WHERE id = {id} LIMIT 1");
            if (rentalDt.Rows.Count > 0)
            {
                var vehicleId = Convert.ToInt32(rentalDt.Rows[0]["vehicle_id"]);
                await hdataLib.hrunQueryAsync(LoginID,
                    $"UPDATE ev_vehicles SET tinh_trang = 'available', updated_at = NOW() WHERE id = {vehicleId}");
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelAsync failed id={Id}", id);
            return false;
        }
    }

    private static AdminRentalItem MapRentalItem(DataRow row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        MaDon = row["ma_don"]?.ToString(),
        VehicleId = Convert.ToInt32(row["vehicle_id"]),
        TenXe = row["ten_xe"]?.ToString(),
        LoaiXe = row["loai_xe"]?.ToString(),
        BienSo = row["bien_so"]?.ToString(),
        KhachTen = row["khach_ten"]?.ToString(),
        KhachSdt = row["khach_sdt"]?.ToString(),
        BatDauThue = Convert.ToDateTime(row["bat_dau_thue"]),
        KetThucThue = Convert.ToDateTime(row["ket_thuc_thue"]),
        TongTien = ToDecimal(row["tong_tien"]),
        TrangThai = row["trang_thai"]?.ToString(),
        CreatedAt = Convert.ToDateTime(row["created_at"])
    };

    private static decimal ToDecimal(object v) => v == DBNull.Value || v == null ? 0 : Convert.ToDecimal(v);
    private static int ToInt(object v) => v == DBNull.Value || v == null ? 0 : Convert.ToInt32(v);
    private static string Escape(string? v) => string.IsNullOrWhiteSpace(v) ? "" : v.Replace("'", "''");
}

// ===== Models =====

public class AdminRentalFilter
{
    public string? TrangThai { get; set; }
    public string? Search { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminRentalItem
{
    public int Id { get; set; }
    public string? MaDon { get; set; }
    public int VehicleId { get; set; }
    public string? TenXe { get; set; }
    public string? LoaiXe { get; set; }
    public string? BienSo { get; set; }
    public string? KhachTen { get; set; }
    public string? KhachSdt { get; set; }
    public DateTime BatDauThue { get; set; }
    public DateTime KetThucThue { get; set; }
    public decimal TongTien { get; set; }
    public string? TrangThai { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TrangThaiLabel => TrangThai switch
    {
        "pending"   => "Chờ xác nhận",
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

public class AdminRentalDetail : AdminRentalItem
{
    public string? HinhAnhXe { get; set; }
    public int? CustomerId { get; set; }
    public string? KhachEmail { get; set; }
    public string? KhachCmnd { get; set; }
    public int SoGio { get; set; }
    public int SoNgay { get; set; }
    public decimal DonGia { get; set; }
    public decimal TienDatCoc { get; set; }
    public string? LyDoHuy { get; set; }
    public string? GhiChu { get; set; }
    public int StartKm { get; set; }
    public int EndKm { get; set; }
    public int StartPin { get; set; }
    public int EndPin { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}
