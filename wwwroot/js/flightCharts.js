window.flightCharts = (function () {
    const instances = {};
    const chartGroup = "flight-charts-sync-group";

    function ensureChart(elementId) {
        const el = document.getElementById(elementId);
        if (!el) return null;

        let chart = instances[elementId];
        if (chart) return chart;

        chart = echarts.init(el, null, { renderer: "canvas" });
        chart.group = chartGroup;
        echarts.connect(chartGroup);

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

    function formatAxisTime(seconds) {
        const total = Math.max(0, Math.floor(seconds));
        const h = Math.floor(total / 3600);
        const m = Math.floor((total % 3600) / 60);
        return `${h}:${String(m).padStart(2, "0")}`;
    }

    function formatValue(value, unit) {
        if (value == null || Number.isNaN(value)) return `— ${unit}`;

        const decimals =
            unit === "m" ? 0 :
            unit === "km/h" ? 0 :
            1;

        return `${Number(value).toFixed(decimals).replace(".", ",")} ${unit}`;
    }

    function getFlightAxisInterval(maxSeconds) {
        if (maxSeconds <= 60 * 60) return 5 * 60;
        if (maxSeconds <= 3 * 60 * 60) return 10 * 60;
        if (maxSeconds <= 6 * 60 * 60) return 15 * 60;
        return 30 * 60;
    }

    function getRoundedAxisMax(maxSeconds, interval) {
        if (!maxSeconds || maxSeconds <= 0) return interval;
        return Math.ceil(maxSeconds / interval) * interval;
    }

    function getTargetPointCount(chart) {
        const width = Math.max(200, chart.getWidth ? chart.getWidth() : 800);
        return Math.max(500, Math.floor(width * 2));
    }

    function buildSeriesData(xValues, yValues, targetPointCount) {
        if (!xValues || !yValues) return [];

        const count = Math.min(xValues.length, yValues.length);
        if (count < 2) return [];

        const step = Math.max(1, Math.ceil(count / targetPointCount));

        if (step === 1) {
            const result = new Array(count);
            for (let i = 0; i < count; i++) {
                result[i] = [xValues[i], yValues[i]];
            }
            return result;
        }

        const reduced = [];
        for (let i = 0; i < count; i += step) {
            reduced.push([xValues[i], yValues[i]]);
        }

        const lastIndex = count - 1;
        const last = reduced[reduced.length - 1];
        if (!last || last[0] !== xValues[lastIndex]) {
            reduced.push([xValues[lastIndex], yValues[lastIndex]]);
        }

        return reduced;
    }

    function baseOption(title, unit, series, extra) {
        const maxX = series.length > 0 ? series[series.length - 1][0] : 0;
        const axisInterval = getFlightAxisInterval(maxX);
        const axisMax = getRoundedAxisMax(maxX, axisInterval);

        const option = {
            animation: false,
            axisPointer: {
                link: [{ xAxisIndex: "all" }]
            },
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
                    snap: true,
                    label: {
                        show: true
                    }
                },
                formatter: function (params) {
                    if (!params || params.length === 0) return "";

                    const p = params[0];
                    const y = Array.isArray(p.value) ? p.value[1] : null;

                    return formatValue(y, unit);
                }
            },
            xAxis: {
                type: "value",
                min: 0,
                max: axisMax,
                interval: axisInterval,
                axisLabel: {
                    color: "#94a3b8",
                    formatter: value => formatAxisTime(value)
                },
                axisPointer: {
                    show: true,
                    snap: true,
                    label: {
                        show: true,
                        formatter: function (params) {
                            return formatTime(params.value);
                        }
                    }
                },
                splitLine: {
                    show: false
                }
            },
            yAxis: {
                type: "value",
                axisLabel: {
                    color: "#64748b"
                },
                axisPointer: {
                    show: true,
                    label: {
                        show: true,
                        formatter: function (params) {
                            return formatValue(params.value, unit);
                        }
                    }
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

    function renderOne(elementId, title, unit, xValues, yValues, extra) {
        const chart = ensureChart(elementId);
        if (!chart || !xValues || !yValues) return;

        const targetPointCount = getTargetPointCount(chart);
        const series = buildSeriesData(xValues, yValues, targetPointCount);

        chart.setOption(baseOption(title, unit, series, extra), true);
        requestAnimationFrame(() => chart.resize());
    }

    function renderAll(altId, varioId, speedId, payload) {
        renderOne(
            altId,
            payload.altitudeTitle,
            payload.altitudeUnit,
            payload.timeSec,
            payload.altitudeValues,
            {
                lineStyle: { width: 2, color: "#2563eb" },
                areaStyle: { opacity: 0.08 }
            }
        );

        renderOne(
            varioId,
            payload.varioTitle,
            payload.varioUnit,
            payload.timeSec,
            payload.varioValues,
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
            payload.timeSec,
            payload.speedValues,
            {
                lineStyle: { width: 2, color: "#ea580c" }
            }
        );

        echarts.connect(chartGroup);
    }

    window.addEventListener("resize", () => {
        Object.values(instances).forEach(chart => chart.resize());
    });

    return {
        renderAll,
        dispose
    };
})();