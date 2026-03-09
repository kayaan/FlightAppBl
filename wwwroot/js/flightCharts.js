window.flightCharts = window.flightCharts || (function () {
    const instances = {};

    function ensureChart(elementId) {
        const el = document.getElementById(elementId);
        if (!el) return null;

        let chart = instances[elementId];
        if (chart) return chart;

        chart = echarts.init(el, null, { renderer: "canvas" });
        instances[elementId] = chart;
        return chart;
    }

    function dispose(elementId) {
        const chart = instances[elementId];
        if (!chart) return;

        chart.dispose();
        delete instances[elementId];
    }

    function formatTime(seconds) {
        const total = Math.max(0, Math.floor(seconds));
        const h = Math.floor(total / 3600);
        const m = Math.floor((total % 3600) / 60);
        const s = total % 60;

        if (h > 0) {
            return `${h}:${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`;
        }

        return `${m}:${String(s).padStart(2, "0")}`;
    }

    function baseOption(title, unit, series, extra) {
        const option = {
            animation: false,
            grid: {
                left: 52,
                right: 14,
                top: 28,
                bottom: 28
            },
            title: {
                text: title,
                left: 12,
                top: 8,
                textStyle: {
                    fontSize: 12,
                    fontWeight: 600,
                    color: "#64748b"
                }
            },
            tooltip: {
                trigger: "axis",
                confine: true,
                axisPointer: {
                    type: "line",
                    snap: true
                },
                formatter: function (params) {
                    if (!params || params.length === 0) return "";

                    const p = params[0];
                    const x = Array.isArray(p.value) ? p.value[0] : null;
                    const y = Array.isArray(p.value) ? p.value[1] : null;

                    return `${formatTime(x)}<br/>${y?.toFixed(1) ?? "—"} ${unit}`;
                }
            },
            xAxis: {
                type: "value",
                axisLabel: {
                    color: "#94a3b8",
                    formatter: value => formatTime(value)
                },
                splitLine: {
                    show: false
                }
            },
            yAxis: {
                type: "value",
                axisLabel: {
                    color: "#64748b",
                    formatter: value => `${value}`
                },
                splitLine: {
                    lineStyle: {
                        color: "#f1f5f9"
                    }
                }
            },
            dataZoom: [
                {
                    type: "inside",
                    xAxisIndex: 0,
                    filterMode: "none"
                }
            ],
            series: [
                {
                    type: "line",
                    showSymbol: false,
                    smooth: false,
                    sampling: "lttb",
                    data: series,
                    lineStyle: extra.lineStyle,
                    areaStyle: extra.areaStyle ?? undefined
                }
            ]
        };

        if (extra.markLine) {
            option.series[0].markLine = extra.markLine;
        }

        return option;
    }

    function renderOne(elementId, title, unit, series, extra) {
        const chart = ensureChart(elementId);
        if (!chart) return;

        chart.setOption(baseOption(title, unit, series, extra), true);
        requestAnimationFrame(() => chart.resize());
    }

    function renderAll(altId, varioId, speedId, payload) {
        renderOne(
            altId,
            payload.altitudeTitle,
            payload.altitudeUnit,
            payload.altitudeSeries,
            {
                lineStyle: { width: 2, color: "#2563eb" },
                areaStyle: { opacity: 0.08 }
            }
        );

        renderOne(
            varioId,
            payload.varioTitle,
            payload.varioUnit,
            payload.varioSeries,
            {
                lineStyle: { width: 2, color: "#7c3aed" },
                markLine: {
                    silent: true,
                    symbol: "none",
                    lineStyle: {
                        color: "#cbd5e1",
                        width: 1,
                        type: "dashed"
                    },
                    data: [{ yAxis: 0 }]
                }
            }
        );

        renderOne(
            speedId,
            payload.speedTitle,
            payload.speedUnit,
            payload.speedSeries,
            {
                lineStyle: { width: 2, color: "#ea580c" }
            }
        );
    }

    window.addEventListener("resize", () => {
        Object.values(instances).forEach(chart => chart.resize());
    });

    return {
        renderAll,
        dispose
    };
})();