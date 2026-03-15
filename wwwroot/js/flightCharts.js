window.flightCharts = (function () {

    const instances = {};
    const chartGroup = "flight-charts-sync-group";

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
            return null;

        if (!seriesData || seriesData.length === 0)
            return null;

        const beginPoint = seriesData.find(p => p[2] === beginIndex);
        const endPoint = seriesData.find(p => p[2] === endIndex);

        if (!beginPoint || !endPoint)
            return null;

        return {
            silent: true,
            symbol: ["none", "none"],
            animation: false,
            label: { show: false },
            lineStyle: {
                type: "dashed",
                width: 1,
                color: "#ef4444"
            },
            data: [
                { xAxis: beginPoint[0] },
                { xAxis: endPoint[0] }
            ]
        };
    }

    function baseOption(title, unit, series, extra) {
        return {
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
                confine: true
            },

            xAxis: {
                type: "value"
            },

            yAxis: {
                type: "value"
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
                    areaStyle: extra.areaStyle ?? undefined
                }
            ]
        };
    }

    function renderOne(elementId, title, unit, xValues, yValues, extra) {
        const chart = ensureChart(elementId);
        if (!chart || !xValues || !yValues) return;

        const series = buildSeriesData(xValues, yValues);
        chart.__seriesData = series;

        chart.setOption(baseOption(title, unit, series, extra), true);

        chart.dispatchAction({
            type: "takeGlobalCursor",
            key: "brush",
            brushOption: {
                brushType: "lineX",
                brushMode: "single"
            }
        });

        requestAnimationFrame(() => chart.resize());
    }

    function updateSelectedClimbOne(elementId, payload) {
        const chart = instances[elementId];
        if (!chart)
            return;

        const seriesData = chart.__seriesData;
        if (!seriesData || seriesData.length === 0)
            return;

        const markLine = buildSelectedClimbMarkLine(seriesData, payload);

        chart.setOption({
            series: [
                {
                    markLine: markLine ?? { data: [] }
                }
            ]
        }, false);
    }

    function updateAllClimbsOne(elementId, payload) {

        const chart = instances[elementId];
        if (!chart) return;

        const seriesData = chart.__seriesData;
        if (!seriesData || seriesData.length === 0)
            return;

        if (!payload.showAllClimbs) {
            chart.setOption({
                series: [{
                    markLine: { data: [] }
                }]
            }, false);
            return;
        }

        const colors = [
            "#2563eb",
            "#16a34a",
            "#ea580c",
            "#9333ea",
            "#0891b2",
            "#dc2626"
        ];

        const lines = [];

        for (let i = 0; i < payload.begin.length; i++) {

            const begin = payload.begin[i];
            const end = payload.end[i];

            const p1 = seriesData.find(p => p[2] === begin);
            const p2 = seriesData.find(p => p[2] === end);

            if (!p1 || !p2) continue;

            const color = colors[i % colors.length];

            lines.push(
                { xAxis: p1[0], lineStyle: { color, type: "dashed", width: 1 } },
                { xAxis: p2[0], lineStyle: { color, type: "dashed", width: 1 } }
            );
        }

        chart.setOption({
            series: [{
                markLine: {
                    silent: true,
                    symbol: ["none", "none"],
                    data: lines
                }
            }]
        }, false);
    }

    function updateAllClimbs(altId, varioId, speedId, payload) {

        updateAllClimbsOne(altId, payload);
        updateAllClimbsOne(varioId, payload);
        updateAllClimbsOne(speedId, payload);

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
                lineStyle: { width: 1, color: "#2563eb" }
            }
        );

        renderOne(
            speedId,
            payload.speedTitle,
            payload.speedUnit,
            payload.timeSec,
            payload.speedValues,
            {
                lineStyle: { width: 1, color: "#2563eb" }
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
        updateAllClimbs
    };

})();