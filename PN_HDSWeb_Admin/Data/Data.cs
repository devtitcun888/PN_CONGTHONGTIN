
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
namespace PN_HDSWeb_Admin.Data
{
    public class data
    {

    }


    public class UserLoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string MaTruongBo { get; set; }
    }
    public class UserSSOLoginRequest
    {
        public string SSOUserName { get; set; }
        public string SSOPassword { get; set; }
        public bool IsHocSinh { get; set; }
    }
    public class SSOLoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string __RequestVerificationToken { get; set; }
        public string Token { get; set; }
        public bool isHocSinh { get; set; }
        public string Cookie { get; set; }
    }
    public class GetSSOLoginUrlRequest
    {
        public string SysUserName { get; set; }
        public string SysPassword { get; set; }
        public string Param1 { get; set; }
        public string Param2 { get; set; }
        public string Param3 { get; set; }
        public string Returnuri { get; set; }
        public bool isHocSinh { get; set; }
    }
    public class GetSSOLoginUrl
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string Result { get; set; }
    }


    public class AppUser : IdentityUser<Guid>
    {
        public string DisplayName { get; set; }
        public string SSOUserName { get; set; }
        public string SSOPassword { get; set; }
    }
    public class LoginViewModel
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime Expires { get; set; }
        public bool IsSchool { get; set; }
        public string RoleName { get; set; }
        public int? DepartmentId { get; set; }
        public string SSOToken { get; set; }
    }
    //Authen

    public class AuthRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public record AuthResponse
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
        public string? Token { get; set; }
    }
    //Token provider
    public class TokenProvider
    {
        public string? AccessToken { get; set; }
    }

    public class ThongTinTruongSSO
    {
        public object? ID { get; set; }
        public string? UserID { get; set; }
        public string? UserName { get; set; }
        public object? TimeStampt { get; set; }
        public string IP { get; set; }
        public bool isActive { get; set; }
        public string? SchoolId { get; set; }
        public string? AccountType { get; set; }
        public int SystemUsing { get; set; }
        public string? Param1 { get; set; }
        public string? Param2 { get; set; }
        public string? Param3 { get; set; }
        public int TimeInterval { get; set; }
        public int CallCount { get; set; }
        public List<object>? UserData { get; set; }
        public List<object>? PhanCongGiangDay { get; set; }
        public List<object>? PhanCongChuNhiem { get; set; }
        public List<TruongData>? TruongData { get; set; }
    }
    public class TruongData
    {
        public object? ID { get; set; }
        public int? MA_NAM_HOC { get; set; }
        public string? MA_SO_GD { get; set; }
        public string? MA { get; set; }
        public string? TEN { get; set; }
        public string? MA_NHOM_CAP_HOC { get; set; }
        public string? DS_CAP_HOC { get; set; }
        public object? ID_PHONG_GD { get; set; }
        public string? MA_PHONG_GD { get; set; }
        public string? MA_TINH { get; set; }
        public object? ID_HUYEN { get; set; }
        public string? MA_HUYEN { get; set; }
        public object? ID_XA { get; set; }
        public string? MA_XA { get; set; }
        public string? DIA_CHI { get; set; }
        public string? MA_LOAI_HINH_TRUONG { get; set; }
        public string? MA_LOAI_TRUONG { get; set; }
        public string? MA_KHU_VUC { get; set; }
        public object? MA_DAT_CHUAN_DANH_GIA_CLGD { get; set; }
        public object? MA_TRUC_THUOC { get; set; }
        public object? MA_DU_AN { get; set; }
        public int? SO_DIEM_TRUONG { get; set; }
        public string? DIEN_THOAI { get; set; }
        public string? EMAIL { get; set; }
        public object? FAX { get; set; }
        public string? WEBSITE { get; set; }
        public object? VI_TRI_BAN_DO { get; set; }
        public string? HIEU_TRUONG { get; set; }
        public string? DIEN_THOAI_HIEU_TRUONG { get; set; }
        public string? EMAIL_HIEU_TRUONG { get; set; }
        public int? IS_CO_CHI_BO_DANG { get; set; }
        public int? IS_DAT_CHUAN_QG { get; set; }
        public int? IS_TRUONG_QUOC_TE { get; set; }
        public object? IS_CAP_MN { get; set; }
        public object? IS_CAP_TH { get; set; }
        public object? IS_CAP_THCS { get; set; }
        public object? IS_CAP_THPT { get; set; }
        public object? IS_CAP_GDTX { get; set; }
        public int? IS_HOC_SINH_KHUYET_TAT { get; set; }
        public int? IS_HOC_SINH_BAN_TRU { get; set; }
        public int? IS_HOC_SINH_NOI_TRU { get; set; }
        public int? IS_VUNG_DAC_BIET_KHO_KHAN { get; set; }
        public int? IS_DAT_CHAT_LUONG_TOI_THIEU { get; set; }
        public int? IS_2_BUOI_NGAY { get; set; }
        public object? DIEN_TICH { get; set; }
        public object? THU_TU { get; set; }
        public object? NGUOI_TAO { get; set; }
        public object? NGAY_TAO { get; set; }
        public object? NGUOI_SUA { get; set; }
        public object? NGAY_SUA { get; set; }
        public object? NAM_THANH_LAP { get; set; }
        public object? IS_DAY_NGHE_PHO_THONG { get; set; }
        public object? IS_CO_LOP_KHONG_CHUYEN { get; set; }
        public object? IS_KY_NANG_SONG_GDXH { get; set; }
        public object? IS_TT_HOC_TAP_CD { get; set; }
        public object? IS_TT_NGOAI_NGU_TH { get; set; }
        public object? IS_CS_NGOI_NGU_TH { get; set; }
        public object? THU_MUC_ANH { get; set; }
        public object? TEN_ANH_1 { get; set; }
        public object? TEN_ANH_2 { get; set; }
        public object? TEN_ANH_3 { get; set; }
        public object? TEN_ANH_4 { get; set; }
        public object? TEN_ANH_5 { get; set; }
        public object? MA_HOC_BAN_TRU { get; set; }
        public object? MA_SO_BUOI_HOC_TREN_TUAN { get; set; }
        public object? IS_SU_DUNG_MAY_TINH_DAY_HOC { get; set; }
        public object? IS_KHAI_THAC_INTERNET_DAY_HOC { get; set; }
        public object? IS_DIEN_LUOI { get; set; }
        public object? IS_NGUON_NUOC_SACH { get; set; }
        public object? IS_CT_GDVS_DOI_TAY { get; set; }
        public object? IS_CHUONG_TRINH_GIAO_DUC_CO_BAN { get; set; }
        public object? IS_CO_HA_TANG_TLHT_PHU_HOP_HSKT { get; set; }
        public object? IS_CONG_TAC_TU_VAN_HOC_DUONG { get; set; }
        public object? IS_CO_BE_BOI { get; set; }
        public object? IS_MUC_DAT_CHUAN_TOI_THIEU { get; set; }
        public object? IS_CONG_TRINH_VE_SINH { get; set; }
        public string? MA_VUNG { get; set; }
        public object? ID_LOAI_HINH { get; set; }
        public object? ID_DAT_CHUAN_DANH_GIA_CLGD { get; set; }
        public object? MA_TRUNG_TAM { get; set; }
        public object? IS_TT_GDTX_HUYEN { get; set; }
        public object? IS_TT_GD_NGHE_NGHIEP { get; set; }
        public object? IS_TT_GDTX_HUONG_NGHIEP { get; set; }
        public object? IS_TT_HTCD_KH_NHA_VAN_HOA { get; set; }
        public object? IS_TT_NN_CO_VON_NUOC_NGOAI { get; set; }
        public object? TRANG_THAI { get; set; }
        public object? ID_TRUONG_GHEP_VAO { get; set; }
        public object? IS_CO_LOP_NHO { get; set; }
        public object? IS_NHO_TREN { get; set; }
        public object? IS_NHO_DUOI { get; set; }
        public object? IS_KHOA_K1 { get; set; }
        public object? IS_KHOA_K2 { get; set; }
        public object? THANG_DAU_KY_II { get; set; }
        public object? THANG_CUOI_KY_II { get; set; }
        public object? IS_MO_RONG { get; set; }
        public object? IS_CO_KE_HOACH_PHONG_CHONG_THIEN_TAI { get; set; }
        public object? IS_CHUONG_TRINH_SONG_NGU { get; set; }
        public object? IS_ACTIVE_ENV { get; set; }
        public string? BRANDNAME { get; set; }
        public object? IS_ACTIVE_SMS { get; set; }
        public object? IS_ACTIVE_TRAC_NGHIEM { get; set; }
        public object? TUYEN_SINH_SO_LOP { get; set; }
        public object? TUYEN_SINH_CHI_TIEU { get; set; }
        public object? IS_NHOM_TOI_DA_7_TRE { get; set; }
        public object? IS_KHU_CONG_NGHIEP_CHE_XUAT { get; set; }
        public object? IS_GIU_TRE_NGOAI_GIO { get; set; }
        public object? IS_CT_TIEN_TIEN_HOI_NHAP { get; set; }
        public object? IS_XUAT_AN_CONG_NGHIEP { get; set; }
        public object? IS_BEP_NAU_TAI_TRUONG { get; set; }
        public object? MA_KIEM_DINH_CHAT_LUONG { get; set; }
        public object? IS_ACTIVE_TUYEN_SINH_DAU_CAP { get; set; }
        public object? MA_KIEM_DINH_CHAT_LUONG_TU_DANH_GIA { get; set; }
        public object? MA_NAM_DANH_GIA { get; set; }
        public object? MA_THANH_KIEM_TRA_NAM_HOC { get; set; }
        public object? IS_PHONG_HOC_TIENG_ANH { get; set; }
        public object? IS_THI_DIEM_CHO_TRE_LAM_QUEN_TIENG_ANH { get; set; }
        public string? API_MA_HCM { get; set; }
        public object? IS_CO_NUOC_UONG { get; set; }
        public object? IS_TRANG_THAI_K1 { get; set; }
        public object? IS_TRANG_THAI_K2 { get; set; }
        public object? NGAY_TRANG_THAI { get; set; }
        public string? TEN_VIET_TAT { get; set; }
        public object? DIEM_XET_TUYEN { get; set; }
        public int? IS_THAY_DOI { get; set; }
        public string? API_MA_BO { get; set; }
        public int? IS_DUOC_PHEP_DONG_BO { get; set; }
        public object? IS_TRUONG_CO_SAN_CHOI { get; set; }
        public object? IS_TRUONG_CO_KHU_VUI_CHOI_PHAT_TRIEN_VAN_DONG { get; set; }
        public object? IS_TRUONG_CO_BEP_AN { get; set; }
        public object? CHUAN_DGQLGD_NAM_DANH_GIA { get; set; }
        public object? IS_CHUAN_DGQLGD_TRUONG_DE_NGHI_CONG_NHAN_LAI { get; set; }
        public object? IS_CHUAN_DGQLGD_TRUONG_DE_NGHI_CONG_NHAN_MOI { get; set; }
        public object? IS_CHUAN_DGQLGD_TRUONG_DE_NGHI_NANG_CHUAN { get; set; }
        public object? IS_SU_DUNG_KY_DIEN_TU { get; set; }
        public string? TEN_PHONG_GD { get; set; }
    }

    public class ApiResponseSingle<T>
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public T? Result { get; set; }
    }
}
