//window.setupDashboardCharts = () => {
//    // Kiểm tra nếu thư viện chưa load thì dừng
//    if (typeof Chart === 'undefined') return;

//    const existingChart1 = Chart.getChart("chartHK1");
//    if (existingChart1) existingChart1.destroy();

//    const existingChart2 = Chart.getChart("chartHK2");
//    if (existingChart2) existingChart2.destroy();

//    const existingChart3 = Chart.getChart("chartYearEnd");
//    if (existingChart3) existingChart3.destroy();

//    // Cấu hình chung
//    Chart.defaults.font.family = "'Segoe UI', Roboto, sans-serif";
//    const commonOptions = {
//        responsive: true,
//        maintainAspectRatio: false,
//        plugins: { legend: { position: 'bottom', labels: { usePointStyle: true, boxWidth: 8, padding: 15 } } },
//        scales: { y: { beginAtZero: true, grid: { color: '#f0f0f0' }, border: { display: false } }, x: { grid: { display: false }, border: { display: false } } }
//    };

//    // Chart 1: HK1
//    const ctx1 = document.getElementById('chartHK1');
//    if (ctx1) {
//        new Chart(ctx1, {
//            type: 'bar',
//            data: {
//                labels: ['K10', 'K11', 'K12'],
//                datasets: [
//                    { label: 'Hoàn thành', data: [100, 98, 100], backgroundColor: '#28a745', borderRadius: 4, barPercentage: 0.6 },
//                    { label: 'Đang nhập', data: [0, 2, 0], backgroundColor: '#ffc107', borderRadius: 4, barPercentage: 0.6 },
//                    { label: 'Chưa nhập', data: [0, 0, 0], backgroundColor: '#e9ecef', borderRadius: 4, barPercentage: 0.6 }
//                ]
//            },
//            options: { ...commonOptions, scales: { x: { stacked: true }, y: { stacked: true, max: 100, display: false } } }
//        });
//    }

//    // Chart 2: HK2
//    const ctx2 = document.getElementById('chartHK2');
//    if (ctx2) {
//        new Chart(ctx2, {
//            type: 'bar',
//            data: {
//                labels: ['K10', 'K11', 'K12'],
//                datasets: [
//                    { label: 'Hoàn thành', data: [30, 45, 20], backgroundColor: '#28a745', borderRadius: 4, barPercentage: 0.6 },
//                    { label: 'Đang nhập', data: [50, 40, 60], backgroundColor: '#ffc107', borderRadius: 4, barPercentage: 0.6 },
//                    { label: 'Chưa nhập', data: [20, 15, 20], backgroundColor: '#e9ecef', borderRadius: 4, barPercentage: 0.6 }
//                ]
//            },
//            options: { ...commonOptions, scales: { x: { stacked: true }, y: { stacked: true, max: 100, display: true } } }
//        });
//    }

//    // Chart 3: Cuối năm
//    const ctx3 = document.getElementById('chartYearEnd');
//    if (ctx3) {
//        new Chart(ctx3, {
//            type: 'bar',
//            data: {
//                labels: ['K10', 'K11', 'K12'],
//                datasets: [{ label: 'Dự kiến', data: [100, 100, 100], backgroundColor: '#f8f9fa', borderWidth: 1, borderColor: '#dee2e6', borderDash: [5, 5], borderRadius: 4 }]
//            },
//            options: { ...commonOptions, plugins: { legend: { display: false } }, scales: { y: { display: false }, x: { grid: { display: false }, border: { display: false } } } }
//        });
//    }
//};