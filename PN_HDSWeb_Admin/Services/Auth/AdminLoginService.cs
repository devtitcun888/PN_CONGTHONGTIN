using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using PN_HDSWeb_Admin.Authentication;
using PN_HDSWeb_Admin.Data;
using PN_HDSWeb_Admin.Data.Model;
using PN_HDSWeb_Admin.Services.Admin;
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
    private readonly IUserAccountService _userAccountService;
    private readonly IAdminAccountService _adminAccountService;
    private readonly ILogger<AdminLoginService> _logger;

    public AdminLoginService(
        HttpClient httpClient,
        TokenProvider tokenProvider,
        AuthenticationStateProvider authStateProvider,
        IUserAccountService userAccountService,
        IAdminAccountService adminAccountService,
        ILogger<AdminLoginService> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _authStateProvider = authStateProvider;
        _userAccountService = userAccountService;
        _adminAccountService = adminAccountService;
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
            return await LoginByVerifiedLocalAccountAsync(username, password, maTruongBo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local account login failed");
            return LoginResult.Fail("Khong the dang nhap bang tai khoan.");
        }
    }
    public Task LogoutAsync() => Task.CompletedTask;

    private async Task<LoginResult> LoginByVerifiedLocalAccountAsync(string username, string password, string maTruongBo)
    {
        var loginName = username?.Trim() ?? string.Empty;
        var schoolCode = maTruongBo?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(password))
            return LoginResult.Fail("Vui long nhap day du ten dang nhap va mat khau.");

        if (string.IsNullOrWhiteSpace(schoolCode))
            return LoginResult.Fail("Khong xac dinh duoc ma truong.");

        var account = await _userAccountService.GetLocalAccountAsync(schoolCode, loginName);
        if (account == null || !VerifyLocalPassword(account, password))
            return LoginResult.Fail("Ten dang nhap hoac mat khau khong dung.");

        if (!account.IsActive)
            return LoginResult.Fail("Tai khoan da bi tam ngung.");

        if (account.IsLocked)
        {
            var reason = string.IsNullOrWhiteSpace(account.LockReason) ? string.Empty : $": {account.LockReason}";
            return LoginResult.Fail($"Tai khoan dang bi khoa{reason}.");
        }

        var role = NormalizeRole(account.Roles);
        if (!string.Equals(role, "Administrator", StringComparison.Ordinal))
            return LoginResult.Fail("Tai khoan khong co quyen quan tri.");

        var displayName = FirstNonEmpty(account.DisplayName, account.FullName, account.UserName, loginName);
        var session = new UserSession
        {
            MaUser     = FirstNonEmpty(account.Id, account.UserName, loginName),
            MaTruongBo = FirstNonEmpty(account.MaTruongBo, schoolCode),
            Role       = role,
            TenTruong  = FirstNonEmpty(account.MaTruongBo, schoolCode),
            UserName   = FirstNonEmpty(account.UserName, loginName),
            FullName   = displayName,
            SessionId  = Guid.NewGuid().ToString(),
            ExpiryTime = DateTime.UtcNow.AddHours(8)
        };

        if (!string.IsNullOrWhiteSpace(account.Id))
        {
            try
            {
                await _adminAccountService.UpdateLastLoginAsync(account.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not update last login for account {AccountId}", account.Id);
            }
        }

        return LoginResult.Ok(session);
    }

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
        if (IsBCryptHash(storedPassword))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedPassword);
            }
            catch
            {
                return false;
            }
        }
        if (!IsBase64Hash(storedPassword))
        {
            return false;
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
    private static bool IsBCryptHash(string value)
        => value.Length >= 59
           && (value.StartsWith("$2a$", StringComparison.Ordinal)
               || value.StartsWith("$2b$", StringComparison.Ordinal)
               || value.StartsWith("$2x$", StringComparison.Ordinal)
               || value.StartsWith("$2y$", StringComparison.Ordinal));

    private static bool IsBase64Hash(string value)
    {
        Span<byte> buffer = stackalloc byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

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

