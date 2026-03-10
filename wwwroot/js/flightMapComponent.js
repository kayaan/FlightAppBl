window.flightMapComponent = window.flightMapComponent || (function () {
    const instances = {};

    function ensureMap(elementId) {
        const el = document.getElementById(elementId);
        if (!el) return null;

        let instance = instances[elementId];
        if (instance) return instance;

        const map = L.map(el, {
            zoomControl: true,
            preferCanvas: true
        });

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap"
        }).addTo(map);

        instance = {
            map,
            trackLayer: null,
            startMarker: null,
            endMarker: null,
            cursorMarker: null,
            latE7: null,
            lonE7: null,
            lastHoverTrackIndex: -1
        };

        instances[elementId] = instance;
        return instance;
    }

    function clear(elementId) {
        const instance = instances[elementId];
        if (!instance) return;

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

    function renderTrackArrays(elementId, latE7, lonE7) {
        const instance = ensureMap(elementId);
        if (!instance) return;

        clear(elementId);

        instance.latE7 = latE7;
        instance.lonE7 = lonE7;
        instance.lastHoverTrackIndex = -1;

        const latLngs = buildLatLngs(latE7, lonE7);
        if (latLngs.length === 0) return;

        instance.trackLayer = L.polyline(latLngs, {
            weight: 3,
            color: "#2563eb"
        }).addTo(instance.map);

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
            fillOpacity: 1,
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
                instance.cursorMarker.setLatLng([lat, lon]);
            }

            if (!window.flightCharts?.isSuppressChartToMap?.()) {
                window.flightCharts.showCursorAtTrackIndex(trackIndex);
            }
        });

        instance.map.on("mouseout", function () {
            instance.lastHoverTrackIndex = -1;

            if (window.flightCharts?.hideCursor) {
                window.flightCharts.hideCursor();
            }
        });

        if (window.flightCharts?.registerMapCursor) {
            window.flightCharts.registerMapCursor(
                latE7,
                lonE7,
                instance.cursorMarker
            );
        }

        instance.map.fitBounds(instance.trackLayer.getBounds(), {
            padding: [20, 20]
        });

        setTimeout(() => {
            instance.map.invalidateSize();
        }, 0);
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