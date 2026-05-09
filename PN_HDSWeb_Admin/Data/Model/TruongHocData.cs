
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using PN_HDSWeb_Library;

using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace PN_HDSWeb_Admin.Data.Model
{
    public class TruongHocData
    {

        public List<KhoiHoc_DTO> danhSachKhoiList = new List<KhoiHoc_DTO>();
        public List<LopHoc_DTO> danhSachLopList = new List<LopHoc_DTO>();

        public List<ThongTinTruongV2> danhSachTruongList = new List<ThongTinTruongV2>();
        public ThongTinTruongV2 truong = default!;

        public List<SsoSessionTimeDTO> thongtinSsoTimeList = new List<SsoSessionTimeDTO>();
        public static string API_KetNoiSGD { get; set; } = "https://ketnoisogd.titkul.edu.vn/SSO/";
    }

    #region CẤU HÌNH NĂM HỌC 
    public class CauHinhNamHocDTO
    {
        public string MaNamHoc { get; set; } = string.Empty;
        public string TenNamHoc { get; set; } = string.Empty;

        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
    }

    public class HocKyDTO
    {
        public int MaHocKy { get; set; }          // ma_hocky (PK, serial)

        public string TenHocKy { get; set; } = ""; // tenhocky

        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }

        public string NamHoc { get; set; } = "";   // vd: 2024-2025

        public int HocKy { get; set; }             // 1 / 2
    }
    #endregion

    #region THÔNG TIN TRƯỜNG
    public class SsoSessionTimeDTO
    {
        public DateTimeOffset Start_Time { get; set; }
        public DateTimeOffset End_Time { get; set; }
    }
    public class ThongTinTruongV2
    {
        public string MaTruongBo { get; set; }
        public string TenTruong { get; set; }
        public string[] Cap { get; set; }
        public int TrangThai { get; set; }
        public int IdTruongSo { get; set; }
        public string PhuongXa { get; set; }
        public string HoTenHieuTruong { get; set; }
        public string SoDienThoaiHieuTruong { set; get; }

        public string DiaChiTruong { get; set; }

        public string LogoTruong { get; set; }


    }
    public class ThongTinNguoiDung
    {
        public string MaTruongBo { get; set; }
        public string HoTen { get; set; }
        public string MaSo { get; set; }
        public int MaChucVu { get; set; }
    }
    public class ThongKeThongTinTruongHoc
    {
        public int SoLuongKhoi { get; set; }
        public int SoLuongLop { get; set; }
        public int SoLuongHocSinh { get; set; }
        public int SoLuongGiaoVien { get; set; }
    }
    public class KhoiHoc_DTO
    {
        public int MaKhoi { get; set; }
        public string MaKhoiBo { get; set; } = string.Empty;
        public string TenKhoi { get; set; } = string.Empty;
        public string Cap { get; set; } = string.Empty;
    }
    public class LopHoc_DTO
    {
        public int IdLopBo { get; set; }
        public string MaKhoiBo { get; set; } = string.Empty;
        public string TenLop { get; set; } = string.Empty;
        public string MaLopBo { get; set; } = string.Empty;
        public string BuoiHoc { get; set; } = string.Empty;

        public int MaKhoi { get; set; }
    }

    public class DanhSachLopTheoBuoi_DTO
    {
        public List<string> LopSang { get; set; } = new List<string>();
        public List<string> LopChieu { get; set; } = new List<string>();
    }

    #endregion




}
