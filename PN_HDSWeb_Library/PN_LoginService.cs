using hDataLibraryN8;

namespace PN_HDSWeb_Library
{
    public static class PN_LoginService
    {
        public static readonly string LoginID_System = hdataLib.hgetLoginID(hConstants.dbSystem); 
        public static readonly string LoginID_CongDiem = hdataLib.hgetLoginID("9200_tk_congnhapdiem");
        public static readonly string LoginID_CongThongTin = hdataLib.hgetLoginID("9200_tk_congthongtin");


        public static readonly string LoginID_School_Dev = hdataLib.hgetLoginID($"{PN_PublicVariables.ConfigLoginId}");

    }
}
