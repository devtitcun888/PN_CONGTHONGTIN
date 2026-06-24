using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicRentalService
{
    Task<CreateRentalResult> CreateRentalAsync(CreateRentalDto dto);
    Task<PublicRentalInfo?> GetRentalByMaDonAsync(string maDon);
    Task<CancelRentalResult> CancelRentalAsync(string maDon, string lyDoHuy);
    Task<List<PublicRentalInfo>> GetRentalsByCustomerAsync(int customerId, int limit = 20);
}

public class PublicRentalService : IPublicRentalService
{
    private static readonly string LoginID = PN_LoginService.LoginID_XeDien;
    private readonly ILogger<PublicRentalService> _logger;
    private readonly IPublicVehicleService _vehicleService;

    public PublicRentalService(ILogger<PublicRentalService> logger, IPublicVehicleService vehicleService)
    {
        _logger = logger;
        _vehicleService = vehicleService;
    }

    public async Task<CreateRentalResult> CreateRentalAsync(CreateRentalDto dto)
    {
        try
        {
            // 1. Kiểm tra xe còn trống
            var available = await _vehicleService.CheckAvailabilityAsync(dto.VehicleId, dto.BatDauThue, dto.KetThucThue);
            if (!available)
                return CreateRentalResult.Fail("Xe đã được đặt trong khoảng thời gian này.");

            // 2. Lấy thông tin xe để tính tiền
            var xe = await _vehicleService.GetVehicleDetailAsync(dto.VehicleId);
            if (xe == null)
                return CreateRentalResult.Fail("Không tìm thấy thông tin xe.");
            if (xe.TinhTrang != "available")
                return CreateRentalResult.Fail("Xe hiện không khả dụng.");

            // 3. Tính tiền
            var span = dto.KetThucThue - dto.BatDauThue;
            var soGio = (int)Math.Ceiling(span.TotalHours);
            var soNgay = (int)Math.Ceiling(span.TotalDays);
            decimal tongTien;

            if (soNgay >= 1 && xe.GiaThueNgay > 0)
                tongTien = soNgay * xe.GiaThueNgay;
            else
                tongTien = soGio * xe.GiaThueGio;

            // 4. Sinh mã đơn
            var maDon = await GenerateMaDonAsync();

            // 5. Insert đơn thuê
            var sql = $@"
                INSERT INTO ev_rentals
                    (ma_don, vehicle_id, customer_id,
                     khach_ten, khach_sdt, khach_email, khach_cmnd,
                     bat_dau_thue, ket_thuc_thue, so_gio, so_ngay,
                     don_gia, tong_tien, tien_dat_coc, ghi_chu, trang_thai)
                VALUES (
                    '{Escape(maDon)}', {dto.VehicleId}, {(dto.CustomerId.HasValue ? dto.CustomerId.Value.ToString() : "NULL")},
                    '{Escape(dto.KhachTen)}', '{Escape(dto.KhachSdt)}',
                    {SqlStr(dto.KhachEmail)}, {SqlStr(dto.KhachCmnd)},
                    '{dto.BatDauThue:yyyy-MM-dd HH:mm:ss}', '{dto.KetThucThue:yyyy-MM-dd HH:mm:ss}',
                    {soGio}, {soNgay},
                    {(xe.GiaThueNgay > 0 ? xe.GiaThueNgay : xe.GiaThueGio)},
                    {tongTien}, {xe.DatCoc},
                    {SqlStr(dto.GhiChu)}, 'pending'
                )
                RETURNING id";

            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0)
                return CreateRentalResult.Fail("Lỗi tạo đơn. Vui lòng thử lại.");

            var rentalId = Convert.ToInt32(dt.Rows[0][0]);
            return CreateRentalResult.Ok(rentalId, maDon, tongTien);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateRentalAsync failed");
            return CreateRentalResult.Fail("Lỗi hệ thống. Vui lòng thử lại sau.");
        }
    }

    public async Task<PublicRentalInfo?> GetRentalByMaDonAsync(string maDon)
    {
        if (string.IsNullOrWhiteSpace(maDon)) return null;

        var sql = $@"
            SELECT r.id, r.ma_don, r.vehicle_id, v.ten_xe, v.loai_xe, v.hinh_anh_chinh,
                   r.khach_ten, r.khach_sdt, r.khach_email,
                   r.bat_dau_thue, r.ket_thuc_thue, r.so_gio, r.so_ngay,
                   r.tong_tien, r.tien_dat_coc, r.trang_thai,
                   r.ghi_chu, r.created_at, r.confirmed_at, r.returned_at
            FROM ev_rentals r
            JOIN ev_vehicles v ON v.id = r.vehicle_id
            WHERE r.ma_don = '{Escape(maDon)}'
            LIMIT 1";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0) return null;
            return MapRentalInfo(dt.Rows[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRentalByMaDonAsync failed for {MaDon}", maDon);
            return null;
        }
    }

    public async Task<CancelRentalResult> CancelRentalAsync(string maDon, string lyDoHuy)
    {
        try
        {
            var rental = await GetRentalByMaDonAsync(maDon);
            if (rental == null)
                return CancelRentalResult.Fail("Không tìm thấy đơn.");

            if (rental.TrangThai is "active" or "completed")
                return CancelRentalResult.Fail("Không thể hủy đơn đang thực hiện hoặc đã hoàn thành.");

            if (rental.TrangThai == "cancelled")
                return CancelRentalResult.Fail("Đơn đã được hủy trước đó.");

            var sql = $@"
                UPDATE ev_rentals
                SET trang_thai = 'cancelled', ly_do_huy = '{Escape(lyDoHuy)}', updated_at = NOW()
                WHERE ma_don = '{Escape(maDon)}'";

            await hdataLib.hrunQueryAsync(LoginID, sql);
            return CancelRentalResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelRentalAsync failed for {MaDon}", maDon);
            return CancelRentalResult.Fail("Lỗi hệ thống.");
        }
    }

    public async Task<List<PublicRentalInfo>> GetRentalsByCustomerAsync(int customerId, int limit = 20)
    {
        var list = new List<PublicRentalInfo>();
        var sql = $@"
            SELECT r.id, r.ma_don, r.vehicle_id, v.ten_xe, v.loai_xe, v.hinh_anh_chinh,
                   r.khach_ten, r.khach_sdt, r.khach_email,
                   r.bat_dau_thue, r.ket_thuc_thue, r.so_gio, r.so_ngay,
                   r.tong_tien, r.tien_dat_coc, r.trang_thai,
                   r.ghi_chu, r.created_at, r.confirmed_at, r.returned_at
            FROM ev_rentals r
            JOIN ev_vehicles v ON v.id = r.vehicle_id
            WHERE r.customer_id = {customerId}
            ORDER BY r.created_at DESC
            LIMIT {limit}";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            foreach (DataRow row in dt.Rows)
                list.Add(MapRentalInfo(row));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRentalsByCustomerAsync failed for customerId={Id}", customerId);
        }

        return list;
    }

    // ===== Helpers =====

    private async Task<string> GenerateMaDonAsync()
    {
        var prefix = $"EV{DateTime.Now:yyyyMMdd}";
        var sql = $@"
            SELECT COALESCE(MAX(CAST(SUBSTRING(ma_don FROM 11) AS INT)), 0) + 1
            FROM ev_rentals
            WHERE ma_don LIKE '{prefix}%'";
        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            var seq = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 1;
            return $"{prefix}{seq:D3}";
        }
        catch
        {
            return $"{prefix}{new Random().Next(100, 999)}";
        }
    }

    private static PublicRentalInfo MapRentalInfo(DataRow row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        MaDon = row["ma_don"]?.ToString(),
        VehicleId = Convert.ToInt32(row["vehicle_id"]),
        TenXe = row["ten_xe"]?.ToString(),
        LoaiXe = row["loai_xe"]?.ToString(),
        HinhAnhXe = row["hinh_anh_chinh"]?.ToString(),
        KhachTen = row["khach_ten"]?.ToString(),
        KhachSdt = row["khach_sdt"]?.ToString(),
        KhachEmail = row["khach_email"]?.ToString(),
        BatDauThue = row["bat_dau_thue"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["bat_dau_thue"]),
        KetThucThue = row["ket_thuc_thue"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["ket_thuc_thue"]),
        SoGio = row["so_gio"] == DBNull.Value ? 0 : Convert.ToInt32(row["so_gio"]),
        SoNgay = row["so_ngay"] == DBNull.Value ? 0 : Convert.ToInt32(row["so_ngay"]),
        TongTien = row["tong_tien"] == DBNull.Value ? 0 : Convert.ToDecimal(row["tong_tien"]),
        TienDatCoc = row["tien_dat_coc"] == DBNull.Value ? 0 : Convert.ToDecimal(row["tien_dat_coc"]),
        TrangThai = row["trang_thai"]?.ToString(),
        GhiChu = row["ghi_chu"]?.ToString(),
        CreatedAt = row["created_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["created_at"]),
        ConfirmedAt = row["confirmed_at"] == DBNull.Value ? null : Convert.ToDateTime(row["confirmed_at"]),
        ReturnedAt = row["returned_at"] == DBNull.Value ? null : Convert.ToDateTime(row["returned_at"])
    };

    private static string Escape(string? v) =>
        string.IsNullOrWhiteSpace(v) ? "" : v.Replace("'", "''");

    private static string SqlStr(string? v) =>
        string.IsNullOrWhiteSpace(v) ? "NULL" : $"'{Escape(v)}'";
}

