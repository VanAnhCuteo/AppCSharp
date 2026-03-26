window.mapHelper = {
    map: null,
    marker: null,
    dotNetReference: null,

    initMap: function (elementId, lat, lng, zoom, dotNetRef) {
        // Remove existing map instance if any
        if (this.map) {
            this.map.remove();
        }
        
        this.dotNetReference = dotNetRef;
        
        // Default to Hanoi if coordinates are not provided (0,0)
        const initialLat = (lat === 0 && lng === 0) ? 21.0285 : lat;
        const initialLng = (lat === 0 && lng === 0) ? 105.8542 : lng;
        
        this.map = L.map(elementId).setView([initialLat, initialLng], zoom);
        
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);

        // Add draggable marker
        this.marker = L.marker([initialLat, initialLng], { 
            draggable: true 
        }).addTo(this.map);

        // Event: Marker dragged
        this.marker.on('dragend', (event) => {
            const position = event.target.getLatLng();
            this.updateCoordinates(position.lat, position.lng);
        });

        // Event: Map clicked
        this.map.on('click', (event) => {
            const position = event.latlng;
            this.marker.setLatLng(position);
            this.updateCoordinates(position.lat, position.lng);
        });
        
        // Return initial coordinates if we defaulted them
        if (lat === 0 && lng === 0) {
            this.updateCoordinates(initialLat, initialLng);
        }
    },

    updateCoordinates: function (lat, lng) {
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('UpdateCoordinates', lat, lng);
        }
    },

    setMarkerPosition: function (lat, lng) {
        if (this.marker && this.map) {
            this.marker.setLatLng([lat, lng]);
            this.map.panTo([lat, lng]);
        }
    }
};
