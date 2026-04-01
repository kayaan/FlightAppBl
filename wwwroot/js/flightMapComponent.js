window.flightMapComponent = window.flightMapComponent || (function () {
    const instances = {};

    const hoverTrackThresholdPx = 20;

    const climbColors = [
        "#2563eb",
        "#16a34a",
        "#ea580c",
        "#9333ea",
        "#0891b2",
        "#dc2626"
    ];

    const hoverConfig = {
        haloWeight: 10,
        lineWeight: 5,
        opacity: 0.9
    };

    const selectedConfig = {
        haloWeight: 12,
        lineWeight: 6,
        opacity: 1.0
    };

    function createRoadLayer() {
        return L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap"
        });
    }

    function createTopoLayer() {
        return L.tileLayer("https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png", {
            maxZoom: 17,
            attribution: "&copy; OpenTopoMap, OpenStreetMap contributors"
        });
    }

    function setBaseLayer(instance, mapStyle) {
        const nextBaseLayer = mapStyle === "topo" ? "topo" : "road";

        if (instance.activeBaseLayer === nextBaseLayer) {
            return;
        }

        if (instance.activeBaseLayer === "road" && instance.roadLayer) {
            instance.map.removeLayer(instance.roadLayer);
        }

        if (instance.activeBaseLayer === "topo" && instance.topoLayer) {
            instance.map.removeLayer(instance.topoLayer);
        }

        if (nextBaseLayer === "road") {
            instance.roadLayer.addTo(instance.map);
        } else {
            instance.topoLayer.addTo(instance.map);
        }

        instance.activeBaseLayer = nextBaseLayer;
    }

    function ensureMap(elementId) {
        const el = document.getElementById(elementId);
        if (!el) return null;

        let instance = instances[elementId];
        if (instance) return instance;

        const map = L.map(el, {
            zoomControl: true,
            preferCanvas: true
        });

        if (!map.getPane("trackPane")) {
            map.createPane("trackPane");
        }

        if (!map.getPane("climbPane")) {
            map.createPane("climbPane");
        }

        if (!map.getPane("cursorPane")) {
            map.createPane("cursorPane");
        }

        map.getPane("trackPane").style.zIndex = 400;
        map.getPane("climbPane").style.zIndex = 450;
        map.getPane("cursorPane").style.zIndex = 500;

        const roadLayer = createRoadLayer();
        const topoLayer = createTopoLayer();

        topoLayer.addTo(map);

        instance = {
            map,
            roadLayer,
            topoLayer,
            activeBaseLayer: "topo",
            trackLayer: null,
            startMarker: null,
            endMarker: null,
            cursorMarker: null,
            cursorHalo: null,
            hoveredClimbLayer: null,
            hoveredClimbHalo: null,
            hoveredClimbLine: null,
            selectedClimbLayer: null,
            selectedClimbHalo: null,
            selectedClimbLine: null,
            latE7: null,
            lonE7: null,
            lastHoverTrackIndex: -1,
        };



        instances[elementId] = instance;
        return instance;
    }

    function renderClimbHighlight(instance, config) {
        if (!instance) return;

        const {
            beginIndex,
            endIndex,
            color,
            haloWeight,
            lineWeight,
            opacity,
            layerKey
        } = config;

        instance[layerKey + "Halo"] = removeLayer(instance.map, instance[layerKey + "Halo"]);
        instance[layerKey + "Line"] = removeLayer(instance.map, instance[layerKey + "Line"]);

        if (beginIndex == null || endIndex == null)
            return;

        const halo = buildSegmentPolyline(
            instance,
            beginIndex,
            endIndex,
            "#000000",
            haloWeight,
            0.5
        );

        const line = buildSegmentPolyline(
            instance,
            beginIndex,
            endIndex,
            color,
            lineWeight,
            opacity
        );

        if (halo) {
            halo.addTo(instance.map);
            halo.bringToFront();
            instance[layerKey + "Halo"] = halo;
        }

        if (line) {
            line.addTo(instance.map);
            line.bringToFront();
            instance[layerKey + "Line"] = line;
        }
    }

    function updateSelectedClimb(mapId, payload) {

        const instance = instances[mapId];
        if (!instance) {
            return;
        }

        if (!instance) return;

        renderClimbHighlight(instance, {
            beginIndex: payload?.beginIndex,
            endIndex: payload?.endIndex,
            color: "#ff3b30",
            haloWeight: 7,
            lineWeight: 3,
            opacity: 1.0,
            layerKey: "selectedClimb"
        });

        if (instance.selectedClimbHalo) instance.selectedClimbHalo.bringToFront();
        if (instance.selectedClimbLine) instance.selectedClimbLine.bringToFront();

        const cursorIndex = payload?.cursorIndex;

        if (cursorIndex == null || cursorIndex < 0) {
            if (instance.cursorMarker) {
                instance.cursorMarker.setStyle({
                    opacity: 0,
                    fillOpacity: 0
                });
            }
            return;
        }

        moveCursorMarkerToIndex(instance, cursorIndex);
    }

    function moveCursorMarkerToIndex(instance, trackIndex) {
        if (!instance || !instance.cursorMarker || !instance.latE7 || !instance.lonE7)
            return;

        if (trackIndex == null || trackIndex < 0)
            return;

        if (trackIndex >= instance.latE7.length || trackIndex >= instance.lonE7.length)
            return;

        const lat = instance.latE7[trackIndex] / 1e7;
        const lon = instance.lonE7[trackIndex] / 1e7;

        instance.cursorMarker.setStyle({
            opacity: 1,
            fillOpacity: 1
        });

        instance.cursorMarker.setLatLng([lat, lon]);

        if (instance.cursorHalo) {
            instance.cursorHalo.setStyle({
                opacity: 0.5,
                fillOpacity: 0.2
            });

            instance.cursorHalo.setLatLng([lat, lon]);
        }
    }


    function updateHoveredClimb(mapId, payload) {
        const instance = instances[mapId];
        if (!instance) return;

        const beginIndex = payload?.beginIndex;
        const endIndex = payload?.endIndex;
        const climbIndex = payload?.climbIndex;
        const isSameAsSelected = payload?.isSameAsSelected === true;

        if (isSameAsSelected || beginIndex == null || endIndex == null) {
            instance.hoveredClimbHalo = removeLayer(instance.map, instance.hoveredClimbHalo);
            instance.hoveredClimbLine = removeLayer(instance.map, instance.hoveredClimbLine);

            if (instance.selectedClimbHalo) instance.selectedClimbHalo.bringToFront();
            if (instance.selectedClimbLine) instance.selectedClimbLine.bringToFront();
            return;
        }

        const color = climbIndex != null
            ? climbColors[climbIndex % climbColors.length]
            : "#2563eb";

        renderClimbHighlight(instance, {
            beginIndex,
            endIndex,
            color,
            haloWeight: 9,
            lineWeight: 3,
            opacity: 0.95,
            layerKey: "hoveredClimb"
        });

        // Selected bleibt dominant
        if (instance.selectedClimbHalo) instance.selectedClimbHalo.bringToFront();
        if (instance.selectedClimbLine) instance.selectedClimbLine.bringToFront();
    }

    function buildSegmentPolyline(instance, beginIndex, endIndex, color, weight, opacity) {
        if (!instance || !instance.latE7 || !instance.lonE7)
            return null;

        if (beginIndex == null || endIndex == null)
            return null;

        if (beginIndex < 0 || endIndex < beginIndex)
            return null;

        const maxIndex = Math.min(instance.latE7.length, instance.lonE7.length) - 1;
        if (maxIndex < 0)
            return null;

        const from = Math.max(0, Math.min(beginIndex, maxIndex));
        const to = Math.max(0, Math.min(endIndex, maxIndex));

        if (to < from)
            return null;

        const latlngs = [];

        for (let i = from; i <= to; i++) {
            latlngs.push([
                instance.latE7[i] / 1e7,
                instance.lonE7[i] / 1e7
            ]);
        }

        if (latlngs.length < 2)
            return null;

        return L.polyline(latlngs, {
            color,
            weight,
            opacity,
            interactive: false,
            pane: "climbPane"
        });
    }

    function removeLayer(map, layer) {
        if (!map || !layer) return null;

        if (map.hasLayer(layer)) {
            map.removeLayer(layer);
        }

        return null;
    }

    function clear(elementId) {
        const instance = instances[elementId];
        if (!instance) return;

        instance.map.off("mousemove");
        instance.map.off("mouseout");

        if (instance.trackLayer) {
            instance.map.removeLayer(instance.trackLayer);
            instance.trackLayer = null;
        }

        if (instance.startMarker) {
            instance.map.removeLayer(instance.startMarker);
            instance.startMarker = null;
        }

        if (instance.endMarker) {
            instance.map.removeLayer(instance.endMarker);
            instance.endMarker = null;
        }

        if (instance.cursorMarker) {
            instance.map.removeLayer(instance.cursorMarker);
            instance.cursorMarker = null;
        }

        if (instance.cursorHalo) {
            instance.map.removeLayer(instance.cursorHalo);
            instance.cursorHalo = null;
        }

        instance.hoveredClimbHalo = removeLayer(instance.map, instance.hoveredClimbHalo);
        instance.hoveredClimbLine = removeLayer(instance.map, instance.hoveredClimbLine);
        instance.selectedClimbHalo = removeLayer(instance.map, instance.selectedClimbHalo);
        instance.selectedClimbLine = removeLayer(instance.map, instance.selectedClimbLine);

        instance.latE7 = null;
        instance.lonE7 = null;
        instance.lastHoverTrackIndex = -1;
    }

    function buildLatLngs(latE7, lonE7) {
        if (!latE7 || !lonE7) return [];

        const count = Math.min(latE7.length, lonE7.length);
        const latLngs = [];

        for (let i = 0; i < count; i++) {
            const lat = latE7[i] / 1e7;
            const lng = lonE7[i] / 1e7;

            if (lat < -90 || lat > 90 || lng < -180 || lng > 180) {
                continue;
            }

            latLngs.push([lat, lng]);
        }

        return latLngs;
    }

    function clamp(value, min, max) {
        return Math.max(min, Math.min(max, value));
    }

    function lerp(a, b, t) {
        return a + (b - a) * t;
    }

    function rgbToHex(r, g, b) {
        const toHex = (x) => Math.round(x).toString(16).padStart(2, "0");
        return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
    }

    function interpolateColor(colorA, colorB, t) {
        return rgbToHex(
            lerp(colorA[0], colorB[0], t),
            lerp(colorA[1], colorB[1], t),
            lerp(colorA[2], colorB[2], t)
        );
    }

    function getTrackColorByVario(varioCms) {
        const v = varioCms * 0.01;

        const maxAbsVario = 3.5;

        const tRaw = clamp(Math.abs(v) / maxAbsVario, 0, 1);
        const t = Math.pow(tRaw, 0.35); // 🔥 sehr aggressiv

        const neutral = [180, 180, 180];
        const darkRed = [140, 0, 0];
        const darkBlue = [0, 30, 180];

        if (v > 0) {
            return interpolateColor(neutral, darkRed, t);
        }

        if (v < 0) {
            return interpolateColor(neutral, darkBlue, t);
        }

        return rgbToHex(neutral[0], neutral[1], neutral[2]);
    }
    function buildColoredTrackSegments(latE7, lonE7, varioCms) {
        if (!latE7 || !lonE7 || !varioCms) return [];

        const count = Math.min(latE7.length, lonE7.length, varioCms.length);
        if (count < 2) return [];

        const segments = [];

        for (let i = 1; i < count; i++) {
            const lat1 = latE7[i - 1] / 1e7;
            const lon1 = lonE7[i - 1] / 1e7;
            const lat2 = latE7[i] / 1e7;
            const lon2 = lonE7[i] / 1e7;

            const isValid =
                Number.isFinite(lat1) &&
                Number.isFinite(lon1) &&
                Number.isFinite(lat2) &&
                Number.isFinite(lon2) &&
                lat1 >= -90 && lat1 <= 90 &&
                lon1 >= -180 && lon1 <= 180 &&
                lat2 >= -90 && lat2 <= 90 &&
                lon2 >= -180 && lon2 <= 180;

            if (!isValid) {
                continue;
            }

            segments.push({
                latLngs: [
                    [lat1, lon1],
                    [lat2, lon2]
                ],
                color: getTrackColorByVario(varioCms[i]),
                trackIndex: i
            });
        }

        return segments;
    }

    function findNearestTrackIndex(latE7, lonE7, lat, lng) {
        if (!latE7 || !lonE7) return -1;

        const count = Math.min(latE7.length, lonE7.length);
        if (count === 0) return -1;

        const targetLatE7 = Math.round(lat * 1e7);
        const targetLngE7 = Math.round(lng * 1e7);

        let bestIndex = 0;
        let bestDist = Number.POSITIVE_INFINITY;

        for (let i = 0; i < count; i++) {
            const dLat = latE7[i] - targetLatE7;
            const dLng = lonE7[i] - targetLngE7;
            const dist = dLat * dLat + dLng * dLng;

            if (dist < bestDist) {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    function findNearestTrackPointInPixels(map, latE7, lonE7, mouseLatLng) {
        if (!map || !latE7 || !lonE7 || !mouseLatLng)
            return { trackIndex: -1, distancePx: Number.POSITIVE_INFINITY };

        const count = Math.min(latE7.length, lonE7.length);
        if (count === 0)
            return { trackIndex: -1, distancePx: Number.POSITIVE_INFINITY };

        const mousePoint = map.latLngToContainerPoint(mouseLatLng);

        let bestIndex = -1;
        let bestDistSq = Number.POSITIVE_INFINITY;

        for (let i = 0; i < count; i++) {
            const lat = latE7[i] / 1e7;
            const lon = lonE7[i] / 1e7;

            if (
                !Number.isFinite(lat) ||
                !Number.isFinite(lon) ||
                lat < -90 || lat > 90 ||
                lon < -180 || lon > 180
            ) {
                continue;
            }

            const point = map.latLngToContainerPoint([lat, lon]);

            const dx = point.x - mousePoint.x;
            const dy = point.y - mousePoint.y;
            const distSq = dx * dx + dy * dy;

            if (distSq < bestDistSq) {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }

        return {
            trackIndex: bestIndex,
            distancePx: bestIndex >= 0 ? Math.sqrt(bestDistSq) : Number.POSITIVE_INFINITY
        };
    }

    function renderTrackArrays(elementId, latE7, lonE7, varioCms, mapStyle) {
        const instance = ensureMap(elementId);
        if (!instance) return;

        setBaseLayer(instance, mapStyle);

        clear(elementId);

        instance.latE7 = latE7;
        instance.lonE7 = lonE7;
        instance.lastHoverTrackIndex = -1;

        const latLngs = buildLatLngs(latE7, lonE7);
        if (latLngs.length < 2) return;

        const segments = buildColoredTrackSegments(latE7, lonE7, varioCms);
        if (segments.length === 0) {
            console.warn("No colored track segments could be built.", {
                latCount: latE7?.length ?? 0,
                lonCount: lonE7?.length ?? 0,
                varioCount: varioCms?.length ?? 0
            });
            return;
        }

        const bounds = L.latLngBounds(latLngs);
        instance.map.fitBounds(bounds, {
            padding: [20, 20]
        });

        instance.map.invalidateSize();

        instance.trackLayer = L.featureGroup().addTo(instance.map);

        for (const segment of segments) {
            L.polyline(segment.latLngs, {
                weight: 4,
                color: segment.color,
                interactive: false,
                pane: "trackPane"
            }).addTo(instance.trackLayer);
        }

        instance.startMarker = L.circleMarker(latLngs[0], {
            radius: 6,
            color: "#16a34a",
            fillColor: "#16a34a",
            fillOpacity: 1,
            weight: 2,
            pane: "cursorPane"
        })
            .addTo(instance.map)
            .bindTooltip("Takeoff");

        instance.endMarker = L.circleMarker(latLngs[latLngs.length - 1], {
            radius: 6,
            color: "#dc2626",
            fillColor: "#dc2626",
            fillOpacity: 1,
            weight: 2,
            pane: "cursorPane"
        })
            .addTo(instance.map)
            .bindTooltip("Landing");

        instance.cursorMarker = L.circleMarker(latLngs[0], {
            radius: 7,
            color: "#000000",
            weight: 3,
            opacity: 0,
            fillColor: "#fff200",
            fillOpacity: 0,
            pane: "cursorPane"
        }).addTo(instance.map);

        instance.cursorHalo = L.circleMarker(latLngs[0], {
            radius: 14,
            color: "#fff200",
            weight: 0,
            fillColor: "#fff200",
            fillOpacity: 0,
            opacity: 0,
            interactive: false,
            pane: "cursorPane"
        }).addTo(instance.map);

        instance.map.off("mousemove");
        instance.map.off("mouseout");

        instance.map.on("mousemove", function (e) {
            if (!instance.latE7 || !instance.lonE7) return;

            const hit = findNearestTrackPointInPixels(
                instance.map,
                instance.latE7,
                instance.lonE7,
                e.latlng
            );

            if (hit.trackIndex < 0 || hit.distancePx > hoverTrackThresholdPx) {
                if (instance.lastHoverTrackIndex !== -1) {
                    instance.lastHoverTrackIndex = -1;

                    if (instance.cursorMarker) {
                        instance.cursorMarker.setStyle({
                            opacity: 0,
                            fillOpacity: 0
                        });
                    }

                    if (instance.cursorHalo) {
                        instance.cursorHalo.setStyle({
                            opacity: 0,
                            fillOpacity: 0
                        });
                    }

                    if (window.flightCharts?.clearCursor) {
                        window.flightCharts.clearCursor();
                    }
                }

                return;
            }

            const trackIndex = hit.trackIndex;

            if (trackIndex === instance.lastHoverTrackIndex) {
                return;
            }

            instance.lastHoverTrackIndex = trackIndex;

            const lat = instance.latE7[trackIndex] / 1e7;
            const lon = instance.lonE7[trackIndex] / 1e7;

            if (instance.cursorMarker) {
                instance.cursorMarker.setStyle({
                    opacity: 1,
                    fillOpacity: 1,
                    weight: 3
                });

                instance.cursorMarker.setLatLng([lat, lon]);
            }

            if (instance.cursorHalo) {
                instance.cursorHalo.setStyle({
                    opacity: 0.5,
                    fillOpacity: 0.2
                });

                instance.cursorHalo.setLatLng([lat, lon]);
            }

            if (!window.flightCharts?.isSuppressChartToMap?.()) {
                window.flightCharts.showCursorAtTrackIndex(trackIndex);
            }
        });

        instance.map.on("mouseout", function () {
            instance.lastHoverTrackIndex = -1;

            if (instance.cursorMarker) {
                instance.cursorMarker.setStyle({
                    opacity: 0,
                    fillOpacity: 0
                });
            }

            if (instance.cursorHalo) {
                instance.cursorHalo.setStyle({
                    opacity: 0,
                    fillOpacity: 0
                });
            }

            if (window.flightCharts?.clearCursor) {
                window.flightCharts.clearCursor();
            }
        });

        if (window.flightCharts?.registerMapCursor) {
            window.flightCharts.registerMapCursor(
                latE7,
                lonE7,
                instance.cursorMarker
            );
        }

        setTimeout(() => {
            instance.map.invalidateSize();
        }, 0);
    }

    function dispose(elementId) {
        const instance = instances[elementId];
        if (!instance) return;

        instance.hoveredClimbHalo = removeLayer(instance.map, instance.hoveredClimbHalo);
        instance.hoveredClimbLine = removeLayer(instance.map, instance.hoveredClimbLine);
        instance.selectedClimbHalo = removeLayer(instance.map, instance.selectedClimbHalo);
        instance.selectedClimbLine = removeLayer(instance.map, instance.selectedClimbLine);

        instance.map.remove();
        delete instances[elementId];
    }

    function registerInteraction(elementId, dotNetRef) {
        const instance = instances[elementId];
        if (!instance) return;

        instance.dotNetRef = dotNetRef;

        instance.map.on("mousemove", (e) => {
            const index = findNearestTrackIndex(instance, e.latlng);
            if (index != null) {
                dotNetRef.invokeMethodAsync("OnMapTrackHover", index);
            }
        });

        instance.map.on("mouseout", () => {
            dotNetRef.invokeMethodAsync("OnMapTrackLeave");
        });

        instance.map.on("click", () => {
            dotNetRef.invokeMethodAsync("OnMapTrackClick");
        });
    }

    function findNearestTrackIndex(instance, latlng) {
        const latE7 = instance.latE7;
        const lonE7 = instance.lonE7;

        if (!latE7 || !lonE7) return null;

        let bestIndex = null;
        let bestDist = Infinity;

        const lat = latlng.lat;
        const lon = latlng.lng;

        for (let i = 0; i < latE7.length; i += 5) { // step für Performance
            const dLat = lat - latE7[i] / 1e7;
            const dLon = lon - lonE7[i] / 1e7;
            const dist = dLat * dLat + dLon * dLon;

            if (dist < bestDist) {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    return {
        renderTrackArrays,
        clear,
        dispose,
        updateHoveredClimb,
        updateSelectedClimb,
        registerInteraction
    };
})();