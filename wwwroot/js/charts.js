window.flightCharts = {
    charts: {},

    getOrCreateChart(id) {
        const el = document.getElementById(id);
        if (!el) return null;

        if (!this.charts[id]) {
            this.charts[id] = echarts.init(el);
            window.addEventListener("resize", () => {
                if (this.charts[id]) this.charts[id].resize();
            });
        }

        return this.charts[id];
    },

    renderAltitudeChart(id, indices, altBaro, altGps) {
        const chart = this.getOrCreateChart(id);
        if (!chart) return;

        chart.setOption({
            animation: false,
            tooltip: { trigger: "axis" },
            legend: { data: ["Baro", "GPS"] },
            grid: { left: 50, right: 20, top: 30, bottom: 40 },
            xAxis: { type: "value", name: "Index" },
            yAxis: { type: "value", name: "m" },
            dataZoom: [
                { type: "inside", xAxisIndex: 0 },
                { type: "slider", xAxisIndex: 0 }
            ],
            series: [
                {
                    name: "Baro",
                    type: "line",
                    showSymbol: false,
                    sampling: "minmax",
                    data: indices.map((x, i) => [x, altBaro[i]])
                },
                {
                    name: "GPS",
                    type: "line",
                    showSymbol: false,
                    sampling: "minmax",
                    data: indices.map((x, i) => [x, altGps[i]])
                }
            ]
        }, true);
    },

    renderVarioChart(id, indices, vario) {
        const chart = this.getOrCreateChart(id);
        if (!chart) return;

        chart.setOption({
            animation: false,
            tooltip: { trigger: "axis" },
            grid: { left: 50, right: 20, top: 20, bottom: 40 },
            xAxis: { type: "value", name: "Index" },
            yAxis: { type: "value", name: "m/s" },
            dataZoom: [
                { type: "inside", xAxisIndex: 0 },
                { type: "slider", xAxisIndex: 0 }
            ],
            series: [
                {
                    name: "Vario",
                    type: "line",
                    showSymbol: false,
                    sampling: "minmax",
                    data: indices.map((x, i) => [x, vario[i]])
                }
            ]
        }, true);
    },

    renderSpeedChart(id, indices, speed) {
        const chart = this.getOrCreateChart(id);
        if (!chart) return;

        chart.setOption({
            animation: false,
            tooltip: { trigger: "axis" },
            grid: { left: 50, right: 20, top: 20, bottom: 40 },
            xAxis: { type: "value", name: "Index" },
            yAxis: { type: "value", name: "km/h" },
            dataZoom: [
                { type: "inside", xAxisIndex: 0 },
                { type: "slider", xAxisIndex: 0 }
            ],
            series: [
                {
                    name: "Speed",
                    type: "line",
                    showSymbol: false,
                    sampling: "minmax",
                    data: indices.map((x, i) => [x, speed[i]])
                }
            ]
        }, true);
    }
};