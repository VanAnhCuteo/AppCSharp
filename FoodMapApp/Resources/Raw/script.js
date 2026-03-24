// Initialize Map
const map = L.map('map', {
    zoomControl: false,
    attributionControl: false
}).setView([10.7672, 106.6931], 15);

L.control.zoom({ position: 'bottomright' }).addTo(map);

L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
    maxZoom: 20
}).addTo(map);

// Dynamic Marker Zoom Scaling
function updateMarkerScale() {
    const zoom = map.getZoom();
    // Default size at zoom 15, gets smaller on zoom out, grows slightly on zoom in
    if (zoom < 11.5) {
        document.documentElement.style.setProperty('--marker-scale', '0.001'); // essentially hid
    } else if (zoom > 16) {
        document.documentElement.style.setProperty('--marker-scale', '1.3');
    } else {
        // Linearly scale between 11.5 and 16
        // 11.5 -> 0.1, 15 -> 1.0
        const scale = 0.1 + ((zoom - 11.5) / 3.5) * 0.9;
        document.documentElement.style.setProperty('--marker-scale', scale);
    }
}
map.on('zoom', updateMarkerScale);
updateMarkerScale();


// Main Entry Point from C#
async function loadFoods(foods, userId = 0) {
    console.log("Loading foods into map:", foods, "User:", userId);
    const markers = [];
    allFoodsData = foods;

    if (userId > 0) {
        await syncVisitedHistory(userId);
    }

    if (markersGroup) map.removeLayer(markersGroup);
    mapMarkers = [];
    markersGroup = L.featureGroup();

    foods.forEach(food => {
        let imgSection = '';
        const rawImgUrl = food.image_url || food.imageUrl || "";
        const parsedImg = parseFirstImage(rawImgUrl);
        
        if (parsedImg && parsedImg.length > 0) {
            let imgPath = parsedImg;
            if (!imgPath.startsWith('http')) {
                if (imgPath.startsWith('/')) imgPath = imgPath.substring(1);
                if (!imgPath.startsWith('images/')) {
                    const fileName = imgPath.split('/').pop();
                    imgPath = `images/${fileName}`;
                }
            }
            console.log(`Marker ${food.id} using image path: ${imgPath}`);

            imgSection = `
                <img src="${imgPath}" class="resto-img" alt="${food.name}" loading="lazy"
                     onerror="handleImageError(this)">
                <div class="img-overlay"></div>
            `;
        } else {
            imgSection = `<div class="resto-placeholder">${iconCamera}</div>`;
        }

        const displayTime = food.open_time || "Hours Not Available";
        const displayAddress = food.address || "Unknown Location";

        const popupContent = `
            <div class="resto-card">
                <div class="resto-img-wrapper">
                    ${imgSection}
                </div>
                <div class="resto-info">
                    <h3 class="resto-name">${food.name}</h3>
                    <div class="info-row">
                        ${iconLocation}
                        <span>${displayAddress}</span>
                    </div>
                    <div class="info-row">
                        ${iconClock}
                        <span>${displayTime}</span>
                    </div>
                    <button class="resto-btn" onclick="openDetails(${food.id})">Explore</button>
                </div>
            </div>
        `;

        const marker = L.marker([food.latitude, food.longitude], { icon: pinkIcon })
            .bindPopup(popupContent, {
                closeButton: true,
                autoPanPadding: [60, 60]
            });

        marker.on('click', function (e) {
            map.flyTo(e.latlng, map.getZoom(), { animate: true, duration: 0.5 });
        });

        mapMarkers.push({ marker, food });
        markersGroup.addLayer(marker);
        markers.push([food.latitude, food.longitude]);
    });

    map.addLayer(markersGroup);

    if (markers.length > 0) {
        map.fitBounds(markers, {
            padding: [80, 80],
            maxZoom: 16,
            animate: true,
            duration: 1.2
        });
    }

    startGeofencing();
    setupMapSearch();
}

function setupMapSearch() {
    const searchInput = document.getElementById('map-search-input');
    if (!searchInput) return;

    searchInput.addEventListener('input', (e) => {
        const term = e.target.value.toLowerCase().trim();
        const normalizedTerm = removeDiacritics(term);

        mapMarkers.forEach(({ marker, food }) => {
            const name = removeDiacritics(food.name.toLowerCase());
            const addr = removeDiacritics(food.address.toLowerCase());
            const desc = removeDiacritics(food.description.toLowerCase());

            if (name.includes(normalizedTerm) || addr.includes(normalizedTerm) || desc.includes(normalizedTerm)) {
                if (!markersGroup.hasLayer(marker)) markersGroup.addLayer(marker);
            } else {
                if (markersGroup.hasLayer(marker)) markersGroup.removeLayer(marker);
            }
        });
    });
}

function removeDiacritics(text) {
    return text.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
}

async function syncVisitedHistory(userId) {
    try {
        const res = await fetch(`${platformApiBase}/history/${userId}`);
        if (res.ok) {
            const history = await res.json();
            console.log("Synced history from server:", history);
            if (Array.isArray(history)) {
                history.forEach(item => {
                    if (item.id) visitedFoods.add(item.id);
                });
            }
        }
    } catch (e) {
        console.error("History sync error:", e);
    }
}

// Global hook for C# routing
window.routeToPoi = async function(id) {
    try {
        const detailsRes = await fetch(`${platformApiBase}/${id}?lang=${selectedLanguage}`);
        if(detailsRes.ok) {
            const data = await detailsRes.json();
            if (userMarker) {
                const userCoords = userMarker.getLatLng();
                startNavigation(userCoords.lat, userCoords.lng, data.latitude, data.longitude);
            } else {
                alert("Đang xác định vị trí của bạn, vui lòng đợi chốc lát...");
            }
        }
    } catch(e) { console.error("Error fetching POI for routing", e); }
};
