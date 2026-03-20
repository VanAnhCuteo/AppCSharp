// Bottom Sheet Logic
const sheet = document.getElementById('bottom-sheet');
const dragHandle = document.getElementById('drag-handle');
let startY = 0;
let isDragging = false;

if (dragHandle) {
    dragHandle.addEventListener('touchstart', (e) => {
        startY = e.touches[0].clientY;
        isDragging = true;
        sheet.style.transition = 'none';
    }, { passive: true });

    document.addEventListener('touchmove', (e) => {
        if (!isDragging) return;
        const deltaY = e.touches[0].clientY - startY;
        if (deltaY > 0) {
            sheet.style.transform = `translateY(calc(-80vh + ${deltaY}px))`;
        }
    }, { passive: true });

    document.addEventListener('touchend', (e) => {
        if (!isDragging) return;
        isDragging = false;
        sheet.style.transition = 'transform 0.3s ease-out';
        const deltaY = e.changedTouches[0].clientY - startY;

        if (deltaY > 100) {
            closeDetails();
        } else {
            sheet.style.transform = 'translateY(-80vh)';
        }
    });
}

function closeDetails() {
    sheet.classList.remove('open');
    sheet.style.transform = '';
}

let currentBasePoiId = null;
let currentDestCoords = null;
let routingLayer = null;
let navigationActive = false;

function setSelectedLang(lang, el) {
    selectedLanguage = lang;
    document.querySelectorAll('.lang-btn').forEach(btn => btn.classList.remove('active'));
    el.classList.add('active');

    // Request C# to reload markers for this language
    window.location.href = `app-request-reload://markers?lang=${lang}`;

    // Reload current sheet details if open
    if (currentBasePoiId) {
        openDetails(currentBasePoiId, lang);
    }
}

async function openDetails(poiId, lang = selectedLanguage) {
    currentBasePoiId = poiId;
    sheet.classList.add('open');
    sheet.style.transform = 'translateY(-80vh)';

    const audioBtn = document.getElementById('sheet-audio-btn');
    if (audioBtn) audioBtn.style.display = 'none';

    try {
        const detailsRes = await fetch(`${platformApiBase}/${poiId}?lang=${lang}`);
        if (detailsRes.ok) {
            const data = await detailsRes.json();
            currentPoiId = data.id; // Specific row ID

            document.getElementById('sheet-title').innerText = data.name;
            document.getElementById('sheet-visitors').innerText = `${data.visitor_count} visits`;
            document.getElementById('sheet-address').innerText = data.address || '';
            document.getElementById('sheet-time').innerText = data.open_time || '';

            currentPoiDescription = data.description || "";
            if (currentPoiDescription.trim().length > 0) {
                if (audioBtn) audioBtn.style.display = 'inline-flex';
            }

            const imgContainer = document.getElementById('sheet-images');
            imgContainer.innerHTML = ''; // Clear previous images
            const mainImg = (data.image_url || data.imageUrl || "").trim();
            let secondaryImages = (data.images || data.Images || []).map(img => img.trim());

            console.log(`Food ${poiId} details:`, data);
            currentDestCoords = [data.latitude, data.longitude];

            // Use a Set to ensure all images are unique
            let imageSet = new Set();
            if (mainImg) imageSet.add(mainImg);
            secondaryImages.forEach(img => {
                if (img) imageSet.add(img);
            });

            const uniqueImages = Array.from(imageSet);

            if (uniqueImages.length > 0) {
                uniqueImages.forEach(imgPath => {
                    if (!imgPath.startsWith('http')) {
                        if (imgPath.startsWith('/')) imgPath = imgPath.substring(1);
                        if (!imgPath.startsWith('images/')) {
                            const fileName = imgPath.split('/').pop();
                            imgPath = `images/${fileName}`;
                        }
                    }
                    const el = document.createElement('img');
                    el.className = 'sheet-image';
                    el.src = imgPath;
                    el.loading = 'lazy';
                    el.onerror = () => handleImageError(el);
                    imgContainer.appendChild(el);
                });
            } else {
                imgContainer.innerHTML = '<div style="padding: 20px; color: #888; font-style: italic;">No additional images</div>';
            }
        }
        loadReviews(poiId, lang);
        const guideRes = await fetch(`${platformApiBase}/${poiId}/guide?lang=${lang}`);
        if (guideRes.ok) {
            currentAudioGuide = await guideRes.json();
            if (audioBtn) audioBtn.style.display = 'inline-flex';
        } else {
            currentAudioGuide = null;
        }
    } catch (e) { console.error("Error fetching details", e); }
}

