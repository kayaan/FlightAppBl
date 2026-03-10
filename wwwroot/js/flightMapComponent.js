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
            cursorMarker: null
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

    function renderTrackArrays(elementId, timeSec, latE7, lonE7) {
        const instance = ensureMap(elementId);
        if (!instance) return;

        clear(elementId);

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

        if (window.flightCharts?.registerMapCursor) {
            window.flightCharts.registerMapCursor(
                timeSec,
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