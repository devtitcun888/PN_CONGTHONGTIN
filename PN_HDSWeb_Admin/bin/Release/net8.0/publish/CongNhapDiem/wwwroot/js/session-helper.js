window.sessionHelper = {
    saveSession: function (session) {
        sessionStorage.setItem("UserSession", JSON.stringify(session));
        localStorage.setItem("UserSession", JSON.stringify(session));

        // Dùng làm tín hiệu đồng bộ giữa các tab
        localStorage.setItem("SessionUpdatedAt", new Date().toISOString());
    },

    clearSession: function () {
        sessionStorage.removeItem("UserSession");
        sessionStorage.removeItem("TabId");

        localStorage.removeItem("UserSession");

        // BẮT BUỘC: trigger event cho các tab khác
        localStorage.setItem("ForceLogout", new Date().toISOString());
    },

    getUserSession: function () {
        return sessionStorage.getItem("UserSession")
            || localStorage.getItem("UserSession");
    },

    registerStorageListener: function (dotNetRef) {
        window.addEventListener("storage", function (event) {
            if (event.key === "ForceLogout") {
                dotNetRef.invokeMethodAsync("OnForceLogout");
            }
        });
    }
};