// Reviews Logic
async function loadReviews(poiId, lang = selectedLanguage) {
    try {
        const revRes = await fetch(`${platformApiBase}/${poiId}/reviews?lang=${lang}`);
        const revContainer = document.getElementById('sheet-reviews-list');
        revContainer.innerHTML = '';
        if (revRes.ok) {
            const reviews = await revRes.json();
            if (reviews.length === 0) {
                revContainer.innerHTML = '<p style="color: #999; font-size: 14px;">No reviews yet. Be the first!</p>';
            } else {
                reviews.forEach(r => {
                    revContainer.innerHTML += `
                        <div class="review-item">
                            <span class="review-date">${r.created_at || 'Just now'}</span>
                            <div class="review-rating">? ${r.rating}/5</div>
                            <p class="review-comment">${r.comment}</p>
                        </div>
                    `;
                });
            }
        }
    } catch (e) { console.error("Error loading reviews", e); }
}

const submitBtn = document.getElementById('submit-review-btn');
if (submitBtn) {
    submitBtn.onclick = async () => {
        if (!currentPoiId) return;
        const comment = document.getElementById('review-comment').value;
        if (!comment || comment.trim().length === 0) {
            alert("Please enter a review comment.");
            return;
        }
        try {
            const res = await fetch(`${platformApiBase}/${currentPoiId}/reviews`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ rating: 5, comment: comment, user_id: 1 })
            });
            if (res.ok) {
                document.getElementById('review-comment').value = '';
                loadReviews(currentPoiId);
            }
        } catch (e) { console.error("Failed to submit review", e); }
    };
}

// Geofencing & Audio Logic
const SIMULATE_GPS = false; // Set to false to use real GPS
let simulatedLat = 10.7672;
let simulatedLon = 106.6931;
let currentAudioGuide = null;
let currentPoiDescription = ""; // Store current POI description for TTS
let selectedLanguage = 'vi';

let poiTimers = {};
let lastGeofenceTime = 0;

function calculateDistance(lat1, lon1, lat2, lon2) {
    const R = 6371e3;
    const phi1 = lat1 * Math.PI / 180;
    const phi2 = lat2 * Math.PI / 180;
    const deltaPhi = (lat2 - lat1) * Math.PI / 180;
    const deltaLambda = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(deltaPhi / 2) * Math.sin(deltaPhi / 2) + Math.cos(phi1) * Math.cos(phi2) * Math.sin(deltaLambda / 2) * Math.sin(deltaLambda / 2);
    return R * (2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a)));
}

function startGeofencing() {
    if (SIMULATE_GPS) {
        setInterval(() => {
            processLocation({ coords: { latitude: simulatedLat, longitude: simulatedLon } });
            simulatedLon += 0.0001;
        }, 3000);
        return;
    }
    if ("geolocation" in navigator) {
        navigator.geolocation.watchPosition(processLocation, (err) => {
            console.log(err);
            alert("Geolocation Error: " + err.message + " (Code: " + err.code + ")");
        }, { enableHighAccuracy: true });
    }
}

