window.flightCharts = {
    altitudeChart: null,

    renderAltitudeChart: function (elementId, indices, altBaro, altGps) {
        const el = document.getElementById(elementId);
        if (!el) return;

        if (!this.altitudeChart) {
            this.altitudeChart = echarts.init(el);
            window.addEventListener("resize", () => {
                if (this.altitudeChart) {
                    this.altitudeChart.resize();
                }
            });
        }

        const baroData = indices.map((x, i) => [x, altBaro[i]]);
        const gpsData = indices.map((x, i) => [x, altGps[i]]);

        const option = {
            animation: false,
            tooltip: {
                trigger: "axis"
            },
            legend: {
                data: ["Baro", "GPS"]
            },
            grid: {
                left: 50,
                right: 20,
                top: 40,
                bottom: 40
            },
            xAxis: {
                type: "value",
                name: "Index"
            },
            yAxis: {
                type: "value",
                name: "Altitude (m)"
            },
            dataZoom: [
                { type: "inside", xAxisIndex: 0 },
                { type: "slider", xAxisIndex: 0 }
            ],
            series: [
                {
                    name: "Baro",
                    type: "line",
                    showSymbol: false,
                    sampling: "min-max",
                    data: baroData
                },
                {
                    name: "GPS",
                    type: "line",
                    showSymbol: false,
                    sampling: "min-max",
                    data: gpsData
                }
            ]
        };

        this.altitudeChart.setOption(option, true);
    }
};