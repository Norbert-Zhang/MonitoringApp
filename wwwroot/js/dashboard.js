window.dashboardChart = {
    current: null,

    renderChart: function (canvasId, labels, datasets, chartType) {
        // ---- Completely destroy all charts bound to this canvas. ----
        if (window.dashboardChart.current) {
            try {
                window.dashboardChart.current.destroy();
            } catch { }
        }

        // Chart.js has a caching mechanism and saves charts in Chart.instances.
        // We force-clear all old instances to prevent the Canvas from being occupied
        if (Chart.instances) {
            for (let id in Chart.instances) {
                const chart = Chart.instances[id];
                if (chart && chart.canvas && chart.canvas.id === canvasId) {
                    try { chart.destroy(); } catch { }
                }
            }
        }

        // ---- Create a new chart ----
        const ctx = document.getElementById(canvasId).getContext('2d');  //const canvas = document.getElementById(canvasId);
        window.dashboardChart.current = new Chart(ctx, {
            type: chartType,
            data: {
                labels: labels,
                datasets: datasets.map(ds => ({
                    label: ds.label,
                    data: ds.data,
                    fill: false,
                    tension: 0.3,
                    borderWidth: 2,
                }))
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { display: true }
                }
            }
        });
    },

    // Export Chart
    exportChart: function (canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error("Canvas not found:", canvasId);
            return;
        }

        // Create a temporary canvas (with a background color)
        const tempCanvas = document.createElement("canvas");
        tempCanvas.width = canvas.width;
        tempCanvas.height = canvas.height;

        const ctx = tempCanvas.getContext("2d");

        // 1. First fill in the background color — you can change it to the background color of your page.
        ctx.fillStyle = getComputedStyle(document.body).backgroundColor || "#ffffff";
        ctx.fillRect(0, 0, tempCanvas.width, tempCanvas.height);

        // 2. Copy the original image to a new canvas
        ctx.drawImage(canvas, 0, 0);

        // 3. Export PNG (background color is now consistent)
        const imageURL = tempCanvas.toDataURL("image/png");

        const link = document.createElement("a");
        link.href = imageURL;
        link.download = "chart_" + new Date().toISOString().replace(/[:.]/g, "-") + ".png";
        link.click();
    }

};
