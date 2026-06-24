using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace PN_HDSWeb_Admin.Services.Public;

/// <summary>Service đăng ký / đăng nhập cho khách hàng thuê xe</summary>
public interface IPublicCustomerService
{
    Task<CustomerRegisterResult> RegisterAsync(CustomerRegisterDto dto);
    Task<CustomerLoginResult> LoginAsync(string soDienThoai, string password);
    Task<PublicCustomerInfo?> GetByIdAsync(int customerId);
    Task<PublicCustomerInfo?> GetBySdtAsync(string soDienThoai);
}

public class PublicCustomerService : IPublicCustomerService
{
    private static readonly string LoginID = PN_LoginService.LoginID_XeDien;
    private readonly ILogger<PublicCustomerService> _logger;

    public PublicCustomerService(ILogger<PublicCustomerService> logger)
    {
        _logger = logger;
    }

    public async Task<CustomerRegisterResult> RegisterAsync(CustomerRegisterDto dto)
    {
        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(dto.SoDienThoai))
                return CustomerRegisterResult.Fail("Số điện thoại không được để trống.");
            if (string.IsNullOrWhiteSpace(dto.HoTen))
                return CustomerRegisterResult.Fail("Họ tên không được để trống.");
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return CustomerRegisterResult.Fail("Mật khẩu phải từ 6 ký tự trở lên.");

            // Kiểm tra trùng SĐT
            var existing = await GetBySdtAsync(dto.SoDienThoai);
            if (existing != null)
                return CustomerRegisterResult.Fail("Số điện thoại đã được đăng ký.");

            var passwordHash = HashPassword(dto.Password);

            var sql = $@"
                INSERT INTO ev_customers (ho_ten, so_dien_thoai, email, cmnd_cccd, password_hash, is_verified)
                VALUES (
                    '{Escape(dto.HoTen)}',
                    '{Escape(dto.SoDienThoai)}',
                    {SqlStr(dto.Email)},
                    {SqlStr(dto.CmndCccd)},
                    '{Escape(passwordHash)}',
                    TRUE
                )
                RETURNING id";

            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0)
                return CustomerRegisterResult.Fail("Lỗi tạo tài khoản. Vui lòng thử lại.");

            var newId = Convert.ToInt32(dt.Rows[0][0]);
            return CustomerRegisterResult.Ok(newId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterAsync failed for {SoDienThoai}", dto.SoDienThoai);
            return CustomerRegisterResult.Fail("Lỗi hệ thống.");
        }
    }

    public async Task<CustomerLoginResult> LoginAsync(string soDienThoai, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(soDienThoai) || string.IsNullOrWhiteSpace(password))
                return CustomerLoginResult.Fail("Vui lòng nhập đầy đủ thông tin.");

            var customer = await GetBySdtAsync(soDienThoai);
            if (customer == null)
                return CustomerLoginResult.Fail("Số điện thoại chưa được đăng ký.");

            if (!customer.IsActive)
                return CustomerLoginResult.Fail("Tài khoản đã bị khóa.");

            // Lấy hash để verify
            var sql = $@"
                SELECT password_hash FROM ev_customers
                WHERE so_dien_thoai = '{Escape(soDienThoai)}'
                  AND is_deleted = FALSE
                LIMIT 1";
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0)
                return CustomerLoginResult.Fail("Tài khoản không tồn tại.");

            var storedHash = dt.Rows[0]["password_hash"]?.ToString() ?? "";
            if (!VerifyPassword(password, storedHash))
                return CustomerLoginResult.Fail("Mật khẩu không đúng.");

            // Cập nhật last_login_at
            await hdataLib.hrunQueryAsync(LoginID,
                $"UPDATE ev_customers SET last_login_at = NOW() WHERE so_dien_thoai = '{Escape(soDienThoai)}'");

            return CustomerLoginResult.Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoginAsync failed");
            return CustomerLoginResult.Fail("Lỗi hệ thống.");
        }
    }

    public async Task<PublicCustomerInfo?> GetByIdAsync(int customerId)
    {
        var sql = $@"
            SELECT id, ho_ten, so_dien_thoai, email, cmnd_cccd, dia_chi, is_active, created_at
            FROM ev_customers
            WHERE id = {customerId} AND is_deleted = FALSE
            LIMIT 1";
        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0) return null;
            return MapCustomer(dt.Rows[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByIdAsync failed for id={Id}", customerId);
            return null;
        }
    }

    public async Task<PublicCustomerInfo?> GetBySdtAsync(string soDienThoai)
    {
        var sql = $@"
            SELECT id, ho_ten, so_dien_thoai, email, cmnd_cccd, dia_chi, is_active, created_at
            FROM ev_customers
            WHERE so_dien_thoai = '{Escape(soDienThoai)}' AND is_deleted = FALSE
            LIMIT 1";
        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID, sql);
            if (dt.Rows.Count == 0) return null;
            return MapCustomer(dt.Rows[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBySdtAsync failed");
            return null;
        }
    }

    // ===== Helpers =====

    private static PublicCustomerInfo MapCustomer(DataRow row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        HoTen = row["ho_ten"]?.ToString(),
        SoDienThoai = row["so_dien_thoai"]?.ToString(),
        Email = row["email"]?.ToString(),
        CmndCccd = row["cmnd_cccd"]?.ToString(),
        DiaChi = row["dia_chi"]?.ToString(),
        IsActive = row["is_active"] == DBNull.Value || Convert.ToBoolean(row["is_active"]),
        CreatedAt = row["created_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["created_at"])
    };

    /// <summary>SHA-256 hash đơn giản. Có thể đổi sang BCrypt nếu cài package.</summary>
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "EV_SALT_2026"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool VerifyPassword(string password, string storedHash) =>
        HashPassword(password) == storedHash;

    private static string Escape(string? v) =>
        string.IsNullOrWhiteSpace(v) ? "" : v.Replace("'", "''");
    private static string SqlStr(string? v) =>
        string.IsNullOrWhiteSpace(v) ? "NULL" : $"'{Escape(v)}'";
}

// ===== DTOs & Models =====

public class CustomerRegisterDto
{
    public string HoTen { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string? Email { get; set; }
    public string? CmndCccd { get; set; }
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public class CustomerRegisterResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int? CustomerId { get; set; }
    public static CustomerRegisterResult Ok(int id) => new() { IsSuccess = true, CustomerId = id };
    public static CustomerRegisterResult Fail(string msg) => new() { IsSuccess = false, Message = msg };
}

public class CustomerLoginResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public PublicCustomerInfo? Customer { get; set; }
    public static CustomerLoginResult Ok(PublicCustomerInfo c) => new() { IsSuccess = true, Customer = c };
    public static CustomerLoginResult Fail(string msg) => new() { IsSuccess = false, Message = msg };
}

public class PublicCustomerInfo
{
    public int Id { get; set; }
    public string? HoTen { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? CmndCccd { get; set; }
    public string? DiaChi { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
