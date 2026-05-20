window.adminSidebar = window.adminSidebar || {};

window.adminSidebar.init = function () {
    const STORAGE_KEY = 'admin_sidebar_collapsed';
    const shell = document.getElementById('adminShell');
    const collapseBtn = document.getElementById('sidebarCollapseBtn');
    const mobileBtn = document.getElementById('mobileToggle');
    const backdrop = document.getElementById('sidebarBackdrop');

    if (!shell || !collapseBtn || !mobileBtn || !backdrop) return;
    if (shell.dataset.sidebarReady === '1') return;

    shell.dataset.sidebarReady = '1';

    if (localStorage.getItem(STORAGE_KEY) === '1') {
        shell.classList.add('sidebar-collapsed');
    }

    collapseBtn.addEventListener('click', function () {
        const collapsed = shell.classList.toggle('sidebar-collapsed');
        localStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0');
    });

    function openMobile() {
        shell.classList.add('mobile-open');
        document.body.style.overflow = 'hidden';
    }

    function closeMobile() {
        shell.classList.remove('mobile-open');
        document.body.style.overflow = '';
    }

    mobileBtn.addEventListener('click', function () {
        if (shell.classList.contains('mobile-open')) {
            closeMobile();
        } else {
            openMobile();
        }
    });

    backdrop.addEventListener('click', closeMobile);

    document.addEventListener('click', function (event) {
        const link = event.target.closest('a[href]');
        if (link && shell.classList.contains('mobile-open')) {
            closeMobile();
        }
    });
};
