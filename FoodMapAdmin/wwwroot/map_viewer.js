window.mapViewer = {
    map: null,
    markers: [],
    heatmapLayer: null,

    initMap: function (elementId, lat, lng, zoom) {
        try {
            const container = document.getElementById(elementId);
            if (!container) {
                console.warn("Map container not found: " + elementId);
                return false;
            }

            if (this.map) {
                this.disposeMap();
            }
            
            this.map = L.map(elementId).setView([lat, lng], zoom);
            
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; OpenStreetMap contributors'
            }).addTo(this.map);
            
            console.log("[MapViewer] Map initialized at:", lat, lng);
            return true;
        } catch (e) {
            console.error("Error initializing map:", e);
            return false;
        }
    },

    updateMarkers: function (pois) {
        try {
            this.clearMarkers();
            if (!this.map || !pois) return;

            console.log("[MapViewer] Creating premium teardrop markers for:", pois);
            const markerGroup = [];

            const customIcon = L.divIcon({
                html: `
                    <div class="marker-pin-wrapper" style="filter: drop-shadow(0 4px 8px rgba(0,0,0,0.4));">
                        <svg width="34" height="44" viewBox="0 0 32 42" fill="none" xmlns="http://www.w3.org/2000/svg">
                            <path d="M16 0C7.16344 0 0 7.16344 0 16C0 28 16 42 16 42C16 42 32 28 32 16C32 7.16344 24.8366 0 16 0Z" fill="#ff2d85"/>
                            <circle cx="16" cy="16" r="6" fill="white"/>
                        </svg>
                    </div>
                `,
                className: 'custom-div-icon',
                iconSize: [34, 44],
                iconAnchor: [17, 44],
                popupAnchor: [0, -42]
            });

            pois.forEach(poi => {
                const lat = parseFloat(poi.latitude ?? poi.Latitude ?? poi.lat ?? poi.Lat);
                const lng = parseFloat(poi.longitude ?? poi.Longitude ?? poi.lng ?? poi.Lng);
                const name = poi.name ?? poi.Name;
                const addr = poi.address ?? poi.Address;
                const id = poi.poiId ?? poi.PoiId;

                if (!isNaN(lat) && !isNaN(lng)) {
                    const marker = L.marker([lat, lng], { icon: customIcon })
                    .bindPopup(`
                        <div class="map-popup-card">
                            <div class="popup-header">
                                <h3 class="popup-title">${name || 'Địa điểm'}</h3>
                            </div>
                            <div class="popup-body">
                                <p class="popup-addr"><i class="bi bi-geo-alt-fill text-danger me-1"></i> ${addr || 'Chưa cập nhật địa chỉ'}</p>
                                <hr class="popup-divider"/>
                                <div class="d-flex justify-content-between align-items-center">
                                    <span class="text-muted small">ID: #${id}</span>
                                    <button class="btn btn-sm btn-primary py-1 px-3" style="border-radius: 8px; font-weight: 600;" onclick="location.href='/pois/edit/${id}'">
                                        Chi tiết <i class="bi bi-arrow-right-short"></i>
                                    </button>
                                </div>
                            </div>
                        </div>
                    `, {
                        closeButton: false,
                        className: 'custom-leaflet-popup'
                    })
                    .addTo(this.map);

                    marker.bindTooltip(name || 'Quán ăn', { 
                        permanent: false, 
                        direction: 'top',
                        className: 'custom-map-tooltip'
                    });
                    
                    this.markers.push(marker);
                    markerGroup.push(marker);
                }
            });

            if (markerGroup.length > 0) {
                const group = new L.FeatureGroup(markerGroup);
                const bounds = group.getBounds();
                if (bounds.isValid()) {
                    this.map.fitBounds(bounds, { padding: [50, 50], maxZoom: 16 });
                }
            }
        } catch (e) {
            console.error("Error updating markers:", e);
        }
    },

    clearMarkers: function () {
        if (!this.map) return;
        this.markers.forEach(m => {
            try { this.map.removeLayer(m); } catch(e) {}
        });
        this.markers = [];
    },

    updateHeatmap: function (dataPoints) {
        try {
            if (!this.map || !dataPoints) return;

            console.log("[MapViewer] Data received for heatmap:", dataPoints);

            const normalizedData = dataPoints.map(p => ({
                lat: parseFloat(p.lat ?? p.Lat ?? p.latitude ?? p.Latitude),
                lng: parseFloat(p.lng ?? p.Lng ?? p.longitude ?? p.Longitude),
                count: parseInt(p.count ?? p.Count ?? 1)
            })).filter(p => !isNaN(p.lat) && !isNaN(p.lng));

            if (normalizedData.length === 0) {
                console.warn("[MapViewer] No valid coordinates found for heatmap.");
                this.clearHeatmap();
                return;
            }

            const cfg = {
                radius: 0.0005,
                maxOpacity: .9,
                blur: .80,
                scaleRadius: true,
                useLocalExtrema: false,
                latField: 'lat',
                lngField: 'lng',
                valueField: 'count',
                gradient: {
                    '.1': '#0000FF',
                    '.25': '#00FFFF',
                    '.45': '#00FF00',
                    '.65': '#FFFF00',
                    '.85': '#FF8000',
                    '1.0': '#FF0000'
                }
            };

            if (typeof HeatmapOverlay === 'undefined') {
                console.error("[MapViewer] HeatmapOverlay is not defined.");
                return;
            }

            // Reuse existing layer to avoid flickering
            if (!this.heatmapLayer) {
                this.heatmapLayer = new HeatmapOverlay(cfg);
                this.map.addLayer(this.heatmapLayer);
            }
            
            this.heatmapLayer.setData({
                max: 2,
                data: normalizedData
            });

            // fitBounds only if requested or on first load (disabled by default for smooth updates)
            // const bounds = L.latLngBounds(normalizedData.map(p => [p.lat, p.lng]));
            // if (bounds.isValid()) {
            //     this.map.fitBounds(bounds, { padding: [50, 50], maxZoom: 16 });
            // }
        } catch (e) {
            console.error("Error updating heatmap:", e);
        }
    },

    clearHeatmap: function () {
        try {
            if (this.heatmapLayer && this.map) {
                this.map.removeLayer(this.heatmapLayer);
                this.heatmapLayer = null;
            }
        } catch (e) {}
    },

    disposeMap: function () {
        try {
            if (this.map) {
                this.clearMarkers();
                this.clearHeatmap();
                this.map.remove();
                this.map = null;
            }
        } catch (e) {
            console.error("Error disposing map:", e);
        }
    }
};
