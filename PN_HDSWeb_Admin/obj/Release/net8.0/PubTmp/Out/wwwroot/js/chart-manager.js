window.chartInstances = {};
window.chartInstances = {};

// Cập nhật: Thêm tham số lateValue
window.initializeChart = function (canvasId, presentValue, lateValue, absentValue) {
    try {
        const canvas = document.getElementById(canvasId);

        if (!canvas) {
            console.error(`Canvas element not found: ${canvasId}`);
            return;
        }

        // Destroy existing chart if it exists
        if (window.chartInstances[canvasId]) {
            try {
                window.chartInstances[canvasId].destroy();
            } catch (e) {
                console.warn(`Error destroying chart ${canvasId}:`, e);
            }
        }

        canvas.style.display = 'block';
        const ctx = canvas.getContext('2d');

        if (!ctx) return;

        window.chartInstances[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Có mặt', 'Đi trễ', 'Vắng'],
                datasets: [{
                    data: [presentValue, lateValue, absentValue],
                    backgroundColor: ['#22c55e', '#eab308', '#ef4444'],
                    borderColor: ['#22c55e', '#eab308', '#ef4444'],
                    borderWidth: 0,
                    cutout: '75%'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: true }
                },
                animation: {
                    animateRotate: true,
                    animateScale: false
                }
            }
        });

    } catch (error) {
        console.error(`Error initializing chart ${canvasId}:`, error);
    }
};