// ===== DTOs & Results =====

public class CreateRentalDto
{
    public int VehicleId { get; set; }
    public int? CustomerId { get; set; }
    public string KhachTen { get; set; } = "";
    public string KhachSdt { get; set; } = "";
    public string? KhachEmail { get; set; }
    public string? KhachCmnd { get; set; }
    public DateTime BatDauThue { get; set; }
    public DateTime KetThucThue { get; set; }
    public string? GhiChu { get; set; }
}

public class CreateRentalResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int? RentalId { get; set; }
    public string? MaDon { get; set; }
    public decimal TongTien { get; set; }

    public static CreateRentalResult Ok(int rentalId, string maDon, decimal tongTien) =>
        new() { IsSuccess = true, RentalId = rentalId, MaDon = maDon, TongTien = tongTien };
    public static CreateRentalResult Fail(string message) =>
        new() { IsSuccess = false, Message = message };
}

public class CancelRentalResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public static CancelRentalResult Ok() => new() { IsSuccess = true };
    public static CancelRentalResult Fail(string msg) => new() { IsSuccess = false, Message = msg };
}

public class PublicRentalInfo
{
    public int Id { get; set; }
    public string? MaDon { get; set; }
    public int VehicleId { get; set; }
    public string? TenXe { get; set; }
    public string? LoaiXe { get; set; }
    public string? HinhAnhXe { get; set; }
    public string? KhachTen { get; set; }
    public string? KhachSdt { get; set; }
    public string? KhachEmail { get; set; }
    public DateTime BatDauThue { get; set; }
    public DateTime KetThucThue { get; set; }
    public int SoGio { get; set; }
    public int SoNgay { get; set; }
    public decimal TongTien { get; set; }
    public decimal TienDatCoc { get; set; }
    public string? TrangThai { get; set; }
    public string? GhiChu { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public string TrangThaiLabel => TrangThai switch
    {
        "pending"   => "Chờ xác nhận",
        "confirmed" => "Đã xác nhận",
        "active"    => "Đang thuê",
        "completed" => "Đã hoàn thành",
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
