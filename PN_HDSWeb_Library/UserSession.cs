using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PN_HDSWeb_Library
{
    public class SessionLopChuNhiem
    {
        public int IdLopBo { get; set; }
        public string TenLop { get; set; } = string.Empty;
        public string MaKhoiBo { get; set; } = string.Empty;
    }

    public class SessionPhanCongGiangDay
    {
        public int IdLopBo { get; set; }
        public string TenLop { get; set; } = string.Empty;
        public string MaMonHoc { get; set; } = string.Empty;
        public string TenMonHoc { get; set; } = string.Empty;
        public string MaHocKy { get; set; } = string.Empty;
    }

    public class UserSession
    {
        
    

    public string? UserName { get; set; }

        public string? Role { get; set; }
        public string? MaUser { get; set; }
        public string? MaTruongBo { get; set; }
        public string? DeviceName { get; set; }
        public int IdUser { get; set; }
        public string? TaiKhoanKySo { get; set; }
        public string? NamHoc { get; set; }
        public string? MaChucVu { get; set; }

        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        public string? TenTruong { get; set; }
        public string? FullName { get; set; }
        public string[]? Cap { get; set; }

        // Bổ sung lưu trữ phân công chủ nhiệm và giảng dạy
        public List<SessionLopChuNhiem>? DanhSachLopChuNhiem { get; set; }
        public List<SessionPhanCongGiangDay>? DanhSachLopGiangDay { get; set; }

        public DateTime LoginTime { get; set; }
        public DateTime ExpiryTime { get; set; }
        public string? IPAddress { get; set; }
        public string? BrowserInfo { get; set; }

        /// <summary>
        /// Session Fingerprint: Hash của User-Agent dùng để detect session hijacking.
        /// Được tạo lúc đăng nhập và validate mỗi request.
        /// </summary>
        public string? UserAgentHash { get; set; }

        /// <summary>
        /// Tạo fingerprint hash từ User-Agent string.
        /// </summary>
        public static string CreateUserAgentHash(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(userAgent);
            var hash = SHA256.HashData(bytes);
            // Chỉ lấy 16 byte đầu (128-bit) để hash ngắn gọn
            return Convert.ToHexString(hash[..16]).ToLowerInvariant();
        }

        /// <summary>
        /// Kiểm tra fingerprint có khớp không.
        /// </summary>
        public bool IsValidFingerprint(string? currentUserAgent)
        {
            // Nếu session cũ không có fingerprint → bỏ qua validate (backward compat)
            if (string.IsNullOrEmpty(UserAgentHash))
                return true;

            var currentHash = CreateUserAgentHash(currentUserAgent);
            return string.Equals(UserAgentHash, currentHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra session còn hợp lệ.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(MaUser) &&
                   !string.IsNullOrEmpty(MaTruongBo) &&
                   ExpiryTime > DateTime.UtcNow;
        }
    }
}
