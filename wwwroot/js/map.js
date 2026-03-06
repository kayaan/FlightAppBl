window.flightMap = {

    createMap: function (elementId, lat, lon) {

        const map = L.map(elementId).setView([lat, lon], 12);

        L.tileLayer(
            "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
            {
                attribution: "© OpenStreetMap contributors"
            }
        ).addTo(map);

        window._flightMapInstance = map;
    },

    drawTrack: function (points) {

        const map = window._flightMapInstance;

        const latlngs = points.map(p => [p.latitude, p.longitude]);

        const polyline = L.polyline(latlngs, {
            color: "red",
            weight: 3
        });

        polyline.addTo(map);

        map.fitBounds(polyline.getBounds());
    }

};