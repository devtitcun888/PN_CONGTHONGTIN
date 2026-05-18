// wwwroot/js/adminSidebar.js
export function initSidebar() {
    const STORAGE_KEY = 'admin_sidebar_collapsed';
    const shell = document.getElementById('adminShell');
    const collapseBtn = document.getElementById('sidebarCollapseBtn');
    const mobileBtn = document.getElementById('mobileToggle');
    const backdrop = document.getElementById('sidebarBackdrop');

    if (!shell || !collapseBtn || !mobileBtn || !backdrop) return;

    // ── Khôi phục trạng thái đã lưu ──
    if (localStorage.getItem(STORAGE_KEY) === '1') {
        shell.classList.add('sidebar-collapsed');
    }

    // ── Desktop: thu gọn / mở rộng ──
    collapseBtn.addEventListener('click', () => {
        const collapsed = shell.classList.toggle('sidebar-collapsed');
        localStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0');
    });

    // ── Mobile: mở / đóng sidebar ──
    const openMobile = () => {
        shell.classList.add('mobile-open');
        document.body.style.overflow = 'hidden';
    };

    const closeMobile = () => {
        shell.classList.remove('mobile-open');
        document.body.style.overflow = '';
    };

    mobileBtn.addEventListener('click', () => {
        shell.classList.contains('mobile-open') ? closeMobile() : openMobile();
    });

    // Click backdrop để đóng
    backdrop.addEventListener('click', closeMobile);

    // Tự đóng khi điều hướng (click link)
    document.addEventListener('click', (e) => {
        if (e.target.closest('a[href]') && shell.classList.contains('mobile-open')) {
            closeMobile();
        }
    });
}