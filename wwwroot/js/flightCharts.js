window.flightCharts = (function () {
    const instances = {};
    const chartGroup = "flight-charts-sync-group";

    let hoverThrottleMs = 25;
    let lastHoverUpdateMs = 0;
    let lastTrackIndex = -1;

    let mapTrackLatE7 = null;
    let mapTrackLonE7 = null;
    let mapCursorMarker = null;

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

    function buildSeriesData(xValues, yValues) {
        if (!xValues || !yValues) return [];

        const count = Math.min(xValues.length, yValues.length);
        if (count < 2) return [];

        const result = new Array(count);

        for (let i = 0; i < count; i++) {
            result[i] = [xValues[i], yValues[i], i];
        }

        return result;
    }

    function registerMapCursor(trackLatE7, trackLonE7, marker) {
        mapTrackLatE7 = trackLatE7;
        mapTrackLonE7 = trackLonE7;
        mapCursorMarker = marker;
        lastTrackIndex = -1;
    }

    function moveMapCursorToTrackIndex(trackIndex) {
        if (!mapCursorMarker || !mapTrackLatE7 || !mapTrackLonE7) return;
        if (trackIndex == null || trackIndex < 0) return;
        if (trackIndex >= mapTrackLatE7.length || trackIndex >= mapTrackLonE7.length) return;

        const lat = mapTrackLatE7[trackIndex] / 1e7;
        const lon = mapTrackLonE7[trackIndex] / 1e7;

        mapCursorMarker.setLatLng([lat, lon]);
    }

    function baseOption(title, unit, series, extra) {
        const maxX = series.length > 0 ? series[series.length - 1][0] : 0;
        const axisInterval = getFlightAxisInterval(maxX);
        const axisMax = getRoundedAxisMax(maxX, axisInterval);

        const option = {
            animation: false,
            axisPointer: {
                link: [{ xAxisIndex: "all" }],
                triggerTooltip: false
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
                show: true,
                trigger: "axis",
                confine: true,
                axisPointer: {
                    type: "line",
                    snap: true,
                    label: {
                        show: false
                    }
                },
                formatter: function (params) {
                    if (!params || params.length === 0) return "";

                    const now = Date.now();
                    const p = params[0];

                    let trackIndex = null;

                    if (Array.isArray(p.value) && p.value.length >= 3) {
                        trackIndex = p.value[2];
                    } else if (typeof p.dataIndex === "number") {
                        trackIndex = p.dataIndex;
                    }

                    if (
                        trackIndex != null &&
                        !Number.isNaN(trackIndex) &&
                        trackIndex !== lastTrackIndex &&
                        now - lastHoverUpdateMs >= hoverThrottleMs
                    ) {
                        lastHoverUpdateMs = now;
                        lastTrackIndex = trackIndex;
                        moveMapCursorToTrackIndex(trackIndex);
                    }

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
                    show: false
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
                    showAllSymbol: false,
                    symbol: "none",
                    smooth: false,
                    triggerLineEvent: false,
                    emphasis: {
                        disabled: true
                    },
                    data: series,
                    lineStyle: extra.lineStyle,
                    areaStyle: extra.areaStyle ?? undefined,
                    progressive: 5000,
                    progressiveThreshold: 10000
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

        const series = buildSeriesData(xValues, yValues);

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
        dispose,
        registerMapCursor
    };
})();