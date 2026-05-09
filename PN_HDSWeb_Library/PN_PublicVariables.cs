using hDataLibraryN8;
using hUltiLibraryN8;

namespace PN_HDSWeb_Library 
{
    public class PN_PublicVariables
    {
        public static readonly string pathService = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string pathAppConfigs = pathService + "app_configs";
        public static readonly string beFileConfig = pathAppConfigs + @"\backend_config.json";

        public static readonly string DiemConfig = pathAppConfigs + @"\diem_config.json";

        public static readonly string Token = hLibrary.hGetTextFromFile(hConstants.PN_LOGIN_TOKEN_FILE);
        public static readonly string Url_EduFeature = hJsonLib.hgetValueJF(hConstants.PN_CLIENT_CONFIG_FILE, "BE_SERVICE", "edufeature");
        public static readonly string Url_CoreService = hJsonLib.hgetValueJF(hConstants.PN_CLIENT_CONFIG_FILE, "CORE_SERVICE", "url1");
        public static readonly string Url_FileService = hJsonLib.hgetValueJF(beFileConfig, "FILE_SERVICE", "url1");
        public static readonly string Url_DataFileService = hJsonLib.hgetValueJF(beFileConfig, "FILE_SERVICE", "datafile");
        public static readonly string Url_DataFileService2 = hJsonLib.hgetValueJF(beFileConfig, "FILE_SERVICE", "datafile");
        public static readonly string Url_SSOFileService = hJsonLib.hgetValueJF(beFileConfig, "FILE_SERVICE", "datafile1");
        public static readonly string Url_Backend= hJsonLib.hgetValueJF(beFileConfig, "CAU_HINH", "url_backend");
        public static readonly string ConfigLoginId= hJsonLib.hgetValueJF(beFileConfig, "THONG_TIN", "login_name");
        public static readonly string CapHoc= hJsonLib.hgetValueJF(beFileConfig, "THONG_TIN", "cap_hoc");
        public static readonly string MaTruong= hJsonLib.hgetValueJF(beFileConfig, "THONG_TIN", "ma_truong");
        public static readonly string BieuPhiConn= hJsonLib.hgetValueJF(beFileConfig, "CAU_HINH", "bp_conn");
        public static readonly string IsBanTru= hJsonLib.hgetValueJF(beFileConfig, "CAU_HINH", "is_bantru");

        public static readonly string Hikvision_BaseUrl = hJsonLib.hgetValueJF(beFileConfig, "HIKVISION", "base_url");
        public static readonly string Hikvision_Username = hJsonLib.hgetValueJF(beFileConfig, "HIKVISION", "username");
        public static readonly string Hikvision_Password = hJsonLib.hgetValueJF(beFileConfig, "HIKVISION", "password");
        public static readonly string Hikvision_DeviceKey = hJsonLib.hgetValueJF(beFileConfig, "HIKVISION", "device_key");
        public static readonly string Hikvision_ProtocolType = hJsonLib.hgetValueJF(beFileConfig, "HIKVISION", "protocol_type");
        public static readonly string Hikvision_BaseUrlFace = hJsonLib.hgetValueJF(beFileConfig, "HIKVISION", "urlupload_face");
        //API SDB
        public static readonly string baseUrlAPI_SDB = "https://ketnoisogd.titkul.edu.vn";

        public static string IsKhoaSoDiem = hJsonLib.hgetValueJF(DiemConfig, "CAU_HINH", "khoasodiem");
        public static string IsKhoaSoDiemCKI = hJsonLib.hgetValueJF(DiemConfig, "CAU_HINH", "khoasodiemCKI");
        public PN_PublicVariables()
        {
        }
    }


}
