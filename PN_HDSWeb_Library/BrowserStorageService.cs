using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace PN_HDSWeb_Library
{
    /// <summary>
    /// Service quản lý localStorage an toàn.
    /// Mọi dữ liệu được mã hóa qua secureStorage.js (XOR + session key lưu trong sessionStorage).
    /// → Kẻ tấn công mở F12 chỉ thấy Base64 mã hóa, không thể đọc dữ liệu nhạy cảm.
    /// → Copy localStorage sang trình duyệt/máy khác sẽ không giải mã được.
    /// </summary>
    public class BrowserStorageService
    {
        /* =========================
         * CONSTANTS & CONFIG
         * ========================= */
        private const string SESSION_KEY_PREFIX = "UserSession_";
        private const string SHARED_TAB_ID_KEY = "SharedTabId";
        private const string OLD_SESSION_KEY = "UserSession"; // Key cũ cần xóa

        // Thời gian hết hạn mặc định (8 giờ)
        private static readonly TimeSpan SESSION_TIMEOUT = TimeSpan.FromHours(8);

        /* =========================
         * SHARED TAB ID MANAGEMENT
         * ========================= */

        /// <summary>
        /// Lấy hoặc tạo SharedTabId duy nhất cho tab hiện tại.
        /// SharedTabId KHÔNG mã hóa vì nó là ID định danh, không nhạy cảm.
        /// </summary>
        public static async Task<string> GetOrCreateSharedTabId(IJSRuntime jsRuntime)
        {
            try
            {
                // Gọi secureStorage.js để lấy/tạo TabId (không mã hóa TabId)
                var tabId = await jsRuntime.InvokeAsync<string>(
                    "secureStorage.getOrCreateSharedTabId");

                if (!string.IsNullOrWhiteSpace(tabId))
                {
                    Console.WriteLine($"[BrowserStorage] SharedTabId: {tabId}");
                    return tabId;
                }

                // Fallback nếu JS chưa load
                return $"tab_fallback_{DateTime.UtcNow.Ticks}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error getting SharedTabId: {ex.Message}");
                return $"tab_fallback_{DateTime.UtcNow.Ticks}";
            }
        }

        /// <summary>
        /// Tạo TabId mới sau khi đăng nhập (chống Session Fixation Attack).
        /// </summary>
        public static async Task<string> RegenerateTabId(IJSRuntime jsRuntime)
        {
            try
            {
                var newTabId = await jsRuntime.InvokeAsync<string>("secureStorage.regenerateTabId");
                Console.WriteLine($"[BrowserStorage] TabId regenerated: {newTabId}");
                return newTabId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error regenerating TabId: {ex.Message}");
                return $"tab_{DateTime.UtcNow.Ticks}";
            }
        }

        /// <summary>
        /// Xóa SharedTabId hiện tại (khi đăng xuất)
        /// </summary>
        public static async Task ClearSharedTabId(IJSRuntime jsRuntime)
        {
            await RemoveItem(jsRuntime, SHARED_TAB_ID_KEY);
        }

        /* =========================
         * BASIC STRING OPERATIONS (qua secureStorage - mã hóa)
         * ========================= */

        /// <summary>
        /// Đọc giá trị từ localStorage (tự động giải mã nếu dữ liệu đã được mã hóa).
        /// </summary>
        public static async ValueTask<string?> GetItem(
            IJSRuntime jsRuntime,
            string key)
        {
            try
            {
                // Dùng secureStorage thay vì localStorage trực tiếp
                return await jsRuntime.InvokeAsync<string?>(
                    "secureStorage.getItem", key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error getting item {key}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lưu giá trị vào localStorage (tự động mã hóa trước khi lưu).
        /// </summary>
        public static async ValueTask SetItem(
            IJSRuntime jsRuntime,
            string key,
            string value)
        {
            try
            {
                // Dùng secureStorage để mã hóa trước khi lưu
                await jsRuntime.InvokeVoidAsync(
                    "secureStorage.setItem", key, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error setting item {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa item khỏi localStorage.
        /// </summary>
        public static async ValueTask RemoveItem(
            IJSRuntime jsRuntime,
            string key)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync(
                    "secureStorage.removeItem", key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error removing item {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa tất cả dữ liệu trong localStorage (dùng khi cần reset hoàn toàn).
        /// Cảnh báo: sẽ xóa hết dữ liệu của tất cả ứng dụng.
        /// </summary>
        public static async ValueTask Clear(IJSRuntime jsRuntime)
        {
            try
            {
                // Chỉ xóa session liên quan, không xóa toàn bộ localStorage
                await jsRuntime.InvokeVoidAsync("secureStorage.clearAllSessions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error clearing storage: {ex.Message}");
            }
        }

        /* =========================
         * OBJECT (JSON) OPERATIONS
         * ========================= */

        public static async Task<T?> GetObjectAsync<T>(
            IJSRuntime jsRuntime,
            string key)
        {
            try
            {
                var json = await GetItem(jsRuntime, key);

                if (string.IsNullOrWhiteSpace(json))
                    return default;

                // Kiểm tra JSON hợp lệ trước khi deserialize
                if (IsValidJson(json))
                {
                    return JsonSerializer.Deserialize<T>(json);
                }
                else
                {
                    // Nếu JSON không hợp lệ, xóa key đó đi
                    Console.WriteLine($"[BrowserStorage] Invalid JSON for key {key}, removing it");
                    await RemoveItem(jsRuntime, key);
                    return default;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[BrowserStorage] JSON error for key {key}: {ex.Message}");
                // Xóa key lỗi để tránh lặp lại
                await RemoveItem(jsRuntime, key);
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error getting object {key}: {ex.Message}");
                return default;
            }
        }

        public static async Task SetObjectAsync<T>(
            IJSRuntime jsRuntime,
            string key,
            T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await SetItem(jsRuntime, key, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error setting object {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra chuỗi có phải JSON hợp lệ không
        /// </summary>
        private static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            json = json.Trim();
            if ((json.StartsWith("{") && json.EndsWith("}")) || // Đối tượng
                (json.StartsWith("[") && json.EndsWith("]")))   // Mảng
            {
                try
                {
                    JsonDocument.Parse(json);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        /* =========================
         * USER SESSION MANAGEMENT (CHỈ DÙNG SHAREDTABID)
         * ========================= */

        /// <summary>
        /// Lấy UserSession theo SharedTabId hiện tại
        /// </summary>
        public static async Task<UserSession?> GetUserSessionAsync(
            IJSRuntime jsRuntime)
        {
            try
            {
                // Bước 1: Cleanup key cũ nếu còn
                await CleanupOldSessionKey(jsRuntime);

                // Bước 2: Lấy SharedTabId
                var tabId = await GetOrCreateSharedTabId(jsRuntime);
                var sessionKey = SESSION_KEY_PREFIX + tabId;

                // Bước 3: Lấy session
                var session = await GetObjectAsync<UserSession>(jsRuntime, sessionKey);

                // Bước 4: Kiểm tra hết hạn
                if (session != null && session.ExpiryTime < DateTime.UtcNow)
                {
                    Console.WriteLine($"[BrowserStorage] Session expired for tab {tabId}");
                    await RemoveUserSessionAsync(jsRuntime);
                    return null;
                }

                return session;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error getting user session: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lưu UserSession theo SharedTabId hiện tại
        /// </summary>
        public static async Task SetUserSessionAsync(
            IJSRuntime jsRuntime,
            UserSession session)
        {
            try
            {
                // Đảm bảo session có ExpiryTime
                if (session.ExpiryTime == default)
                {
                    session.ExpiryTime = DateTime.UtcNow.Add(SESSION_TIMEOUT);
                }

                var tabId = await GetOrCreateSharedTabId(jsRuntime);
                var sessionKey = SESSION_KEY_PREFIX + tabId;

                await SetObjectAsync(jsRuntime, sessionKey, session);

                Console.WriteLine($"[BrowserStorage] Session saved for tab {tabId}, user: {session.MaUser}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error setting user session: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa UserSession hiện tại
        /// </summary>
        public static async Task RemoveUserSessionAsync(
            IJSRuntime jsRuntime)
        {
            try
            {
                var tabId = await GetOrCreateSharedTabId(jsRuntime);
                var sessionKey = SESSION_KEY_PREFIX + tabId;

                await RemoveItem(jsRuntime, sessionKey);

                Console.WriteLine($"[BrowserStorage] Session removed for tab {tabId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error removing user session: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup key cũ (UserSession không có hậu tố)
        /// </summary>
        public static async Task CleanupOldSessionKey(
            IJSRuntime jsRuntime)
        {
            try
            {
                var oldSession = await GetItem(jsRuntime, OLD_SESSION_KEY);
                if (!string.IsNullOrWhiteSpace(oldSession))
                {
                    // Nếu là JSON hợp lệ, migrate sang key mới
                    if (IsValidJson(oldSession))
                    {
                        try
                        {
                            var session = JsonSerializer.Deserialize<UserSession>(oldSession);
                            if (session != null)
                            {
                                await SetUserSessionAsync(jsRuntime, session);
                                Console.WriteLine($"[BrowserStorage] Migrated old session to new key format");
                            }
                        }
                        catch
                        {
                            // Không migrate được thì bỏ qua
                        }
                    }

                    // Xóa key cũ
                    await RemoveItem(jsRuntime, OLD_SESSION_KEY);
                    Console.WriteLine($"[BrowserStorage] Removed old UserSession key");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error cleaning up old session: {ex.Message}");
            }
        }

        /* =========================
         * USER SESSION HELPER METHODS
         * ========================= */

        public static async Task<string?> GetMaTruongBoAsync(
            IJSRuntime jsRuntime)
        {
            var session = await GetUserSessionAsync(jsRuntime);
            return session?.MaTruongBo;
        }

        public static async Task<string?> GetMaUserAsync(
            IJSRuntime jsRuntime)
        {
            var session = await GetUserSessionAsync(jsRuntime);
            return session?.MaUser;
        }

        public static async Task<string?> GetTenTruongAsync(
            IJSRuntime jsRuntime)
        {
            var session = await GetUserSessionAsync(jsRuntime);
            return session?.TenTruong;
        }

        public static async Task<string?> GetRoleAsync(
            IJSRuntime jsRuntime)
        {
            var session = await GetUserSessionAsync(jsRuntime);
            return session?.Role;
        }

        public static async Task<bool> IsSessionValidAsync(
            IJSRuntime jsRuntime)
        {
            var session = await GetUserSessionAsync(jsRuntime);
            return session != null && session.ExpiryTime > DateTime.UtcNow;
        }

        /* =========================
         * NAM HOC MANAGEMENT
         * ========================= */

        public static async Task<string?> GetNamHocAsync(
            IJSRuntime jsRuntime)
        {
            return await GetItem(jsRuntime, "currentNamHoc");
        }

        public static async Task SetNamHocAsync(
            IJSRuntime jsRuntime,
            string namHoc)
        {
            await SetItem(jsRuntime, "currentNamHoc", namHoc);
        }

        /* =========================
         * CẤP HỌC SELECTION
         * ========================= */

        public static async Task<string?> GetSelectedLevelAsync(
            IJSRuntime jsRuntime)
        {
            return await GetItem(jsRuntime, "selectedLevel");
        }

        public static async Task<string?> GetSelectedLevelTextAsync(
            IJSRuntime jsRuntime)
        {
            return await GetItem(jsRuntime, "selectedLevelText");
        }

        public static async Task SetSelectedLevelAsync(
            IJSRuntime jsRuntime,
            string levelValue,
            string levelText)
        {
            await SetItem(jsRuntime, "selectedLevel", levelValue);
            await SetItem(jsRuntime, "selectedLevelText", levelText);
        }

        public static async Task ClearSelectedLevelAsync(
            IJSRuntime jsRuntime)
        {
            await RemoveItem(jsRuntime, "selectedLevel");
            await RemoveItem(jsRuntime, "selectedLevelText");
        }

        /* =========================
         * UTILITY METHODS
         * ========================= */

        /// <summary>
        /// Xóa tất cả session keys và session key mã hóa — dùng khi đăng xuất.
        /// Sau khi gọi hàm này, mọi dữ liệu localStorage cũ trở nên không giải mã được.
        /// </summary>
        public static async Task ClearAllSessionsAsync(
            IJSRuntime jsRuntime)
        {
            try
            {
                // Gọi secureStorage.clearAllSessions() - xóa cả session key trong sessionStorage
                await jsRuntime.InvokeVoidAsync("secureStorage.clearAllSessions");
                Console.WriteLine($"[BrowserStorage] All sessions cleared (including encryption key)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error clearing all sessions: {ex.Message}");
            }
        }

        /* =========================
         * FINGERPRINT / USER-AGENT
         * ========================= */

        /// <summary>
        /// Lấy User-Agent string từ browser (dùng để tạo session fingerprint).
        /// </summary>
        public static async Task<string> GetUserAgentAsync(IJSRuntime jsRuntime)
        {
            try
            {
                return await jsRuntime.InvokeAsync<string>("secureStorage.getUserAgentInfo") ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}