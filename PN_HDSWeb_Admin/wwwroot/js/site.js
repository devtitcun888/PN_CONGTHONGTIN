// Storage event listener for cross-tab synchronization
(function() {
    // Listen for storage events
    window.addEventListener('storage', function(event) {
        if (event.key === 'UserSession' || event.key === 'SharedTabId') {
            // Notify Blazor
            if (window.dotNetReference) {
                window.dotNetReference.invokeMethodAsync('OnStorageChanged', event.key);
            }
        }
    });

    // Helper function to add storage event listener in Blazor
    window.addEventListenerStorage = function(dotNetRef) {
        window.dotNetReference = dotNetRef;
    };

    // Helper function to clear all auth storage
    window.clearAuthStorage = function() {
        localStorage.removeItem('UserSession');
        localStorage.removeItem('SharedTabId');
        localStorage.removeItem('SessionUpdateTimestamp');
    };

    // Check if there's an existing session
    window.hasExistingSession = function() {
        return localStorage.getItem('UserSession') !== null;
    };

    // Get shared TabId
    window.getSharedTabId = function() {
        let tabId = localStorage.getItem('SharedTabId');
        if (!tabId) {
            tabId = 'tab_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('SharedTabId', tabId);
        }
        return tabId;
    };
})();