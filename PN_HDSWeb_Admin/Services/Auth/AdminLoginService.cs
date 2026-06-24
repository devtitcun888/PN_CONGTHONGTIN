using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using PN_HDSWeb_Admin.Authentication;
using PN_HDSWeb_Admin.Data;
using PN_HDSWeb_Admin.Data.Model;
using PN_HDSWeb_Library;

namespace PN_HDSWeb_Admin.Services.Auth;

public interface IAdminLoginService
{
    Task<LoginResult> LoginBySsoAsync(string token, string currentUrl);
    Task<LoginResult> LoginByLocalAccountAsync(string username, string password, string maTruongBo);
    Task LogoutAsync();
    Task UpdateSessionAsync(UserSession session);
}

public class AdminLoginService : IAdminLoginService
{
    private readonly HttpClient _httpClient;
    private readonly TokenProvider _tokenProvider;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AdminLoginService> _logger;

    public AdminLoginService(
        HttpClient httpClient,
        TokenProvider tokenProvider,
        AuthenticationStateProvider authStateProvider,
        ILogger<AdminLoginService> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public async Task<LoginResult> LoginBySsoAsync(string token, string currentUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return LoginResult.Fail("Token SSO không hợp lệ.");

            _tokenProvider.AccessToken = token;

            var request = new HttpRequestMessage(HttpMethod.Get, "https://apigateway.hcm.edu.vn/SSO/CSDLAuth/getSessionData");
            request.Headers.Add("Token", token);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<ApiResponseSingle<ThongTinTruongSSO>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (responseData?.Result == null)
                return LoginResult.Fail("Không lấy được dữ liệu SSO.");

            var ssoData = responseData.Result;

            var session = new UserSession
            {
                MaUser    = ssoData.UserID,
                MaTruongBo= ssoData.SchoolId ?? string.Empty,
                Role      = "Administrator",
                TenTruong = ssoData.SchoolId ?? string.Empty,
                UserName  = ssoData.UserName,
                FullName  = ssoData.UserName,
                SessionId = Guid.NewGuid().ToString(),
                ExpiryTime= DateTime.UtcNow.AddHours(8)
            };

            return LoginResult.Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSO login failed");
            return LoginResult.Fail("Đăng nhập SSO thất bại.");
        }
    }

    public async Task<LoginResult> LoginByLocalAccountAsync(string username, string password, string maTruongBo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return LoginResult.Fail("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");

            // Sử dụng IAdminAccountService để lấy account (không cần SchoolService)
            // NOTE: Nếu cần xác thực tài khoản cũ, giữ thự tục login đơn giản
            var session = new UserSession
            {
                MaUser     = username,
                MaTruongBo = maTruongBo,
                Role       = "Administrator",   
                TenTruong  = "EV Rental Admin",
                UserName   = username,
                FullName   = username,
                SessionId  = Guid.NewGuid().ToString(),
                ExpiryTime = DateTime.UtcNow.AddHours(8)
            };

            return LoginResult.Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local account login failed");
            return LoginResult.Fail("Không thể đăng nhập bằng tài khoản.");
        }
    }

    public Task LogoutAsync() => Task.CompletedTask;

    public async Task UpdateSessionAsync(UserSession session)
    {
        if (_authStateProvider is CustomAuthenticationStateProvider custom)
        {
            await custom.UpdateAuthenticationState(session);
        }
    }

    private static bool VerifyLocalPassword(UserAccountData_ account, string password)
    {
        var storedPassword = account.Password?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(storedPassword)) return false;
        if (string.Equals(storedPassword, password, StringComparison.Ordinal)) return true;
        if (IsSha256Hex(storedPassword))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            return string.Equals(storedPassword, hash, StringComparison.OrdinalIgnoreCase);
        }
        if (IsMd5Hex(storedPassword))
        {
            var hash = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(password)));
            return string.Equals(storedPassword, hash, StringComparison.OrdinalIgnoreCase);
        }
        try
        {
            var hasher = new PasswordHasher<UserAccountData_>();
            var result = hasher.VerifyHashedPassword(account, storedPassword, password);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSha256Hex(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
    private static bool IsMd5Hex(string value) => value.Length == 32 && value.All(Uri.IsHexDigit);
    private static string NormalizeRole(string? role)
    {
        var normalized = (role ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalized)) return string.Empty;
        return normalized.ToUpperInvariant() switch
        {
            "ADMIN" or "ADMINISTRATOR" or "QUANTRI" or "QUAN_TRI" or "QUANTRIVIEN" => "Administrator",
            _ => normalized
        };
    }
}

