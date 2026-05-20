(function () {
    const helper = window.sessionHelper || {};

    helper.validateSession = helper.validateSession || function () {
        try {
            const oldSession = localStorage.getItem("UserSession");
            if (oldSession) {
                try {
                    JSON.parse(oldSession);
                    const tabId = localStorage.getItem("SharedTabId");
                    if (tabId) {
                        localStorage.setItem("UserSession_" + tabId, oldSession);
                    }
                } catch (e) {
                    // Keep compatibility with existing session payloads.
                }
                localStorage.removeItem("UserSession");
            }

            if (!localStorage.getItem("SharedTabId")) {
                const newTabId = "tab_" + Date.now() + "_" + Math.random().toString(36).substr(2, 9);
                localStorage.setItem("SharedTabId", newTabId);
            }
        } catch (e) {
            console.error("Error in validateSession:", e);
        }
    };

    helper.saveSession = function (session) {
        const serializedSession = JSON.stringify(session);
        sessionStorage.setItem("UserSession", serializedSession);
        localStorage.setItem("UserSession", serializedSession);
        localStorage.setItem("SessionUpdatedAt", new Date().toISOString());
    };

    helper.clearSession = function () {
        try {
            const tabId = localStorage.getItem("SharedTabId");
            if (tabId) {
                localStorage.removeItem("UserSession_" + tabId);
            }

            sessionStorage.removeItem("UserSession");
            sessionStorage.removeItem("TabId");
            localStorage.removeItem("UserSession");
            localStorage.removeItem("selectedLevel");
            localStorage.removeItem("selectedLevelText");
            localStorage.setItem("ForceLogout", new Date().toISOString());
        } catch (e) {
            console.error("Error clearing session:", e);
        }
    };

    helper.getUserSession = function () {
        return sessionStorage.getItem("UserSession")
            || localStorage.getItem("UserSession");
    };

    helper.registerStorageListener = function (dotNetRef) {
        window.addEventListener("storage", function (event) {
            if (event.key === "ForceLogout") {
                dotNetRef.invokeMethodAsync("OnForceLogout");
            }
        });
    };

    window.sessionHelper = helper;
})();
