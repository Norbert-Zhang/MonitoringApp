window.dashboardChart = {
    current: null,

    // =========================
    // NEW: generate distinct colors
    // =========================
    generateDistinctColors: function (count) {
        const colors = [];
        const saturation = 70;
        const lightness = 50;

        for (let i = 0; i < count; i++) {
            const hue = Math.round((360 / count) * i);
            colors.push(`hsl(${hue}, ${saturation}%, ${lightness}%)`);
        }

        return colors;
    },

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
        // assign non-repeating colors
        const colors = window.dashboardChart.generateDistinctColors(datasets.length);

        window.dashboardChart.current = new Chart(ctx, {
            type: chartType,
            data: {
                labels: labels,
                datasets: datasets.map((ds, index)=> ({
                    label: ds.label,
                    data: ds.data,
                    fill: false,
                    tension: 0.3,
                    borderWidth: 2,

                    // ===== color assignment =====
                    borderColor: colors[index],
                    backgroundColor:
                        chartType === "bar"
                            ? colors[index].replace(")", ", 0.6)").replace("hsl", "hsla")
                            : colors[index],

                    pointBackgroundColor: colors[index],
                    pointBorderColor: colors[index]
                }))
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                scales: {
                    y: {
                        title: {
                            display: true,
                            text: "Login Count",
                            font: {
                                size: 15,
                                weight: "bold"
                            },
                            /*color: "#000000"*/
                        }
                    }
                },
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
        const margin = 20;
        // Create a temporary canvas (with a background color)
        const tempCanvas = document.createElement("canvas");
        tempCanvas.width = canvas.width + margin * 2;
        tempCanvas.height = canvas.height + margin * 2;

        const ctx = tempCanvas.getContext("2d");

        // 1. First fill in the background color — you can change it to the background color of your page.
        ctx.fillStyle = getComputedStyle(document.body).backgroundColor || "#ffffff";
        ctx.fillRect(0, 0, tempCanvas.width, tempCanvas.height);

        // 2. Copy the original image to a new canvas
        ctx.drawImage(canvas, margin, margin);

        // 3. Export PNG (background color is now consistent)
        const imageURL = tempCanvas.toDataURL("image/png");

        const link = document.createElement("a");
        link.href = imageURL;
        link.download = "chart_" + new Date().toISOString().replace(/[:.]/g, "-") + ".png";
        link.click();
    }

};

window.downloadFileFromBytes = (fileName, contentType, bytes) => {
    const blob = new Blob([bytes], { type: contentType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.style.display = "none";
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

