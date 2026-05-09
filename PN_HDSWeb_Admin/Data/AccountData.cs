using System.ComponentModel.DataAnnotations;

namespace PN_HDSWeb_Admin.Data
{
    public class AccountData
    {

        [Required(AllowEmptyStrings = false, ErrorMessage = "Vui lòng nhập tên tài khoản")]
        public string? UserName { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string? Password { get; set; }
        public string? UserNameSSO { get; set; }


    }
    public class UserAccountData
    {
        public string UserName { get; set; } = default!;
        public string UserNameSSO { get; set; } = default!;
        public int IdUser { get; set; }
        public string Password { get; set; } = default!;
        public string Roles { get; set; } = default!;
        public string MaTruongBo { get; set; } = default!;
        public string DeviceName { get; set; } = default!;
    }


    #region DangNhapDiem


    #endregion



}
