// Global State
let allFoodsData = [];
const visitedFoods = new Set();
let currentPoiId = null;
const platformApiBase = window.navigator.userAgent.includes("Android") ? "http://10.0.2.2:5000/api/food" : "http://localhost:5000/api/food";

// Initialize Map
const map = L.map('map', {
    zoomControl: false,
    attributionControl: false
}).setView([10.7672, 106.6931], 15);

L.control.zoom({ position: 'bottomright' }).addTo(map);

L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
    maxZoom: 20
}).addTo(map);

// Modern 3D/Gradient aesthetic SVG marker
const markerSvg = `
<svg width="46" height="52" viewBox="0 0 46 52" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M23 2C11.954 2 3 10.954 3 22C3 34.5 23 50 23 50C23 50 43 34.5 43 22C43 10.954 34.046 2 23 2Z" fill="url(#grad_marker)"/>
    <path d="M23 2C11.954 2 3 10.954 3 22C3 34.5 23 50 23 50C23 50 43 34.5 43 22C43 10.954 34.046 2 23 2Z" fill="#FB6F92" fill-opacity="0.9"/>
    <path d="M23 2C11.954 2 3 10.954 3 22C3 22.463 3.033 22.915 3.09 23.356C4.414 13.064 12.876 5 23 5C33.124 5 41.586 13.064 42.91 23.356C42.967 22.915 43 22.463 43 22C43 10.954 34.046 2 23 2Z" fill="white" fill-opacity="0.4"/>
    <circle cx="23" cy="22" r="9" fill="white"/>
    <defs>
        <linearGradient id="grad_marker" x1="23" y1="2" x2="23" y2="50" gradientUnits="userSpaceOnUse">
            <stop stop-color="#FFBA08"/>
            <stop offset="1" stop-color="#FB6F92"/>
        </linearGradient>
    </defs>
</svg>`;

const pinkIcon = L.divIcon({
    className: 'custom-pink-marker',
    html: '<div class="marker-pulse"></div>' + markerSvg,
    iconSize: [46, 52],
    iconAnchor: [23, 50],
    popupAnchor: [0, -46]
});

const iconLocation = `<svg class="info-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path></svg>`;
const iconClock = `<svg class="info-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>`;
const iconCamera = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"></path><circle cx="12" cy="13" r="4"></circle></svg>`;

function loadFoods(foods) {
    const markers = [];
    allFoodsData = foods; // Store for geofencing

    const markersGroup = L.markerClusterGroup({
        chunkedLoading: true,
        maxClusterRadius: 50
    });

    foods.forEach(food => {
        let imgSection = '';
        if (food.image_url && food.image_url.trim().length > 0) {
            let imgPath = food.image_url.trim();
            if (!imgPath.startsWith('http')) {
                if (imgPath.startsWith('/')) {
                    imgPath = imgPath.substring(1);
                }
                if (!imgPath.startsWith('images/')) {
                    const fileName = imgPath.split('/').pop();
                    imgPath = `images/${fileName}`;
                }
                imgPath = `../../../${imgPath}`;
            }

            imgSection = `
                <img src="${imgPath}" class="resto-img" alt="${food.name}" 
                     onerror="this.onerror=null; this.parentElement.innerHTML='<div class=\\'resto-placeholder\\'>${iconCamera}</div>';">
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
            map.flyTo(e.latlng, map.getZoom(), {
                animate: true,
                duration: 0.5
            });
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

    // Start geolocation tracking
    startGeofencing();
}

// Bottom Sheet Elements
const sheet = document.getElementById('bottom-sheet');
const dragHandle = document.getElementById('drag-handle');
let startY = 0;
let isDragging = false;

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

function closeDetails() {
    sheet.classList.remove('open');
    sheet.style.transform = '';
}

// Action when 'Explore' clicked
async function openDetails(foodId) {
    currentPoiId = foodId;

    // Slide up panel
    sheet.classList.add('open');
    sheet.style.transform = 'translateY(-80vh)';

    const audioBtn = document.getElementById('sheet-audio-btn');
    if (audioBtn) audioBtn.style.display = 'none';

    // Load data
    try {
        const detailsRes = await fetch(`${platformApiBase}/${foodId}`);
        if (detailsRes.ok) {
            const data = await detailsRes.json();
            document.getElementById('sheet-title').innerText = data.name;
            document.getElementById('sheet-visitors').innerText = `${data.visitor_count} visits`;
            document.getElementById('sheet-address').innerText = data.address || '';
            document.getElementById('sheet-time').innerText = data.open_time || '';

            // Images
            const imgContainer = document.getElementById('sheet-images');
            imgContainer.innerHTML = '';
            if (data.images && data.images.length > 0) {
                data.images.forEach(img => {
                    let imgPath = img.trim();
                    if (!imgPath.startsWith('http')) {
                        if (imgPath.startsWith('/')) {
                            imgPath = imgPath.substring(1);
                        }
                        if (!imgPath.startsWith('images/')) {
                            const fileName = imgPath.split('/').pop();
                            imgPath = `images/${fileName}`;
                        }
                        //imgPath = `http://10.0.2.2:5000/${imgPath}`;
                        console.log(imgPath);
                    }
                    const el = document.createElement('img');
                    el.className = 'sheet-image';
                    el.src = imgPath;
                    imgContainer.appendChild(el);
                });
            } else {
                imgContainer.innerHTML = '<div style="padding: 20px; color: #888; font-style: italic;">No additional images</div>';
            }
        }

        loadReviews(foodId);

        // Fetch guide for manual play
        const guideRes = await fetch(`${platformApiBase}/${foodId}/guide`);
        if (guideRes.ok) {
            currentAudioGuide = await guideRes.json();
            if (audioBtn) audioBtn.style.display = 'inline-flex';
        } else {
            currentAudioGuide = null;
        }

    } catch (e) {
        console.error("Error fetching details", e);
    }
}

