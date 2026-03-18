window.flightMapComponent = window.flightMapComponent || (function () {
    const instances = {};

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
            latE7: null,
            lonE7: null,
            lastHoverTrackIndex: -1,
            legend: null
        };

        instance.legend = createVarioLegend();
        instance.legend.addTo(map);

        instances[elementId] = instance;
        return instance;
    }

    function createVarioLegend() {
        const legend = L.control({ position: "bottomright" });

        legend.onAdd = function () {
            const div = L.DomUtil.create("div", "flight-vario-legend");

            div.innerHTML = `
        <div class="legend-title">Vario</div>

        <div><span style="background:#b30000"></span> &gt; 1.5 m/s</div>
        <div><span style="background:#ff3030"></span> 0.3 – 1.5</div>
        <div><span style="background:#9ca3af"></span> -0.3 – 0.3</div>
        <div><span style="background:#2a62ff"></span> -1.5 – -0.3</div>
        <div><span style="background:#0038d9"></span> &lt; -1.5</div>
        `;

            return div;
        };

        return legend;
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

    function getTrackColorByVario(varioCms) {
        const v = varioCms * 0.01;

        if (v > 1.5)
            return "#b30000";

        if (v > 0.3)
            return "#ff3030";

        if (v >= -0.3)
            return "#9ca3af";

        if (v >= -1.5)
            return "#2a62ff";

        return "#0038d9";
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
                interactive: false
            }).addTo(instance.trackLayer);
        }

        instance.startMarker = L.circleMarker(latLngs[0], {
            radius: 6,
            color: "#16a34a",
            fillColor: "#16a34a",
            fillOpacity: 1,
            weight: 2
        })
            .addTo(instance.map)
            .bindTooltip("Takeoff");

        instance.endMarker = L.circleMarker(latLngs[latLngs.length - 1], {
            radius: 6,
            color: "#dc2626",
            fillColor: "#dc2626",
            fillOpacity: 1,
            weight: 2
        })
            .addTo(instance.map)
            .bindTooltip("Landing");

        instance.cursorMarker = L.circleMarker(latLngs[0], {
            radius: 5,
            color: "#0f172a",
            fillColor: "#f59e0b",
            fillOpacity: 0,
            opacity: 0,
            weight: 2
        }).addTo(instance.map);

        instance.map.off("mousemove");
        instance.map.off("mouseout");

        instance.map.on("mousemove", function (e) {
            if (!instance.latE7 || !instance.lonE7) return;

            const trackIndex = findNearestTrackIndex(
                instance.latE7,
                instance.lonE7,
                e.latlng.lat,
                e.latlng.lng
            );

            if (trackIndex < 0) return;
            if (trackIndex === instance.lastHoverTrackIndex) return;

            instance.lastHoverTrackIndex = trackIndex;

            const lat = instance.latE7[trackIndex] / 1e7;
            const lon = instance.lonE7[trackIndex] / 1e7;

            if (instance.cursorMarker) {
                instance.cursorMarker.setStyle({
                    opacity: 1,
                    fillOpacity: 1
                });

                instance.cursorMarker.setLatLng([lat, lon]);
            }

            if (!window.flightCharts?.isSuppressChartToMap?.()) {
                window.flightCharts.showCursorAtTrackIndex(trackIndex);
            }
        });

        instance.map.on("mouseout", function () {
            instance.lastHoverTrackIndex = -1;

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

        instance.map.remove();
        delete instances[elementId];
    }

    return {
        renderTrackArrays,
        clear,
        dispose
    };
})();