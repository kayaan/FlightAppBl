window.flightCharts = (function () {

    const instances = {};
    const chartGroup = "flight-charts-sync-group";
    const alpha = 0.02;
    const colors = [
        "#2563eb",
        "#16a34a",
        "#ea580c",
        "#9333ea",
        "#0891b2",
        "#dc2626"
    ];

    let hoverThrottleMs = 25;
    let lastHoverUpdateMs = 0;
    let lastTrackIndex = -1;

    let mapTrackLatE7 = null;
    let mapTrackLonE7 = null;
    let mapCursorMarker = null;

    let suppressChartToMap = false;
    let suppressMapToChart = false;

    let selectionCallback = null;
    let lastSelectionKey = null;

    let isSyncingBrush = false;

    function hexToRgba(hex, alpha) {
        const r = parseInt(hex.substring(1, 3), 16);
        const g = parseInt(hex.substring(3, 5), 16);
        const b = parseInt(hex.substring(5, 7), 16);

        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    }

    function computeEma(values, alpha) {
        if (!values || values.length === 0) return [];

        const result = new Array(values.length);
        result[0] = values[0];

        for (let i = 1; i < values.length; i++) {
            result[i] = alpha * values[i] + (1 - alpha) * result[i - 1];
        }

        return result;
    }

    function registerSelectionCallback(dotNetRef) {
        selectionCallback = dotNetRef;
    }

    function clearSelectionCallback() {
        selectionCallback = null;
        lastSelectionKey = null;
    }

    function ensureChart(elementId) {
        const el = document.getElementById(elementId);
        if (!el) return null;

        let chart = instances[elementId];
        if (chart) return chart;

        chart = echarts.init(el, null, { renderer: "canvas" });

        chart.group = chartGroup;
        echarts.connect(chartGroup);

        chart.on("brushEnd", function (params) {
            if (isSyncingBrush)
                return;

            if (!selectionCallback)
                return;

            const areas = params?.areas;

            if (!areas || areas.length === 0) {
                lastSelectionKey = null;

                isSyncingBrush = true;

                try {
                    Object.values(instances).forEach(otherChart => {
                        if (!otherChart) return;

                        otherChart.dispatchAction({
                            type: "brush",
                            areas: []
                        });
                    });
                } finally {
                    setTimeout(() => {
                        isSyncingBrush = false;
                    }, 0);
                }

                selectionCallback.invokeMethodAsync("OnChartSelectionCleared");
                return;
            }

            const range = areas[0]?.coordRange;
            if (!range || range.length < 2)
                return;

            const seriesData = chart.__seriesData;
            if (!seriesData || seriesData.length === 0)
                return;

            const x1 = Math.min(range[0], range[1]);
            const x2 = Math.max(range[0], range[1]);

            const startIndex = findFirstIndexAtOrAfterX(seriesData, x1);
            const endIndex = findLastIndexAtOrBeforeX(seriesData, x2);

            if (startIndex < 0 || endIndex < 0 || endIndex < startIndex)
                return;

            const key = `${startIndex}:${endIndex}`;
            if (key === lastSelectionKey)
                return;

            lastSelectionKey = key;

            isSyncingBrush = true;

            try {
                Object.values(instances).forEach(otherChart => {
                    if (!otherChart) return;

                    otherChart.dispatchAction({
                        type: "brush",
                        areas: [
                            {
                                brushType: "lineX",
                                coordRange: [x1, x2],
                                xAxisIndex: 0
                            }
                        ]
                    });
                });
            } finally {
                setTimeout(() => {
                    isSyncingBrush = false;
                }, 0);
            }

            selectionCallback.invokeMethodAsync(
                "OnChartSelection",
                startIndex,
                endIndex
            );
        });

        chart.getZr().on("globalout", function () {
            clearCursor();
        });

        chart.getZr().on("dblclick", function () {
            chart.dispatchAction({
                type: "brush",
                areas: []
            });

            if (selectionCallback) {
                selectionCallback.invokeMethodAsync("OnChartSelectionCleared");
            }
        });

        instances[elementId] = chart;
        return chart;
    }

    function dispose(elementId) {
        const chart = instances[elementId];
        if (!chart) return;

        chart.dispose();
        delete instances[elementId];
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

    function findFirstIndexAtOrAfterX(seriesData, targetX) {
        let left = 0;
        let right = seriesData.length - 1;
        let answer = -1;

        while (left <= right) {
            const mid = (left + right) >> 1;
            const x = seriesData[mid][0];

            if (x >= targetX) {
                answer = seriesData[mid][2];
                right = mid - 1;
            } else {
                left = mid + 1;
            }
        }

        if (answer >= 0) return answer;
        return seriesData[seriesData.length - 1][2];
    }

    function findLastIndexAtOrBeforeX(seriesData, targetX) {
        let left = 0;
        let right = seriesData.length - 1;
        let answer = -1;

        while (left <= right) {
            const mid = (left + right) >> 1;
            const x = seriesData[mid][0];

            if (x <= targetX) {
                answer = seriesData[mid][2];
                left = mid + 1;
            } else {
                right = mid - 1;
            }
        }

        if (answer >= 0) return answer;
        return seriesData[0][2];
    }

    function registerMapCursor(trackLatE7, trackLonE7, marker) {
        mapTrackLatE7 = trackLatE7;
        mapTrackLonE7 = trackLonE7;
        mapCursorMarker = marker;

        lastTrackIndex = -1;

        if (mapCursorMarker) {
            mapCursorMarker.setStyle({
                opacity: 0,
                fillOpacity: 0
            });
        }
    }

    function moveMapCursorToTrackIndex(trackIndex) {
        if (!mapCursorMarker || !mapTrackLatE7 || !mapTrackLonE7) return;
        if (trackIndex == null || trackIndex < 0) return;

        if (trackIndex >= mapTrackLatE7.length || trackIndex >= mapTrackLonE7.length)
            return;

        const lat = mapTrackLatE7[trackIndex] / 1e7;
        const lon = mapTrackLonE7[trackIndex] / 1e7;

        mapCursorMarker.setStyle({
            opacity: 1,
            fillOpacity: 1
        });

        mapCursorMarker.setLatLng([lat, lon]);
    }

    function showCursorAtTrackIndex(trackIndex) {
        if (trackIndex == null || trackIndex < 0) return;

        suppressMapToChart = true;

        try {
            Object.values(instances).forEach(chart => {
                chart.dispatchAction({
                    type: "showTip",
                    seriesIndex: 0,
                    dataIndex: trackIndex
                });
            });

            lastTrackIndex = trackIndex;
        } finally {
            suppressMapToChart = false;
        }
    }

    function hideCursor() {
        clearCursor();
    }

    function clearCursor() {
        suppressMapToChart = true;

        try {
            Object.values(instances).forEach(chart => {
                chart.dispatchAction({
                    type: "hideTip"
                });
            });

            lastTrackIndex = -1;

            if (mapCursorMarker) {
                mapCursorMarker.setStyle({
                    opacity: 0,
                    fillOpacity: 0
                });
            }
        } finally {
            suppressMapToChart = false;
        }
    }

    function buildSelectedClimbMarkLine(seriesData, payload) {
        const beginIndex = payload?.selectedClimbBeginIndex ?? payload?.SelectedClimbBeginIndex;
        const endIndex = payload?.selectedClimbEndIndex ?? payload?.SelectedClimbEndIndex;

        if (beginIndex == null || endIndex == null)
            return [];

        if (!seriesData || seriesData.length === 0)
            return [];

        const beginPoint = seriesData.find(p => p[2] === beginIndex);
        const endPoint = seriesData.find(p => p[2] === endIndex);

        if (!beginPoint || !endPoint)
            return [];

        return [
            {
                xAxis: beginPoint[0],
                lineStyle: {
                    type: "dashed",
                    width: 1,
                    color: "#ef4444"
                }
            },
            {
                xAxis: endPoint[0],
                lineStyle: {
                    type: "dashed",
                    width: 1,
                    color: "#ef4444"
                }
            }
        ];
    }

    function applyCombinedMarkLine(chart) {
        if (!chart) return;

        const baseData = chart.__baseMarkLineData ?? [];
        const selectedData = chart.__selectedMarkLineData ?? [];
        const allClimbsData = chart.__allClimbsMarkLineData ?? [];

        const combinedData = [
            ...baseData,
            ...selectedData,
            ...allClimbsData
        ];

        chart.setOption({
            series: [{
                markLine: {
                    silent: true,
                    symbol: ["none", "none"],
                    animation: false,
                    label: {
                        show: true
                    },
                    lineStyle: {
                        width: 1
                    },
                    data: combinedData
                }
            }]
        }, false);
    }

    function baseOption(title, unit, series, extra) {
        const maxX = series && series.length > 0
            ? series[series.length - 1][0]
            : 0;

        const timeInterval = getTimeAxisInterval(maxX);

        return {
            toolbox: {
                show: false
            },
            animation: false,

            brush: {
                xAxisIndex: "all",
                brushMode: "single",
                transformable: false,
                brushStyle: {
                    borderWidth: 1,
                    color: "rgba(245, 158, 11, 0.16)",
                    borderColor: "#f59e0b"
                }
            },

            axisPointer: {
                link: [{ xAxisIndex: "all" }],
                triggerTooltip: false
            },

            grid: {
                left: 64,
                right: 14,
                top: 28,
                bottom: 40,
                containLabel: false
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
                        !suppressMapToChart &&
                        trackIndex != null &&
                        !Number.isNaN(trackIndex) &&
                        trackIndex !== lastTrackIndex &&
                        now - lastHoverUpdateMs >= hoverThrottleMs
                    ) {
                        lastHoverUpdateMs = now;
                        lastTrackIndex = trackIndex;

                        suppressChartToMap = true;
                        try {
                            moveMapCursorToTrackIndex(trackIndex);
                        } finally {
                            suppressChartToMap = false;
                        }
                    }

                    const y = Array.isArray(p.value) ? p.value[1] : null;
                    return formatValue(y, unit);
                }
            },

            xAxis: {
                type: "value",
                min: 0,
                max: function (value) {
                    return value.max + Math.min(120, Math.max(30, value.max * 0.01));
                },
                interval: timeInterval,
                axisLine: {
                    show: true
                },
                axisTick: {
                    show: true
                },
                axisLabel: {
                    show: true,
                    color: "#64748b",
                    hideOverlap: false,
                    formatter: function (value) {
                        const total = Math.max(0, Math.floor(value));
                        const h = Math.floor(total / 3600);
                        const m = Math.floor((total % 3600) / 60);

                        return `${h}:${String(m).padStart(2, "0")}`;
                    }
                },
                axisPointer: {
                    show: true,
                    label: {
                        show: true,
                        backgroundColor: "#1e293b",
                        color: "#fff",
                        borderRadius: 4,
                        padding: [3, 6],
                        formatter: function (params) {
                            const seconds = Number(params.value || 0);
                            return formatTime(seconds);
                        }
                    }
                },
                splitLine: {
                    show: false
                }
            },

            yAxis: {
                type: "value",
                interval: extra.yInterval,
                axisLabel: {
                    color: "#64748b"
                }
            },

            dataZoom: [
                {
                    type: "inside",
                    xAxisIndex: 0,
                    filterMode: "none",
                    zoomOnMouseWheel: true
                }
            ],

            series: [
                {
                    type: "line",
                    showSymbol: false,
                    data: series,
                    lineStyle: extra.lineStyle,
                    areaStyle: extra.areaStyle ?? undefined,
                    markLine: extra.markLine ?? undefined
                }
            ]
        };
    }

    function getTimeAxisInterval(maxSeconds) {
        if (maxSeconds <= 60 * 60) return 600;
        if (maxSeconds <= 3 * 60 * 60) return 1800;
        return 3600;
    }

    function formatValue(value, unit) {
        if (value == null || Number.isNaN(value)) return `— ${unit}`;

        const decimals =
            unit === "m" ? 0 :
                unit === "km/h" ? 0 :
                    1;

        return `${Number(value).toFixed(decimals).replace(".", ",")} ${unit}`;
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

    function renderOne(elementId, title, unit, xValues, yValues, extra) {
        const chart = ensureChart(elementId);
        if (!chart || !xValues || !yValues) return;

        const series = buildSeriesData(xValues, yValues);
        chart.__seriesData = series;

        chart.__baseMarkLineData = extra.markLine?.data ?? [];
        chart.__selectedMarkLineData = [];
        chart.__allClimbsMarkLineData = [];

        chart.setOption(baseOption(title, unit, series, extra), true);
        applyCombinedMarkLine(chart);

        requestAnimationFrame(() => chart.resize());
    }

    function updateSelectedClimbOne(elementId, payload) {
        const chart = instances[elementId];
        if (!chart)
            return;

        const seriesData = chart.__seriesData;
        if (!seriesData || seriesData.length === 0)
            return;

        chart.__selectedMarkLineData = buildSelectedClimbMarkLine(seriesData, payload);
        applyCombinedMarkLine(chart);
    }

    function updateAllClimbsOne(elementId, payload) {
        const chart = instances[elementId];
        if (!chart) return;

        const seriesData = chart.__seriesData;
        if (!seriesData || seriesData.length === 0)
            return;

        if (!payload.showAllClimbs) {
            chart.__allClimbsMarkLineData = [];
            applyCombinedMarkLine(chart);
            return;
        }

        const lines = [];

        for (let i = 0; i < payload.begin.length; i++) {
            const begin = payload.begin[i];
            const end = payload.end[i];

            const p1 = seriesData.find(p => p[2] === begin);
            const p2 = seriesData.find(p => p[2] === end);

            if (!p1 || !p2) continue;

            const color = colors[i % colors.length];

            lines.push(
                {
                    xAxis: p1[0],
                    lineStyle: { color, type: "dashed", width: 1 }
                },
                {
                    xAxis: p2[0],
                    lineStyle: { color, type: "dashed", width: 1 }
                }
            );
        }

        chart.__allClimbsMarkLineData = lines;
        applyCombinedMarkLine(chart);
    }

    function updateAllClimbs(altId, varioId, speedId, payload) {
        updateAllClimbsOne(altId, payload);
        updateAllClimbsOne(varioId, payload);
        updateAllClimbsOne(speedId, payload);
    }

    function updateHoveredClimb(altChartId, varioChartId, speedChartId, payload) {
        updateHoveredClimbForChart(altChartId, payload);
        updateHoveredClimbForChart(varioChartId, payload);
        updateHoveredClimbForChart(speedChartId, payload);
    }

    function updateHoveredClimbForChart(chartId, payload) {
        const chart = instances[chartId];
        if (!chart) return;

        const beginSec = payload?.hoveredClimbBeginSec;
        const endSec = payload?.hoveredClimbEndSec;
        const index = payload?.hoveredClimbIndex;

        const series = chart.getOption()?.series ?? [];
        if (!series.length) return;

        let fillColor = "rgba(37, 99, 235, 0.12)";

        if (index != null) {
            const baseColor = colors[index % colors.length];
            fillColor = hexToRgba(baseColor, 0.3);
        }

        const markArea = (beginSec != null && endSec != null)
            ? {
                silent: true,
                itemStyle: {
                    color: fillColor
                },
                data: [[
                    { xAxis: beginSec },
                    { xAxis: endSec }
                ]]
            }
            : { data: [] };

        chart.setOption({
            series: series.map((s, i) =>
                i === 0
                    ? { ...s, markArea }
                    : s)
        }, false);
    }

    function updateSelectedClimb(altId, varioId, speedId, payload) {
        updateSelectedClimbOne(altId, payload);
        updateSelectedClimbOne(varioId, payload);
        updateSelectedClimbOne(speedId, payload);
    }

    function renderAll(altId, varioId, speedId, payload) {
        renderOne(
            altId,
            payload.altitudeTitle,
            payload.altitudeUnit,
            payload.timeSec,
            payload.altitudeValues,
            {
                lineStyle: { width: 1, color: "#2563eb" },
                areaStyle: { opacity: 0.08 },
                yMin: 0,
                yMax: 2500,
                yInterval: 500
            }
        );

        const varioChart = ensureChart(varioId);
        if (varioChart && payload.timeSec && payload.varioValues) {
            const rawSeries = buildSeriesData(payload.timeSec, payload.varioValues);
            const smoothValues = computeEma(payload.varioValues, alpha);
            const smoothSeries = buildSeriesData(payload.timeSec, smoothValues);

            varioChart.__seriesData = rawSeries;
            varioChart.__baseMarkLineData = [];
            varioChart.__selectedMarkLineData = [];
            varioChart.__allClimbsMarkLineData = [];

            const option = baseOption(
                payload.varioTitle,
                payload.varioUnit,
                rawSeries,
                {
                    yMin: -3,
                    yMax: 3,
                    yInterval: 1
                }
            );

            option.series = [
                {
                    name: "raw",
                    type: "line",
                    showSymbol: false,
                    data: rawSeries,
                    lineStyle: {
                        width: 1,
                        color: "#2563eb"
                    },
                    z: 1
                },
                {
                    name: "smooth",
                    type: "line",
                    showSymbol: false,
                    data: smoothSeries,
                    lineStyle: {
                        width: 1,
                        color: "#ef4444"
                    },
                    z: 3
                }
            ];

            varioChart.setOption(option, true);
            applyCombinedMarkLine(varioChart);

            requestAnimationFrame(() => varioChart.resize());
        }

        renderOne(
            speedId,
            payload.speedTitle,
            payload.speedUnit,
            payload.timeSec,
            payload.speedValues,
            {
                lineStyle: { width: 1, color: "#2563eb" },
                yMin: 0,
                yMax: 60,
                yInterval: 10,
                markLine: {
                    data: [
                        {
                            yAxis: 38,
                            label: {
                                show: true,
                                formatter: "Trim 38 km/h"
                            },
                            lineStyle: {
                                type: "dashed",
                                width: 2,
                                color: "#ef4444"
                            }
                        }
                    ]
                }
            }
        );

        echarts.connect(chartGroup);
    }

    window.addEventListener("resize", () => {
        Object.values(instances).forEach(chart => chart.resize());
    });

    return {
        renderAll,
        updateSelectedClimb,
        dispose,
        registerMapCursor,
        showCursorAtTrackIndex,
        hideCursor,
        clearCursor,
        registerSelectionCallback,
        clearSelectionCallback,
        updateAllClimbs,
        updateHoveredClimb
    };

})();