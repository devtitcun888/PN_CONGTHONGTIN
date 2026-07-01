/* =============================================
   Admin Sidebar Controller
   - Desktop: collapse/expand toggle
   - Mobile: off-canvas open/close
   ============================================= */
(function () {
    'use strict';

    var STORAGE_KEY = 'admin_sidebar_collapsed';

    function getEls() {
        return {
            shell:       document.getElementById('adminShell'),
            collapseBtn: document.getElementById('sidebarCollapseBtn'),
            mobileBtn:   document.getElementById('mobileToggle'),
            backdrop:    document.getElementById('sidebarBackdrop')
        };
    }

    function isMobile() {
        return window.innerWidth <= 768;
    }

    function openMobile(shell) {
        shell.classList.add('sidebar-open');
        document.body.style.overflow = 'hidden';
    }

    function closeMobile(shell) {
        shell.classList.remove('sidebar-open');
        document.body.style.overflow = '';
    }

    function initSidebar() {
        var els = getEls();
        if (!els.shell) return false;

        // Prevent double-init
        if (els.shell.dataset.sidebarReady === '1') return true;
        els.shell.dataset.sidebarReady = '1';

        // Restore collapsed state on desktop
        if (!isMobile() && localStorage.getItem(STORAGE_KEY) === '1') {
            els.shell.classList.add('sidebar-collapsed');
        }

        // Desktop: collapse button
        if (els.collapseBtn) {
            els.collapseBtn.addEventListener('click', function () {
                var collapsed = els.shell.classList.toggle('sidebar-collapsed');
                localStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0');
            });
        }

        // Mobile: hamburger open/close
        if (els.mobileBtn) {
            els.mobileBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                if (els.shell.classList.contains('sidebar-open')) {
                    closeMobile(els.shell);
                } else {
                    openMobile(els.shell);
                }
            });
        }

        // Close on backdrop click
        if (els.backdrop) {
            els.backdrop.addEventListener('click', function () {
                closeMobile(els.shell);
            });
        }

        // Close when nav link is clicked on mobile
        document.addEventListener('click', function (e) {
            if (!els.shell.classList.contains('sidebar-open')) return;
            var link = e.target.closest('a[href], button.admin-nav-item');
            if (link) closeMobile(els.shell);
        });

        // Close on resize to desktop
        window.addEventListener('resize', function () {
            if (!isMobile()) closeMobile(els.shell);
        });

        console.log('[AdminSidebar] Initialized OK');
        return true;
    }

    // ── Expose for Blazor JS Interop ─────────────────
    window.adminSidebar = {
        init: function () {
            // Try immediately; if elements not ready, retry once after short delay
            if (!initSidebar()) {
                setTimeout(function () {
                    if (!initSidebar()) {
                        console.warn('[AdminSidebar] Elements not found after retry.');
                    }
                }, 300);
            }
        }
    };

    // ── Auto-init on DOMContentLoaded (fallback) ────
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            setTimeout(initSidebar, 100);
        });
    } else {
        setTimeout(initSidebar, 100);
    }

})();
