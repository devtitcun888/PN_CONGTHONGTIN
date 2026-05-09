// sessionHelper.js
window.sessionHelper = {
    // Lưu session vào cả sessionStorage và localStorage
    saveSession: function (session) {
        try {
            const sessionJson = JSON.stringify(session);

            // Lưu vào sessionStorage (cho tab hiện tại)
            sessionStorage.setItem("UserSession", sessionJson);
            sessionStorage.setItem("TabId", this.generateTabId());

            // Lưu vào localStorage (đồng bộ giữa các tab)
            localStorage.setItem("UserSession", sessionJson);

            // Tín hiệu đồng bộ
            localStorage.setItem("SessionUpdatedAt", new Date().toISOString());

            console.log("Đã lưu session");
            return true;
        } catch (error) {
            console.error("Lỗi khi lưu session:", error);
            return false;
        }
    },

    // Xóa tất cả session data
    clearSession: function () {
        try {
            // Xóa từ sessionStorage
            sessionStorage.removeItem("UserSession");
            sessionStorage.removeItem("TabId");
            sessionStorage.removeItem("SharedTabId");
            sessionStorage.removeItem("TabShare");

            // Xóa từ localStorage
            localStorage.removeItem("UserSession");
            localStorage.removeItem("TabId");
            localStorage.removeItem("SharedTabId");
            localStorage.removeItem("TabShare");

            // BẮT BUỘC: trigger event cho các tab khác
            localStorage.setItem("ForceLogout", new Date().toISOString());

            console.log("Đã xóa session và kích hoạt ForceLogout");
            return true;
        } catch (error) {
            console.error("Lỗi khi xóa session:", error);
            return false;
        }
    },

    // Lấy UserSession (ưu tiên sessionStorage trước)
    getUserSession: function () {
        return sessionStorage.getItem("UserSession") || localStorage.getItem("UserSession");
    },

    // Đăng ký listener cho storage events
    registerStorageListener: function (dotNetRef) {
        window.addEventListener("storage", function (event) {
            console.log("Storage event:", event.key, event.newValue);

            if (dotNetRef && dotNetRef.invokeMethodAsync) {
                if (event.key === "ForceLogout") {
                    dotNetRef.invokeMethodAsync("OnForceLogout");
                } else {
                    dotNetRef.invokeMethodAsync("OnStorageChanged", event.key, event.newValue || '');
                }
            }
        });
    },

    // Kiểm tra session có hợp lệ không
    checkSession: function () {
        try {
            const userSession = this.getUserSession();

            if (!userSession) {
                return { isValid: false, reason: 'No session found' };
            }

            const sessionData = JSON.parse(userSession);
            const expiryTime = new Date(sessionData.ExpiryTime);
            const currentTime = new Date();

            // Session hợp lệ nếu expiryTime LỚN HƠN currentTime
            const isValid = expiryTime > currentTime;

            return {
                isValid: isValid,
                expiryTime: expiryTime,
                currentTime: currentTime,
                remainingMs: expiryTime - currentTime,
                userName: sessionData.UserName
            };
        } catch (error) {
            return { isValid: false, reason: 'Error parsing session: ' + error.message };
        }
    },

    // Tạo TabId duy nhất
    generateTabId: function () {
        return 'tab-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
    },

    // Kiểm tra và xử lý session hết hạn
    checkAndHandleExpiredSession: function () {
        const checkResult = this.checkSession();

        if (!checkResult.isValid) {
            console.log("Session không hợp lệ:", checkResult.reason);
            this.clearSession();
            return false;
        }

        return true;
    }
};