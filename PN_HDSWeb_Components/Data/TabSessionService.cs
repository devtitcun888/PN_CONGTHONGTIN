using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using PN_HDSWeb_Library;

namespace PN_HDSWeb_Components.Data
{
    public class TabSessionService
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly ILogger<TabSessionService> _logger;
        private readonly IJSRuntime _jsRuntime;

        public TabSessionService(
            ProtectedSessionStorage sessionStorage,
            ILogger<TabSessionService> logger,
            IJSRuntime jsRuntime)
        {
            _sessionStorage = sessionStorage;
            _logger = logger;
            _jsRuntime = jsRuntime;
        }

        public async Task<string> GetOrCreateTabId()
        {
            try
            {
                // Ưu tiên lấy từ localStorage (shared giữa các tab)
                var sharedTabId = await _jsRuntime.InvokeAsync<string>(
                    "localStorage.getItem", "SharedTabId");

                if (!string.IsNullOrEmpty(sharedTabId))
                {
                    _logger.LogInformation($"Retrieved shared TabId: {sharedTabId}");

                    // Lưu vào session storage để backup
                    await _sessionStorage.SetAsync("TabId", sharedTabId);

                    return sharedTabId;
                }
                else
                {
                    // Tạo TabId mới và lưu vào localStorage
                    var newTabId = Guid.NewGuid().ToString();
                    await _jsRuntime.InvokeVoidAsync(
                        "localStorage.setItem",
                        "SharedTabId",
                        newTabId);

                    // Lưu vào session storage
                    await _sessionStorage.SetAsync("TabId", newTabId);

                    _logger.LogInformation($"Created new shared TabId: {newTabId}");
                    return newTabId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared TabId from localStorage");

                // Fallback: lấy từ session storage
                var result = await _sessionStorage.GetAsync<string>("TabId");
                if (result.Success)
                {
                    return result.Value;
                }
                else
                {
                    var newTabId = Guid.NewGuid().ToString();
                    await _sessionStorage.SetAsync("TabId", newTabId);
                    _logger.LogInformation($"Created new TabId (fallback): {newTabId}");
                    return newTabId;
                }
            }
        }

        public event Action OnSessionChanged;

        public async Task SetUserSession(string tabId, UserSession userSession)
        {
            // Lưu vào session storage với key riêng cho tab
            await _sessionStorage.SetAsync($"UserSession_{tabId}", userSession);

            // Cũng lưu vào key chung
            await _sessionStorage.SetAsync("UserSession", userSession);

            OnSessionChanged?.Invoke();
        }

        public async Task<UserSession> GetUserSession(string tabId)
        {
            // Ưu tiên lấy từ key chung
            var result = await _sessionStorage.GetAsync<UserSession>("UserSession");
            if (result.Success)
            {
                return result.Value;
            }

            // Fallback: lấy từ key riêng của tab
            result = await _sessionStorage.GetAsync<UserSession>($"UserSession_{tabId}");
            return result.Success ? result.Value : null;
        }

        public async Task ClearUserSession(string tabId)
        {
            // Xóa cả key chung và key riêng
            await _sessionStorage.DeleteAsync("UserSession");
            await _sessionStorage.DeleteAsync($"UserSession_{tabId}");
        }

        public async Task<string> GetCurrentUserName(string tabId)
        {
            var userSession = await GetUserSession(tabId);
            return userSession?.UserName ?? "Not logged in";
        }

        public async Task<bool> HasActiveSession()
        {
            try
            {
                // Kiểm tra trong localStorage (shared)
                var json = await _jsRuntime.InvokeAsync<string>(
                    "localStorage.getItem", "UserSession");

                if (!string.IsNullOrEmpty(json))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore errors
            }

            // Kiểm tra trong session storage
            var result = await _sessionStorage.GetAsync<UserSession>("UserSession");
            return result.Success && result.Value != null;
        }
    }
}