async function loadReviews(foodId) {
    try {
        const revRes = await fetch(`${platformApiBase}/${foodId}/reviews`);
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
                            <div class="review-rating">★ ${r.rating}/5</div>
                            <p class="review-comment">${r.comment}</p>
                        </div>
                    `;
                });
            }
        }
    } catch (e) { console.error("Error loading reviews", e); }
}

document.getElementById('submit-review-btn').onclick = async () => {
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
            loadReviews(currentPoiId); // reload
        }
    } catch (e) {
        console.error("Failed to submit review", e);
    }
}

// ---------------- GEOFENCING LOGIC ----------------

// Fake GPS Simulation settings
const SIMULATE_GPS = true; // Set to true to override geolocation with Bùi Viện Walk
let simulatedLat = 10.7672;
let simulatedLon = 106.6931;

let currentAudioGuide = null;
let poiTimers = {}; // { foodId: timeoutId }

function calculateDistance(lat1, lon1, lat2, lon2) {
    const R = 6371e3; // metres
    const phi1 = lat1 * Math.PI / 180;
    const phi2 = lat2 * Math.PI / 180;
    const deltaPhi = (lat2 - lat1) * Math.PI / 180;
    const deltaLambda = (lon2 - lon1) * Math.PI / 180;

    const a = Math.sin(deltaPhi / 2) * Math.sin(deltaPhi / 2) +
        Math.cos(phi1) * Math.cos(phi2) *
        Math.sin(deltaLambda / 2) * Math.sin(deltaLambda / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

    return R * c; // in metres
}

let lastGeofenceTime = 0;
function startGeofencing() {
    if (SIMULATE_GPS) {
        // Simulate walking around Bui Vien
        setInterval(() => {
            const position = { coords: { latitude: simulatedLat, longitude: simulatedLon } };
            processLocation(position);
            // Move longitude slightly to simulate walking
            simulatedLon += 0.0001;
        }, 3000);
        return;
    }

    if ("geolocation" in navigator) {
        navigator.geolocation.watchPosition((position) => {
            processLocation(position);
        }, (err) => console.log(err), { enableHighAccuracy: true });
    }
}

function processLocation(position) {
    const now = Date.now();
    if (now - lastGeofenceTime < 3000) return; // 3s throttle
    lastGeofenceTime = now;

    const userLat = position.coords.latitude;
    const userLon = position.coords.longitude;

    allFoodsData.forEach(food => {
        if (!visitedFoods.has(food.id)) {
            const d = calculateDistance(userLat, userLon, food.latitude, food.longitude);
            if (d <= 50) { // within 50 meters
                if (!poiTimers[food.id]) {
                    console.log(`Entered radius of ${food.name}. Starting 8s timer.`);
                    poiTimers[food.id] = setTimeout(() => {
                        console.log(`Stayed 8s at ${food.name}. Triggering audio guide.`);
                        visitedFoods.add(food.id);
                        handleVisit(food.id);
                        delete poiTimers[food.id];
                    }, 8000);
                }
            } else {
                if (poiTimers[food.id]) {
                    console.log(`Exited radius of ${food.name} before 8s. Cancelling timer.`);
                    clearTimeout(poiTimers[food.id]);
                    delete poiTimers[food.id];
                }
            }
        }
    });
}

async function handleVisit(foodId) {
    try {
        // 1. Increment visit count
        await fetch(`${platformApiBase}/${foodId}/visit`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ user_id: 1 })
        });

        // 2. Fetch and play guide
        const guideRes = await fetch(`${platformApiBase}/${foodId}/guide`);
        if (guideRes.ok) {
            const guide = await guideRes.json();
            const textToSpeech = `${guide.title}. ${guide.description || ''}`;
            playAudioGuide(textToSpeech, guide.language);
        }
    } catch (e) {
        console.error("Geofence error", e);
    }
}

function playCurrentAudio() {
    if (currentAudioGuide) {
        const textToSpeech = `${currentAudioGuide.title}. ${currentAudioGuide.description || ''}`;
        playAudioGuide(textToSpeech, currentAudioGuide.language);
    }
}

function playAudioGuide(text, language = 'vi-VN') {
    if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
        const msg = new SpeechSynthesisUtterance(text);
        msg.lang = language || 'vi-VN';
        window.speechSynthesis.speak(msg);
    }
}
