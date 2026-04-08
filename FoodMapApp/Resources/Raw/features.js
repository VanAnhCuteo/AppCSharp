/* ── GLOBAL STATE ── */
let selectedLanguage = 'vi';
let currentBasePoiId = null;     // Current POI ID being viewed
let currentPoiDescription = "";  // Store current POI description for TTS
let currentAudioGuide = null;
let currentDestCoords = null;
let routingLayer = null;
let navigationActive = false;
let navigatingPoiId = null; // New: Tracks the POI currently being navigated to

// Bottom Sheet State
const sheet = document.getElementById('bottom-sheet');
const dragHandle = document.getElementById('drag-handle');
let startY = 0;
let isDragging = false;

/* ── BOTTOM SHEET LOGIC ── */
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
    console.log("DEBUG: Closing details sheet");
    sheet.classList.remove('open');
    sheet.style.transform = '';
}

async function setSelectedLang(lang, el) {
    console.log(`DEBUG: Language switched to: ${lang}`);
    const wasPlaying = isAudioSpeaking && !isAudioPaused;
    
    selectedLanguage = lang;
    document.querySelectorAll('.lang-btn').forEach(btn => btn.classList.remove('active'));
    el.classList.add('active');

    // Request C# to reload markers for this language
    window.location.href = `app-request-reload://markers?lang=${lang}`;

    // Reload current sheet details if open and play audio
    if (currentBasePoiId) {
        await openDetails(currentBasePoiId, lang);
        if (wasPlaying) {
            resumeAudioProfessional();
        }
    }
}

