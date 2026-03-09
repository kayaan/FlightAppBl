window.flightDb = {
    dbName: "FlightAppDb",
    dbVersion: 1,
    db: null,

    async init() {
        if (this.db) return;

        this.db = await new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.dbVersion);

            request.onupgradeneeded = (event) => {
                const db = event.target.result;

                if (!db.objectStoreNames.contains("flights")) {
                    db.createObjectStore("flights", { keyPath: "id" });
                }

                if (!db.objectStoreNames.contains("tracks")) {
                    db.createObjectStore("tracks", { keyPath: "flightId" });
                }

                if (!db.objectStoreNames.contains("igc")) {
                    db.createObjectStore("igc", { keyPath: "flightId" });
                }
            };

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    async putFlight(flight) {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction("flights", "readwrite");
            const store = tx.objectStore("flights");
            const request = store.put(flight);

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    },

    async getFlights() {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction("flights", "readonly");
            const store = tx.objectStore("flights");
            const request = store.getAll();

            request.onsuccess = () => {
                const flights = request.result ?? [];
                flights.sort((a, b) => {
                    const da = a.date ?? "";
                    const db = b.date ?? "";
                    return db.localeCompare(da);
                });
                resolve(flights);
            };
            request.onerror = () => reject(request.error);
        });
    },

    async getFlightById(id) {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction("flights", "readonly");
            const store = tx.objectStore("flights");
            const request = store.get(id);

            request.onsuccess = () => resolve(request.result ?? null);
            request.onerror = () => reject(request.error);
        });
    },

    async putTrack(flightId, trackBinary) {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction("tracks", "readwrite");
            const store = tx.objectStore("tracks");
            const request = store.put({
                flightId: flightId,
                data: trackBinary
            });

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    },

    async getTrack(flightId) {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction("tracks", "readonly");
            const store = tx.objectStore("tracks");
            const request = store.get(flightId);

            request.onsuccess = () => resolve(request.result ? request.result.data : null);
            request.onerror = () => reject(request.error);
        });
    },

    async putIgc(flightId, igcContent) {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction("igc", "readwrite");
            const store = tx.objectStore("igc");
            const request = store.put({
                flightId: flightId,
                content: igcContent
            });

            request.onsuccess = () => resolve();
            request.onerror = () => reject(request.error);
        });
    },

    async getIgc(flightId) {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction("igc", "readonly");
            const store = tx.objectStore("igc");
            const request = store.get(flightId);

            request.onsuccess = () => resolve(request.result ? request.result.content : null);
            request.onerror = () => reject(request.error);
        });
    }
};