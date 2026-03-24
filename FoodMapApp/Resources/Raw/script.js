// Initialize Map
const map = L.map('map', {
    zoomControl: false,
    attributionControl: false
}).setView([10.7672, 106.6931], 15);

L.control.zoom({ position: 'bottomright' }).addTo(map);

L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
    maxZoom: 20
}).addTo(map);

// Main Entry Point from C#
async function loadFoods(foods, userId = 0) {
    console.log("Loading foods into map:", foods, "User:", userId);
    const markers = [];
    allFoodsData = foods;

    if (userId > 0) {
        await syncVisitedHistory(userId);
    }

    const markersGroup = L.featureGroup();

    foods.forEach(food => {
        let imgSection = '';
        const rawImgUrl = food.image_url || food.imageUrl || "";
        if (rawImgUrl && rawImgUrl.trim().length > 0) {
            let imgPath = rawImgUrl.trim();
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