async function openDetails(poiId, lang = selectedLanguage) {
    console.log(`DEBUG: openDetails(poiId=${poiId}, lang=${lang}) called.`);
    if (!poiId) {
        console.error("DEBUG: openDetails called with null/empty poiId");
        return;
    }

    const isSamePoi = (currentBasePoiId === poiId);
    if (!isSamePoi) {
        resetPlayerForNewPoi();
    }
    
    currentBasePoiId = poiId;
    sheet.classList.remove('hidden');
    sheet.classList.add('open');
    sheet.style.transform = 'translateY(-80vh)';

    const audioSection = document.getElementById('sheet-audio-section');
    if (audioSection) audioSection.classList.add('hidden');

    try {
        console.log(`DEBUG: Fetching details from: ${platformApiBase}/${poiId}?lang=${lang}`);
        const detailsRes = await fetch(`${platformApiBase}/${poiId}?lang=${lang}`);
        if (detailsRes.ok) {
            const data = await detailsRes.json();
            console.log("DEBUG: POI details loaded successfully:", data);
            
            currentPoiId = data.id; 
            document.getElementById('sheet-title').innerText = data.name;
            document.getElementById('sheet-visitors').innerText = `${data.visitor_count} visits`;
            document.getElementById('sheet-address').innerText = data.address || '';
            document.getElementById('sheet-time').innerText = data.open_time || '';

            currentPoiDescription = data.description || "";
            if (currentPoiDescription.trim().length > 0) {
                if (audioSection) audioSection.classList.remove('hidden');
            }

            const imgContainer = document.getElementById('sheet-images');
            imgContainer.innerHTML = ''; // Clear previous images
            const rawImgUrl = data.image_url || data.imageUrl || ""; 
            let parsedImages = [];
            if (typeof parseAllImages === 'function') {
                parsedImages = parseAllImages(rawImgUrl);
            } else {
                parsedImages = [rawImgUrl];
            }
            let secondaryImages = (data.images || data.Images || []).map(img => img.trim());

            console.log(`Food ${poiId} details:`, data);
            currentDestCoords = [data.latitude, data.longitude];

            // Use a Set to ensure all images are unique
            let imageSet = new Set();
            parsedImages.forEach(img => { if (img) imageSet.add(img); });
            secondaryImages.forEach(img => {
                if (img) imageSet.add(img);
            });

            const uniqueImages = Array.from(imageSet);

            if (uniqueImages.length > 0) {
                uniqueImages.forEach(imgPath => {
                    if (!imgPath.startsWith('http')) {
                        if (imgPath.startsWith('/')) imgPath = imgPath.substring(1);
                        
                        // Prefix with server URL (platformApiBase is http://.../api/Food)
                        const serverBase = platformApiBase.split('/api')[0];
                        
                        // Ensure it includes the /images/ path segment if missing
                        if (!imgPath.toLowerCase().startsWith('images/')) {
                            imgPath = `images/${imgPath}`;
                        }
                        
                        imgPath = `${serverBase}/${imgPath}`;
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
            if (audioSection) audioSection.classList.remove('hidden');
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
                            <div class="review-rating">⭐ ${r.rating}/5</div>
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

let poiAudioTimers = {};
let poiHistoryTimers = {};
let playedAudioPois = new Set();
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
    if (now - lastGeofenceTime < 2000) return;
    lastGeofenceTime = now;
    const userLat = position.coords.latitude;
    const userLon = position.coords.longitude;

    if (!userMarker) {
        userMarker = L.marker([userLat, userLon], { icon: blueLocationIcon, zIndexOffset: 1000 }).addTo(map);
    } else {
        userMarker.setLatLng([userLat, userLon]);
    }

    // 1. Tính toán khoảng cách tất cả các quán và tìm quán gần nhất tuyệt đối
    let allDistances = allFoodsData.map(food => {
        const dist = calculateDistance(userLat, userLon, food.latitude, food.longitude);
        return { ...food, distance: dist };
    }).sort((a, b) => a.distance - b.distance);

    let absoluteClosestPoi = allDistances.length > 0 ? allDistances[0] : null;

    // 2. Lọc danh sách quán trong phạm vi 50m để phát Audio
    let poisInRange = allDistances.filter(f => f.distance <= (f.range_meters || 50));
    let closestInRangePoi = poisInRange.length > 0 ? poisInRange[0] : null;

    if (poisInRange.length > 0) {
        console.log(`DEBUG: User in range of ${poisInRange.length} POIs. Near: ${poisInRange[0].name}`);
    }

    allFoodsData.forEach(food => {
        // Đổi màu XANH cho quán gần nhất tuyệt đối
        const isClosest = absoluteClosestPoi && absoluteClosestPoi.id === food.id;
        // Đổi màu XANH LÁ nếu đang dẫn đường
        const isNavigating = navigatingPoiId === food.id;
        
        const markerData = mapMarkers.find(m => m.food.id === food.id);
        if (markerData && markerData.marker) {
            if (isNavigating) {
                markerData.marker.setIcon(greenIcon);
            } else if (isClosest) {
                markerData.marker.setIcon(blueIcon);
            } else {
                markerData.marker.setIcon(pinkIcon);
            }
        }

        // Logic phát Audio tự động (Chỉ khi vào đúng phạm vi 50m)
        if (closestInRangePoi && closestInRangePoi.id === food.id) {
            if (!playedAudioPois.has(food.id) && !poiAudioTimers[food.id]) {
                poiAudioTimers[food.id] = setTimeout(() => {
                    if (!playedAudioPois.has(food.id)) {
                        playedAudioPois.add(food.id);
                        triggerAutoAudio(food.id);
                    }
                    delete poiAudioTimers[food.id];
                }, 5000);
            }
            if (currentUserId > 0 && !visitedFoods.has(food.id) && !poiHistoryTimers[food.id]) {
                poiHistoryTimers[food.id] = setTimeout(() => {
                    if (currentUserId > 0 && !visitedFoods.has(food.id)) {
                        visitedFoods.add(food.id);
                        handleVisit(food.id);
                    }
                    delete poiHistoryTimers[food.id];
                }, 1000);
            }
        } else {
            if (poiAudioTimers[food.id]) { clearTimeout(poiAudioTimers[food.id]); delete poiAudioTimers[food.id]; }
            if (poiHistoryTimers[food.id]) { clearTimeout(poiHistoryTimers[food.id]); delete poiHistoryTimers[food.id]; }
        }
    });
}

async function triggerAutoAudio(poiId) {
    try {
        const guideRes = await fetch(`${platformApiBase}/${poiId}/guide?lang=${selectedLanguage}`);
        if (guideRes.ok) {
            const guide = await guideRes.json();
            playAudioGuide(`${guide.title}. ${guide.description || ''}`, guide.language || selectedLanguage, poiId);
        } else {
            const detailsRes = await fetch(`${platformApiBase}/${poiId}?lang=${selectedLanguage}`);
            if (detailsRes.ok) {
                const data = await detailsRes.json();
                if (data.description) playAudioGuide(`${data.name}. ${data.description}`, selectedLanguage, poiId);
            }
        }
    } catch (e) { console.error("Auto-audio error", e); }
}

async function handleVisit(poiId) {
    console.log(`DEBUG: handleVisit attempt for POI: ${poiId}. Authenticated User: ${currentUserId}`);
    if (currentUserId <= 0) {
        console.warn("DEBUG: Skipping visit log - User not authenticated.");
        return;
    }

    try {
        const visitUrl = `${platformApiBase}/${poiId}/visit`;
        console.log(`DEBUG: Sending visit log to: ${visitUrl}`);
        const res = await fetch(visitUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ user_id: currentUserId })
        });
        console.log(`DEBUG: Visit log response: ${res.status}`);
        const guideRes = await fetch(`${platformApiBase}/${poiId}/guide?lang=${selectedLanguage}`);
        if (guideRes.ok) {
            const guide = await guideRes.json();
            playAudioGuide(`${guide.title}. ${guide.description || ''}`, guide.language);
        }
    } catch (e) { console.error("Geofence error", e); }
}

// ── AUDIO PLAYER LOGIC ──
let audioTimerInterval = null;
let audioSeconds = 0;
let isAudioSpeaking = false;
let isAudioPaused = false;

function toggleAudioProfessional() {
    if (isAudioSpeaking && !isAudioPaused) {
        pauseAudioProfessional();
    } else {
        resumeAudioProfessional();
    }
}

function resumeAudioProfessional() {
    isAudioSpeaking = true;
    isAudioPaused = false;
    
    const audioCard = document.querySelector('.audio-card');
    if (audioCard) {
        audioCard.classList.remove('hidden');
        audioCard.classList.add('audio-playing');
    }
    
    // Update play/pause icon and text
    const playIcon = document.getElementById('main-play-icon');
    const pauseIcon = document.getElementById('main-pause-icon');
    const statusText = document.getElementById('audio-status-text');

    if (playIcon) playIcon.classList.add('hidden');
    if (pauseIcon) pauseIcon.classList.remove('hidden');
    if (statusText) statusText.innerText = "Đang phát...";

    startAudioTimer();
    playCurrentAudio();
}

function pauseAudioProfessional() {
    isAudioPaused = true;
    const audioCard = document.querySelector('.audio-card');
    if (audioCard) audioCard.classList.remove('audio-playing');
    
    const playIcon = document.getElementById('main-play-icon');
    const pauseIcon = document.getElementById('main-pause-icon');
    const statusText = document.getElementById('audio-status-text');

    if (playIcon) playIcon.classList.remove('hidden');
    if (pauseIcon) pauseIcon.classList.add('hidden');
    if (statusText) statusText.innerText = "Đã tạm dừng";

    stopAudioTimer();
    window.location.href = `app-tts://stop?id=${currentPoiId}&reset=false`;
}

function stopAudioProfessional() {
    const wasSpeaking = isAudioSpeaking || isAudioPaused;
    isAudioSpeaking = false;
    isAudioPaused = false;
    
    const audioCard = document.querySelector('.audio-card');
    if (audioCard) {
        // Keep it visible but remove playing animation
        audioCard.classList.remove('audio-playing');
    }

    const playIcon = document.getElementById('main-play-icon');
    const pauseIcon = document.getElementById('main-pause-icon');
    const statusText = document.getElementById('audio-status-text');

    if (playIcon) playIcon.classList.remove('hidden');
    if (pauseIcon) pauseIcon.classList.add('hidden');
    if (statusText) statusText.innerText = "Bản dịch Audio";
    
    resetAudioTimer();
    const progressFill = document.getElementById('audio-progress-fill');
    if (progressFill) progressFill.style.width = '0%';

    if (wasSpeaking) {
        window.location.href = `app-tts://stop?id=${currentPoiId}&reset=true`;
    }
}

function restartAudio() {
    // Send stop with reset=true to C#
    window.location.href = `app-tts://stop?reset=true`;
    
    // Reset local state
    isAudioSpeaking = false;
    isAudioPaused = false;
    resetAudioTimer();
    const progressFill = document.getElementById('audio-progress-fill');
    if (progressFill) progressFill.style.width = '0%';
    
    // Start fresh
    setTimeout(() => {
        resumeAudioProfessional();
    }, 100);
}

function playCurrentAudio() {
    let text = "";
    if (currentAudioGuide) {
        text = `${currentAudioGuide.title}. ${currentAudioGuide.description || ''}`;
    } else if (currentPoiDescription) {
        const title = document.getElementById('sheet-title').innerText;
        text = `${title}. ${currentPoiDescription}`;
    }

    if (text) {
        window.location.href = `app-tts://speak?id=${currentPoiId}&text=${encodeURIComponent(text)}&lang=${selectedLanguage}`;
    }
}

function playAudioGuide(text, language = 'vi-VN', poiId = currentPoiId) {
    if (text) {
        window.location.href = `app-tts://speak?id=${poiId}&text=${encodeURIComponent(text)}&lang=${language}`;
    }
}

// Timer Logic
function startAudioTimer() {
    if (audioTimerInterval) return;
    audioTimerInterval = setInterval(() => {
        audioSeconds++;
        const mins = Math.floor(audioSeconds / 60).toString().padStart(2, '0');
        const secs = (audioSeconds % 60).toString().padStart(2, '0');
        const timerEl = document.getElementById('mini-player-timer');
        if (timerEl) timerEl.innerText = `${mins}:${secs}`;
    }, 1000);
}

function stopAudioTimer() {
    if (audioTimerInterval) {
        clearInterval(audioTimerInterval);
        audioTimerInterval = null;
    }
}

function resetAudioTimer() {
    stopAudioTimer();
    audioSeconds = 0;
    const timerEl = document.getElementById('mini-player-timer');
    if (timerEl) timerEl.innerText = "00:00";
}

// Callbacks from C#
window.onTtsProgress = function(index, total) {
    console.log(`TTS Progress: ${index + 1}/${total}`);
    const progressFill = document.getElementById('audio-progress-fill');
    if (progressFill && total > 0) {
        const percent = ((index + 1) / total) * 100;
        progressFill.style.width = `${percent}%`;
    }
};

window.onTtsFinished = function() {
    console.log("TTS Finished");
    stopAudioProfessional();
};

function resetPlayerForNewPoi() {
    isAudioSpeaking = false;
    isAudioPaused = false;
    resetAudioTimer();
    const audioSection = document.getElementById('sheet-audio-section');
    if (audioSection) {
        audioSection.classList.add('hidden');
        audioSection.classList.remove('audio-playing');
    }
    const progressFill = document.getElementById('audio-progress-fill');
    if (progressFill) progressFill.style.width = '0%';
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
            navigatingPoiId = currentBasePoiId; // TRACK the navigation target
            startNavigation(userCoords.lat, userCoords.lng, currentDestCoords[0], currentDestCoords[1]);
            closeDetails();
        } else {
            alert("Đang xác định vị trí của bạn...");
        }
    };
}

async function startNavigation(slat, slon, dlat, dlon) {
    if (routingLayer) map.removeLayer(routingLayer);

    // Add alternatives=true to get multiple routes and then pick the shortest distance
    const url = `https://router.project-osrm.org/route/v1/driving/${slon},${slat};${dlon},${dlat}?overview=full&geometries=geojson&alternatives=true`;

    try {
        const res = await fetch(url);
        const data = await res.json();

        if (data.code === 'Ok') {
            // Sort routes by distance (ascending) to get the shortest route
            data.routes.sort((a, b) => a.distance - b.distance);
            const route = data.routes[0];
            const distance = (route.distance / 1000).toFixed(1); // km

            const mainRouteLayer = L.geoJSON(route.geometry, {
                style: {
                    color: '#FB6F92',
                    weight: 6,
                    opacity: 0.8,
                    lineJoin: 'round'
                }
            });

            const lastPoint = route.geometry.coordinates[route.geometry.coordinates.length - 1];
            const walkingLine = L.polyline([
                [lastPoint[1], lastPoint[0]],
                [dlat, dlon]
            ], {
                color: '#FB6F92',
                weight: 4,
                opacity: 0.6,
                dashArray: '5, 10'
            });

            routingLayer = L.featureGroup([mainRouteLayer, walkingLine]).addTo(map);
            map.fitBounds(routingLayer.getBounds(), { padding: [50, 50] });

            document.getElementById('nav-distance').innerText = `${distance} km`;
            document.getElementById('nav-overlay').classList.remove('hidden');
            navigationActive = true;
        }
    } catch (e) {
        console.error("Routing error", e);
        alert("Không thể tìm đường đi lúc này.");
    }
}

function cancelNavigation() {
    navigatingPoiId = null; // CLEAR navigation target
    if (routingLayer) {
        map.removeLayer(routingLayer);
        routingLayer = null;
    }
    document.getElementById('nav-overlay').classList.add('hidden');
    navigationActive = false;
    
    // Force refresh colors to reset the green marker
    if (userMarker) {
        processLocation({ coords: { latitude: userMarker.getLatLng().lat, longitude: userMarker.getLatLng().lng } });
    }
}
