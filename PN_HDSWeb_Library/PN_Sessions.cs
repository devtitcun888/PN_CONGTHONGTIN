using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PN_HDSWeb_Library
{
    public class PN_Sessions
    {

        public static string MaTruongBo { get; set; } = PN_PublicVariables.MaTruong;//Cát lái
        public static string CapHoc { get; set; } = PN_PublicVariables.CapHoc;


        public static string MaUser { get; set; } = default!; 
        public static string Role { get; set; } = default!;
        public static string IdUser { get; set; } = default!;
        public static string? TaiKhoanKySo { get; set; } 
        public static string? MaHocKy { get; set; } 
        public static string? NamHoc{ get; set; }

    }
}
