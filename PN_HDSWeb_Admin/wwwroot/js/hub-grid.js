// ──────────────────────────────────────────────────────────
//  hub-grid.js  —  đo số card vừa vặn trên 1 hàng ngang
// ──────────────────────────────────────────────────────────

var _hubDotnetRef = null;
var _hubResizeTimer = null;
var _hubCardWidth = 230;   // px — phải khớp với CSS
var _hubCardGap = 18;      // px — 1.1rem ≈ 18px

/**
 * Khởi tạo: lưu dotnet ref, đo ngay lập tức, đăng ký resize.
 * @param {DotNetObjectReference} dotnetRef
 * @param {number} cardWidth  - chiều rộng 1 card (px)
 * @param {number} cardGap    - khoảng cách giữa các card (px)
 */
window.hubGridInit = function (dotnetRef, cardWidth, cardGap) {
    _hubDotnetRef = dotnetRef;
    if (cardWidth) _hubCardWidth = cardWidth;
    if (cardGap)   _hubCardGap  = cardGap;

    _hubDoMeasure();

    window.addEventListener('resize', function () {
        clearTimeout(_hubResizeTimer);
        _hubResizeTimer = setTimeout(_hubDoMeasure, 180);
    });
};

/**
 * Dọn dẹp khi component bị dispose.
 */
window.hubGridDispose = function () {
    _hubDotnetRef = null;
    clearTimeout(_hubResizeTimer);
};

/**
 * Đo tất cả các hub-grid có attribute [data-hub-grid].
 * Trả về object { catId: visibleCount }.
 */
window.hubMeasureGrids = function () {
    return _doMeasure();
};

// ── Private ──────────────────────────────────────────────
function _hubDoMeasure() {
    if (!_hubDotnetRef) return;
    var results = _doMeasure();
    _hubDotnetRef.invokeMethodAsync('OnGridMeasured', results);
}

function _doMeasure() {
    var results = {};
    var grids = document.querySelectorAll('[data-hub-grid]');
    grids.forEach(function (grid) {
        var id = grid.getAttribute('data-hub-grid');
        // Lấy chiều rộng từ parent (.hub-category) để tránh bị ảnh hưởng bởi overflow
        var parent = grid.closest('.hub-category') || grid.parentElement;
        var availableWidth = parent ? parent.clientWidth : grid.clientWidth;
        // Mỗi card chiếm: cardWidth + gap  (trừ card cuối không có gap sau)
        var fit = Math.max(1, Math.floor((availableWidth + _hubCardGap) / (_hubCardWidth + _hubCardGap)));
        results[id] = fit;
    });
    return results;
}
