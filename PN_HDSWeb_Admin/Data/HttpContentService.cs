using Microsoft.AspNetCore.Components;
using PN_HDSWeb_Library;
using System.Net.Http;
using System.Threading.Tasks;
namespace PN_HDSWeb_Admin.Data
{
    public class HttpContentService
    {
        private readonly HttpClient _httpClient;

        public HttpContentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetHttpContent(string url)
        {
            return await _httpClient.GetStringAsync(url);
        }
        public async Task<GetSSOLoginUrl> HandleDirectToSSO(int ishocsinh, string previousUrl,string ma_truong_bo)
        {
            try
            {
                bool IsHocSinh = ishocsinh == 1;

                var url = "https://apigateway.hcm.edu.vn/SSO/CSDLAuth/loginsso";
                GetSSOLoginUrl responseData = new GetSSOLoginUrl();

                var requestbody = new
                {
                    sysUserName = "TIT",
                    sysPassword = "NGe4DlO9st#$j",
                    param1 = ma_truong_bo,
                    param2 = "new",
                    param3 = "",
                    returnuri = previousUrl,
                    isHocSinh = IsHocSinh
                };

                // ✅ 1. Xóa header cũ (tránh trùng)
                _httpClient.DefaultRequestHeaders.Clear();

                // ✅ 2. Thêm header xác thực (tùy hệ thống Kong Gateway yêu cầu)
                // Nếu dùng API key:
                _httpClient.DefaultRequestHeaders.Add("apikey", ma_truong_bo);
                // Nếu hệ thống yêu cầu Bearer Token (trong 1 số môi trường):
                // _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", PN_Sessions.MaTruongBo);

                // ✅ 3. Gửi request
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, requestbody);

                // ✅ 4. Xử lý phản hồi
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GetSSOLoginUrl>();
                }
                else
                {
                    var errorMess = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"SSO request failed: {response.StatusCode} - {errorMess}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SSO request: {ex.Message}");
                throw;
            }
        }

    }
}
