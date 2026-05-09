using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using PN_HDSWeb_Components.Data;
using PN_HDSWeb_Library;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;

namespace PN_HDSWeb_Admin.Authentication;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly IJSRuntime _jsRuntime;
    private readonly UserState _userState;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;

    private string? _cachedTabId;
    private UserSession? _cachedUserSession;
    private AuthenticationState? _cachedAuthState;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);
    private bool _isInitialized;

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
        if (_cachedUserSession != null &&
            _cachedAuthState != null &&
            DateTime.UtcNow - _lastCheckTime < _cacheDuration)
        {
            return _cachedAuthState;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
                _isInitialized = true;
            }

            if (_cachedUserSession != null &&
                _cachedAuthState != null &&
                DateTime.UtcNow - _lastCheckTime < _cacheDuration)
            {
                return _cachedAuthState;
            }

            var tabId = await GetSharedTabId();
            UserSession? userSession = null;

            try
            {
                var sessionJson = await _jsRuntime.InvokeAsync<string>("secureStorage.getItem", $"UserSession_{tabId}");
                if (!string.IsNullOrEmpty(sessionJson))
                {
                    userSession = JsonSerializer.Deserialize<UserSession>(sessionJson);
                    if (userSession != null)
                    {
                        if (userSession.ExpiryTime < DateTime.UtcNow)
                        {
                            _logger.LogInformation("Session expired for tab: {TabId}", tabId);
                            await ClearAuthenticationStateInternal(tabId);
                            userSession = null;
                        }
                        else
                        {
                            var currentUserAgent = await GetCurrentUserAgent();
                            if (!userSession.IsValidFingerprint(currentUserAgent))
                            {
                                _logger.LogWarning("Session fingerprint mismatch for tab {TabId}", tabId);
                                await ClearAuthenticationStateInternal(tabId);
                                userSession = null;
                            }
                            else if (userSession.ExpiryTime > DateTime.UtcNow.AddMinutes(30))
                            {
                                userSession.ExpiryTime = DateTime.UtcNow.AddHours(8);
                                await PersistSessionToStorage(userSession, tabId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading session from secureStorage");
            }

            AuthenticationState authState;
            if (userSession == null)
            {
                authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                _cachedUserSession = null;
                _cachedAuthState = authState;
                _userState.ClearSession();
            }
            else
            {
                var claims = CreateClaims(userSession);
                var identity = new ClaimsIdentity(claims, "CustomAuth");
                authState = new AuthenticationState(new ClaimsPrincipal(identity));
                _cachedUserSession = userSession;
                _cachedAuthState = authState;

                if (_userState.CurrentUser == null || _userState.CurrentUser.MaUser != userSession.MaUser)
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
            await GetSharedTabId();
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
            var allKeys = await _jsRuntime.InvokeAsync<string[]>("eval", @"(function() { 
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
                    var sessionJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                    if (!string.IsNullOrEmpty(sessionJson))
                    {
                        var session = JsonSerializer.Deserialize<UserSession>(sessionJson);
                        if (session != null && session.ExpiryTime < now)
                        {
                            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
                        }
                    }
                }
                catch
                {
                }
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
            var newTabId = await _jsRuntime.InvokeAsync<string>("secureStorage.regenerateTabId");
            _cachedTabId = newTabId;

            userSession.ExpiryTime = DateTime.UtcNow.AddHours(8);
            userSession.SessionId = Guid.NewGuid().ToString();
            userSession.LoginTime = DateTime.UtcNow;

            var userAgent = await GetCurrentUserAgent();
            userSession.UserAgentHash = UserSession.CreateUserAgentHash(userAgent);
            userSession.BrowserInfo = userAgent.Length > 200 ? userAgent[..200] : userAgent;

            await PersistSessionToStorage(userSession, newTabId);

            _cachedUserSession = userSession;
            _cachedAuthState = null;
            _lastCheckTime = DateTime.MinValue;

            await _userState.InitializeAsync(userSession);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
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
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearAuthenticationStateSilently()
    {
        await _semaphore.WaitAsync();
        try
        {
            var tabId = await GetSharedTabId();
            await ClearAuthenticationStateInternal(tabId);
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
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"UserSession_{tabId}");
            _cachedUserSession = null;
            _cachedAuthState = null;
            _lastCheckTime = DateTime.MinValue;
            _userState.ClearSession();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session for tab: {TabId}", tabId);
        }
    }

    private async Task PersistSessionToStorage(UserSession userSession, string tabId)
    {
        var json = JsonSerializer.Serialize(userSession);
        await _jsRuntime.InvokeVoidAsync("secureStorage.setItem", $"UserSession_{tabId}", json);
    }

    private async Task<string> GetSharedTabId()
    {
        if (!string.IsNullOrEmpty(_cachedTabId))
        {
            return _cachedTabId;
        }

        try
        {
            var tabId = await _jsRuntime.InvokeAsync<string>("secureStorage.getOrCreateSharedTabId");
            _cachedTabId = tabId;

            try
            {
                await _sessionStorage.SetAsync("SharedTabId_Backup", tabId);
            }
            catch
            {
            }

            return tabId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SharedTabId");

            try
            {
                var backupResult = await _sessionStorage.GetAsync<string>("SharedTabId_Backup");
                if (backupResult.Success && !string.IsNullOrEmpty(backupResult.Value))
                {
                    _cachedTabId = backupResult.Value;
                    return _cachedTabId;
                }
            }
            catch
            {
            }

            _cachedTabId = $"tab_{DateTime.UtcNow.Ticks}";
            return _cachedTabId;
        }
    }

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
            new(ClaimTypes.Name, userSession.UserName ?? string.Empty),
            new(ClaimTypes.Role, userSession.Role ?? string.Empty),
            new("MaUser", userSession.MaUser ?? string.Empty),
            new("MaTruongBo", userSession.MaTruongBo ?? string.Empty),
            new("TenTruong", userSession.TenTruong ?? string.Empty),
            new("SessionId", userSession.SessionId ?? string.Empty),
            new("ExpiryTime", userSession.ExpiryTime.Ticks.ToString())
        };

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

    public async Task<bool> IsUserAuthenticated()
    {
        var authState = await GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated == true;
    }

    public async Task<UserSession?> GetCurrentUserSession()
    {
        if (_cachedUserSession != null && _cachedUserSession.ExpiryTime > DateTime.UtcNow)
        {
            return _cachedUserSession;
        }

        _cachedAuthState = null;
        await GetAuthenticationStateAsync();
        return _cachedUserSession;
    }
}
