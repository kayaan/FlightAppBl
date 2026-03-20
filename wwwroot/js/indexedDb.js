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

                let flightsStore;

                if (!db.objectStoreNames.contains("flights")) {
                    flightsStore = db.createObjectStore("flights", { keyPath: "id" });
                } else {
                    flightsStore = event.target.transaction.objectStore("flights");
                }

                if (!flightsStore.indexNames.contains("fileHash")) {
                    flightsStore.createIndex("fileHash", "fileHash", { unique: true });
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

    async getFlightByFileHashAsync(fileHash) {
        await this.init();

        if (!fileHash) {
            return null;
        }

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction("flights", "readonly");
            const store = tx.objectStore("flights");
            const index = store.index("fileHash");
            const request = index.get(fileHash);

            request.onsuccess = () => resolve(request.result ?? null);
            request.onerror = () => reject(request.error);
        });
    },

    async saveFlightAggregate(flight, trackBinary, igcContent) {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction(["flights", "tracks", "igc"], "readwrite");

            tx.objectStore("flights").put(flight);

            tx.objectStore("tracks").put({
                flightId: flight.id,
                data: trackBinary
            });

            tx.objectStore("igc").put({
                flightId: flight.id,
                content: igcContent
            });

            tx.oncomplete = () => resolve();
            tx.onerror = () => reject(tx.error);
            tx.onabort = () => reject(tx.error);
        });
    },

    async deleteFlight(flightId) {
        await this.init();

        return await new Promise((resolve, reject) => {
            const tx = this.db.transaction(["flights", "tracks", "igc"], "readwrite");

            tx.objectStore("flights").delete(flightId);
            tx.objectStore("tracks").delete(flightId);
            tx.objectStore("igc").delete(flightId);

            tx.oncomplete = () => resolve();
            tx.onerror = () => reject(tx.error);
            tx.onabort = () => reject(tx.error);
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