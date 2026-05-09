using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using PN_HDSWeb_Components.Data;
using PN_HDSWeb_Library;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;

namespace PN_HDSWeb_Admin.Authentication
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly IJSRuntime _jsRuntime;
        private readonly UserState _userState;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;

        private string _cachedTabId;
        private UserSession _cachedUserSession;
        private AuthenticationState _cachedAuthState; // Thêm biến này
        private DateTime _lastCheckTime = DateTime.MinValue;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);
        private bool _isInitialized = false;

        public CustomAuthenticationStateProvider(
            ProtectedSessionStorage sessionStorage,
            IJSRuntime jsRuntime,
            UserState userState,
            ILogger<CustomAuthenticationStateProvider> logger)
        {
            _sessionStorage = sessionStorage;
            _jsRuntime = jsRuntime;
            _userState = userState;
            _logger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Sử dụng cache để tránh xử lý quá nhiều lần
            if (_cachedUserSession != null &&
                _cachedAuthState != null &&
                DateTime.UtcNow - _lastCheckTime < _cacheDuration)
            {
                return _cachedAuthState;
            }

            await _semaphore.WaitAsync();
            try
            {
                // Khởi tạo nếu cần
                if (!_isInitialized)
                {
                    await InitializeAsync();
                    _isInitialized = true;
                }

                // Kiểm tra cache một lần nữa sau khi có semaphore
                if (_cachedUserSession != null &&
                    _cachedAuthState != null &&
                    DateTime.UtcNow - _lastCheckTime < _cacheDuration)
                {
                    return _cachedAuthState;
                }

                // Lấy SharedTabId từ localStorage
                var tabId = await GetSharedTabId();
                UserSession userSession = null;

                // Chỉ đọc từ localStorage (shared giữa các tab)
                try
                {
                    var sessionJson = await _jsRuntime.InvokeAsync<string>(
                        "secureStorage.getItem",
                        $"UserSession_{tabId}");

                    if (!string.IsNullOrEmpty(sessionJson))
                    {
                        userSession = JsonSerializer.Deserialize<UserSession>(sessionJson);

                        // Kiểm tra session còn hiệu lực
                        if (userSession != null)
                        {
                            // Kiểm tra hết hạn
                            if (userSession.ExpiryTime < DateTime.UtcNow)
                            {
                                _logger.LogInformation("Session expired for tab: {TabId}", tabId);
                                await ClearAuthenticationStateInternal(tabId);
                                userSession = null;
                            }
                            else
                            {
                                // ✅ Kiểm tra Fingerprint (User-Agent)
                                var currentUserAgent = await GetCurrentUserAgent();
                                if (!userSession.IsValidFingerprint(currentUserAgent))
                                {
                                    _logger.LogWarning(
                                        "Session fingerprint mismatch for tab {TabId}. Possible session hijacking!", tabId);
                                    await ClearAuthenticationStateInternal(tabId);
                                    userSession = null;
                                }
                                else if (userSession.ExpiryTime > DateTime.UtcNow.AddMinutes(30))
                                {
                                    // Gia hạn thêm nếu còn hoạt động
                                    userSession.ExpiryTime = DateTime.UtcNow.AddHours(8);
                                    await PersistSessionToStorage(userSession, tabId);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading session from localStorage");
                }

                // Tạo AuthenticationState
                AuthenticationState authState;
                if (userSession == null)
                {
                    authState = new AuthenticationState(
                        new ClaimsPrincipal(new ClaimsIdentity()));

                    // Clear cache
                    _cachedUserSession = null;
                    _cachedAuthState = authState;

                    // Clear UserState
                    _userState.ClearSession();
                }
                else
                {
                    var claims = CreateClaims(userSession);
                    var identity = new ClaimsIdentity(claims, "CustomAuth");
                    var user = new ClaimsPrincipal(identity);
                    authState = new AuthenticationState(user);

                    // Update cache
                    _cachedUserSession = userSession;
                    _cachedAuthState = authState;

                    // Initialize UserState (chỉ khi thực sự cần)
                    if (_userState.CurrentUser == null ||
                        _userState.CurrentUser.MaUser != userSession.MaUser)
                    {
                        await _userState.InitializeAsync(userSession);
                    }
                }

                _lastCheckTime = DateTime.UtcNow;
                return authState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAuthenticationStateAsync");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Đảm bảo SharedTabId tồn tại
                await GetSharedTabId();

                // Cleanup các session cũ
                await CleanupOldSessions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InitializeAsync");
            }
        }

        private async Task CleanupOldSessions()
        {
            try
            {
                var allKeys = await _jsRuntime.InvokeAsync<string[]>(
                    "eval",
                    @"(function() { 
                        try { 
                            var keys = [];
                            for (var i = 0; i < localStorage.length; i++) {
                                var key = localStorage.key(i);
                                if (key && key.startsWith('UserSession_')) {
                                    keys.push(key);
                                }
                            }
                            return keys;
                        } catch(e) { return []; }
                    })()");

                var now = DateTime.UtcNow;
                foreach (var key in allKeys)
                {
                    try
                    {
                        var sessionJson = await _jsRuntime.InvokeAsync<string>(
                            "localStorage.getItem", key);

                        if (!string.IsNullOrEmpty(sessionJson))
                        {
                            var session = JsonSerializer.Deserialize<UserSession>(sessionJson);
                            if (session != null && session.ExpiryTime < now)
                            {
                                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
                                _logger.LogInformation("Removed expired session: {Key}", key);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old sessions");
            }
        }

        public async Task UpdateAuthenticationState(UserSession userSession)
        {
            if (userSession == null)
            {
                await ClearAuthenticationState();
                return;
            }

            await _semaphore.WaitAsync();
            try
            {
                // ✅ FIX Session Fixation: Tạo TabId mới sau khi đăng nhập
                // Ngăn kẻ tấn công preset TabId trước đăng nhập để chiếm session
                var newTabId = await _jsRuntime.InvokeAsync<string>("secureStorage.regenerateTabId");
                _cachedTabId = newTabId;

                // Thiết lập thời gian hết hạn
                userSession.ExpiryTime = DateTime.UtcNow.AddHours(8);
                userSession.SessionId = Guid.NewGuid().ToString();
                userSession.LoginTime = DateTime.UtcNow;

                // ✅ Thêm Fingerprint: lưu User-Agent hash để validate mỗi request
                var userAgent = await GetCurrentUserAgent();
                userSession.UserAgentHash = UserSession.CreateUserAgentHash(userAgent);
                userSession.BrowserInfo = userAgent.Length > 200 ? userAgent[..200] : userAgent;

                // Lưu vào localStorage (tự động mã hóa qua secureStorage.js)
                await PersistSessionToStorage(userSession, newTabId);

                // Update cache
                _cachedUserSession = userSession;
                _cachedAuthState = null; // Force refresh
                _lastCheckTime = DateTime.MinValue;

                // Update UserState
                await _userState.InitializeAsync(userSession);

                // Notify state change
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

                _logger.LogInformation("Session updated for user: {UserId}, tab: {TabId}",
                    userSession.MaUser, newTabId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearAuthenticationState()
        {
            await _semaphore.WaitAsync();
            try
            {
                var tabId = await GetSharedTabId();
                await ClearAuthenticationStateInternal(tabId);

                // Notify state change
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

                _logger.LogInformation("Session cleared for tab: {TabId}", tabId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Xóa session mà không notify AuthenticationStateChanged.
        /// Dùng trong login page OnInitializedAsync để clear stale session
        /// mà không gây Blazor re-render loop.
        /// </summary>
        public async Task ClearAuthenticationStateSilently()
        {
            await _semaphore.WaitAsync();
            try
            {
                var tabId = await GetSharedTabId();
                await ClearAuthenticationStateInternal(tabId);
                _logger.LogInformation("Session cleared silently for tab: {TabId}", tabId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ClearAuthenticationStateInternal(string tabId)
        {
            try
            {
                // Xóa session từ localStorage
                await _jsRuntime.InvokeVoidAsync(
                    "localStorage.removeItem",
                    $"UserSession_{tabId}");

                // Clear cache
                _cachedUserSession = null;
                _cachedAuthState = null;
                _lastCheckTime = DateTime.MinValue;

                // Clear UserState
                _userState.ClearSession();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing session for tab: {TabId}", tabId);
            }
        }

        private async Task PersistSessionToStorage(UserSession userSession, string tabId)
        {
            try
            {
                var json = JsonSerializer.Serialize(userSession);
                // Dùng secureStorage.setItem (mã hóa) thay vì localStorage.setItem (plaintext)
                await _jsRuntime.InvokeVoidAsync(
                    "secureStorage.setItem",
                    $"UserSession_{tabId}",
                    json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving session to secureStorage");
                throw;
            }
        }

        private async Task<string> GetSharedTabId()
        {
            if (!string.IsNullOrEmpty(_cachedTabId))
            {
                return _cachedTabId;
            }

            try
            {
                // Dùng secureStorage.js để lấy TabId (không mã hóa TabId)
                var tabId = await _jsRuntime.InvokeAsync<string>(
                    "secureStorage.getOrCreateSharedTabId");

                _cachedTabId = tabId;

                // Lưu vào session storage để backup
                try
                {
                    await _sessionStorage.SetAsync("SharedTabId_Backup", tabId);
                }
                catch { }

                return tabId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SharedTabId");

                // Thử lấy từ session storage backup
                try
                {
                    var backupResult = await _sessionStorage.GetAsync<string>("SharedTabId_Backup");
                    if (backupResult.Success && !string.IsNullOrEmpty(backupResult.Value))
                    {
                        _cachedTabId = backupResult.Value;
                        return _cachedTabId;
                    }
                }
                catch { }

                // Fallback: tạo ID tạm thời
                _cachedTabId = $"tab_{DateTime.UtcNow.Ticks}";
                return _cachedTabId;
            }
        }

        /// <summary>
        /// Lấy User-Agent hiện tại từ browser.
        /// </summary>
        private async Task<string> GetCurrentUserAgent()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("secureStorage.getUserAgentInfo") ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private List<Claim> CreateClaims(UserSession userSession)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.UserName ?? ""),
                new Claim(ClaimTypes.Role, userSession.Role ?? ""),
                new Claim("MaUser", userSession.MaUser ?? ""),
                new Claim("MaTruongBo", userSession.MaTruongBo ?? ""),
                new Claim("TenTruong", userSession.TenTruong ?? ""),
                new Claim("SessionId", userSession.SessionId ?? ""),
                new Claim("ExpiryTime", userSession.ExpiryTime.Ticks.ToString())
            };

            // Xử lý Cap array
            if (userSession.Cap != null && userSession.Cap.Any())
            {
                claims.Add(new Claim("Cap", string.Join(",", userSession.Cap)));
            }

            if (!string.IsNullOrEmpty(userSession.TenTruong))
            {
                claims.Add(new Claim("Organization", userSession.TenTruong));
            }

            return claims;
        }

        // Helper method để kiểm tra session hiện tại
        public async Task<bool> IsUserAuthenticated()
        {
            var authState = await GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated == true;
        }

        // Lấy UserSession hiện tại
        public async Task<UserSession> GetCurrentUserSession()
        {
            if (_cachedUserSession != null && _cachedUserSession.ExpiryTime > DateTime.UtcNow)
            {
                return _cachedUserSession;
            }

            // Force refresh
            _cachedAuthState = null;
            await GetAuthenticationStateAsync();
            return _cachedUserSession;
        }
    }
}