window.AppUtils = {
    charts: {}, 

    // 1. Khởi tạo Chart ban đầu
    initCharts: () => {
        if (typeof Chart === 'undefined') return;

        Chart.defaults.font.family = "'Segoe UI', Roboto, sans-serif";
        const commonOptions = {
            responsive: true, maintainAspectRatio: false,
            plugins: { legend: { position: 'bottom', labels: { usePointStyle: true, boxWidth: 8, padding: 15 } } },
            scales: { y: { beginAtZero: true, grid: { color: '#f0f0f0' }, border: { display: false } }, x: { grid: { display: false }, border: { display: false } } }
        };

        const createBarChart = (id, dataDone, dataEmpty) => {
            const ctx = document.getElementById(id);
            if (!ctx) return;

            if (window.AppUtils.charts[id]) window.AppUtils.charts[id].destroy();

            window.AppUtils.charts[id] = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: ['K10', 'K11', 'K12'],
                    datasets: [
                        {
                            label: 'Đã nhập',
                            data: dataDone,
                            backgroundColor: '#1b6ec2', 
                            borderRadius: 4, barPercentage: 0.6
                        },
                        {
                            label: 'Chưa nhập',
                            data: dataEmpty,
                            backgroundColor: '#e9ecef',
                            borderRadius: 4, barPercentage: 0.6
                        }
                    ]
                },
                options: { ...commonOptions, scales: { x: { stacked: true }, y: { stacked: true, display: false } } }
            });
        };

        // Dữ liệu giả lập ban đầu
        createBarChart('chartHK1', [98, 95, 100], [2, 5, 0]);
        createBarChart('chartHK2', [30, 45, 20], [70, 55, 80]);

        const ctx3 = document.getElementById('chartYearEnd');
        if (ctx3) {
            if (window.AppUtils.charts['chartYearEnd']) window.AppUtils.charts['chartYearEnd'].destroy();
            window.AppUtils.charts['chartYearEnd'] = new Chart(ctx3, {
                type: 'bar',
                data: { labels: ['K10', 'K11', 'K12'], datasets: [{ label: 'Dự kiến', data: [100, 100, 100], backgroundColor: '#f8f9fa', borderWidth: 1, borderColor: '#dee2e6', borderDash: [5, 5], borderRadius: 4 }] },
                options: { ...commonOptions, plugins: { legend: { display: false } }, scales: { y: { display: false }, x: { grid: { display: false } } } }
            });
        }
    },

    // 2. Hàm cập nhật dữ liệu khi bấm Tab
    updateChartData: (id, dataDone, dataEmpty) => {
        const chart = window.AppUtils.charts[id];
        if (chart) {
            chart.data.datasets[0].data = dataDone;
            chart.data.datasets[1].data = dataEmpty;
            chart.update();
        }
    },

    // 3. Điều hướng bảng điểm
    enableExcelNav: (tableId) => {
        const table = document.getElementById(tableId);
        if (!table) return;
        table.addEventListener('keydown', (e) => {
            if (!['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Enter'].includes(e.key)) return;
            const active = document.activeElement;
            if (!active.classList.contains('cell-input')) return;
            e.preventDefault();
            let r = parseInt(active.getAttribute('data-r'));
            let c = parseInt(active.getAttribute('data-c'));
            if (e.key === 'ArrowUp') r--;
            if (e.key === 'ArrowDown' || e.key === 'Enter') r++;
            if (e.key === 'ArrowLeft') c--;
            if (e.key === 'ArrowRight') c++;
            const next = table.querySelector(`input[data-r="${r}"][data-c="${c}"]`);
            if (next) { next.focus(); next.select(); }
        });
    },

    copyToClipboard: async (text) => {
        const value = text || window.location.href;

        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(value);
            } else {
                const input = document.createElement('textarea');
                input.value = value;
                input.setAttribute('readonly', '');
                input.style.position = 'fixed';
                input.style.top = '-1000px';
                document.body.appendChild(input);
                input.select();
                document.execCommand('copy');
                document.body.removeChild(input);
            }

            return 'Đã sao chép';
        } catch (err) {
            console.error('Copy failed:', err);
            return 'Không thể sao chép';
        }
    },

    shareArticle: async (title, text, url) => {
        const shareUrl = url || window.location.href;

        try {
            if (navigator.share) {
                await navigator.share({
                    title: title || document.title,
                    text: text || '',
                    url: shareUrl
                });
                return 'Đã chia sẻ';
            }

            return await window.AppUtils.copyToClipboard(shareUrl);
        } catch (err) {
            if (err && err.name === 'AbortError') return '';
            console.error('Share failed:', err);
            return await window.AppUtils.copyToClipboard(shareUrl);
        }
    },

    shareFacebook: (url) => {
        const shareUrl = encodeURIComponent(url || window.location.href);
        window.AppUtils.openShareWindow(`https://www.facebook.com/sharer/sharer.php?u=${shareUrl}`);
    },

    shareZalo: (url) => {
        const shareUrl = encodeURIComponent(url || window.location.href);
        window.AppUtils.openShareWindow(`https://zalo.me/share?u=${shareUrl}`);
    },

    openShareWindow: (url) => {
        const width = 640;
        const height = 560;
        const left = Math.max(0, (window.screen.width - width) / 2);
        const top = Math.max(0, (window.screen.height - height) / 2);
        const popup = window.open(
            url,
            'share-window',
            `width=${width},height=${height},left=${left},top=${top},noopener,noreferrer`
        );

        if (popup) popup.focus();
    }
};

document.addEventListener("error", function (event) {
    const target = event.target;
    if (!(target instanceof HTMLImageElement)) return;

    const fallbackSrc = target.dataset.fallbackSrc;
    if (!fallbackSrc || target.dataset.fallbackApplied === "true") return;

    target.dataset.fallbackApplied = "true";
    target.src = fallbackSrc;
}, true);
