window.publicHeader = window.publicHeader || {};

window.publicHeader.init = function (dotNetRef) {
    if (!dotNetRef) return;

    let ticking = false;
    let isCollapsed = null;

    const getBannerHeight = () => {
        const banner = document.querySelector('.public-header-banner');
        if (!banner) return 220;
        return Math.max(180, banner.scrollHeight || banner.offsetHeight || 220);
    };

    const getThresholds = () => {
        const bannerHeight = getBannerHeight();
        return {
            collapseAt: bannerHeight + 80,
            expandAt: Math.max(80, bannerHeight - 80)
        };
    };

    const update = () => {
        const y = window.scrollY || window.pageYOffset || 0;
        const thresholds = getThresholds();
        const next = isCollapsed === true ? y > thresholds.expandAt : y > thresholds.collapseAt;

        if (next !== isCollapsed) {
            isCollapsed = next;
            dotNetRef.invokeMethodAsync('SetHeaderCollapsed', next);
        }
    };

    const onScroll = () => {
        if (ticking) return;
        ticking = true;
        window.requestAnimationFrame(() => {
            update();
            ticking = false;
        });
    };

    update();
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('resize', onScroll, { passive: true });
};