function processLocation(position) {
    const now = Date.now();
    if (now - lastGeofenceTime < 3000) return;
    lastGeofenceTime = now;
    const userLat = position.coords.latitude;
    const userLon = position.coords.longitude;

    // Show/Update User Marker
    if (!userMarker) {
        userMarker = L.marker([userLat, userLon], {
            icon: blueLocationIcon,
            zIndexOffset: 1000
        }).addTo(map);
    } else {
        userMarker.setLatLng([userLat, userLon]);
    }

    allFoodsData.forEach(food => {
        if (!visitedFoods.has(food.id)) {
            const d = calculateDistance(userLat, userLon, food.latitude, food.longitude);
            if (d <= 50) {
                if (!poiTimers[food.id]) {
                    poiTimers[food.id] = setTimeout(() => {
                        visitedFoods.add(food.id);
                        handleVisit(food.id);
                        delete poiTimers[food.id];
                    }, 8000);
                }
            } else if (poiTimers[food.id]) {
                clearTimeout(poiTimers[food.id]);
                delete poiTimers[food.id];
            }
        }
    });
}

async function handleVisit(poiId) {
    try {
        await fetch(`${platformApiBase}/${poiId}/visit`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ user_id: 1 })
        });
        const guideRes = await fetch(`${platformApiBase}/${poiId}/guide?lang=${selectedLanguage}`);
        if (guideRes.ok) {
            const guide = await guideRes.json();
            playAudioGuide(`${guide.title}. ${guide.description || ''}`, guide.language);
        }
    } catch (e) { console.error("Geofence error", e); }
}

function playCurrentAudio() {
    if (currentAudioGuide) {
        playAudioGuide(`${currentAudioGuide.title}. ${currentAudioGuide.description || ''}`, currentAudioGuide.language || selectedLanguage);
    } else if (currentPoiDescription) {
        const title = document.getElementById('sheet-title').innerText;
        playAudioGuide(`${title}. ${currentPoiDescription}`, selectedLanguage);
    }
}

function playAudioGuide(text, language = 'vi-VN') {
    if (text) {
        window.location.href = `app-tts://speak?text=${encodeURIComponent(text)}&lang=${language}`;
    }
}

function centerOnUser() {
    if (userMarker) {
        map.flyTo(userMarker.getLatLng(), 17, { animate: true, duration: 1 });
    } else {
        alert("Waiting for GPS location...");
    }
}

const directionsBtn = document.getElementById('get-directions-btn');
if (directionsBtn) {
    directionsBtn.onclick = () => {
        if (currentDestCoords && userMarker) {
            const userCoords = userMarker.getLatLng();
            startNavigation(userCoords.lat, userCoords.lng, currentDestCoords[0], currentDestCoords[1]);
            closeDetails();
        } else {
            alert("?ang xác ??nh v? trí c?a b?n...");
        }
    };
}

async function startNavigation(slat, slon, dlat, dlon) {
    if (routingLayer) map.removeLayer(routingLayer);

    // OSRM expects [lon, lat]
    const url = `https://router.project-osrm.org/route/v1/driving/${slon},${slat};${dlon},${dlat}?overview=full&geometries=geojson`;

    try {
        const res = await fetch(url);
        const data = await res.json();

        if (data.code === 'Ok') {
            const route = data.routes[0];
            const distance = (route.distance / 1000).toFixed(1); // km

            // Draw Route
            routingLayer = L.geoJSON(route.geometry, {
                style: {
                    color: '#FB6F92',
                    weight: 6,
                    opacity: 0.8,
                    lineJoin: 'round'
                }
            }).addTo(map);

            // Fit bounds
            map.fitBounds(routingLayer.getBounds(), { padding: [50, 50] });

            // Update UI
            document.getElementById('nav-distance').innerText = `${distance} km`;
            document.getElementById('nav-overlay').classList.remove('hidden');
            navigationActive = true;
        }
    } catch (e) {
        console.error("Routing error", e);
        alert("Không th? tìm ???ng ?i lúc này.");
    }
}

function cancelNavigation() {
    if (routingLayer) {
        map.removeLayer(routingLayer);
        routingLayer = null;
    }
    document.getElementById('nav-overlay').classList.add('hidden');
    navigationActive = false;
}
