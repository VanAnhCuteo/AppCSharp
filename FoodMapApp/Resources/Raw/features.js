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
    console.log(`DEBUG: Language switch requested to: ${lang}`);
    
    // If audio is currently playing, ask for confirmation
    if (isAudioSpeaking || isAudioPaused) {
        // Pause audio first to be polite during the dialog
        const originalStatus = isAudioPaused;
        if (!isAudioPaused) {
            window.location.href = `app-tts://stop?id=${currentPoiId}&reset=false`;
            stopAudioTimer();
            isAudioPaused = true;
        }

        const confirmSwitch = confirm("Bạn muốn chuyển ngôn ngữ?");
        
        if (!confirmSwitch) {
            console.log("DEBUG: Language switch cancelled by user.");
            // Resume if it was playing before
            if (!originalStatus) {
                resumeAudioProfessional();
            }
            return;
        }
        
        // If confirmed, stop completely and reset timer for new language
        stopAudioProfessional();
    }
    
    selectedLanguage = lang;
    document.querySelectorAll('.lang-btn').forEach(btn => btn.classList.remove('active'));
    el.classList.add('active');

    // Request C# to reload markers for this language
    window.location.href = `app-request-reload://markers?lang=${lang}`;

    // Reload current sheet details if open
    if (currentBasePoiId) {
        await openDetails(currentBasePoiId, lang);
        // Automatically start playing in new language if we confirmed the switch
        if (confirmSwitch) {
            setTimeout(() => resumeAudioProfessional(), 500); 
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

        const guideRes = await fetch(`${platformApiBase}/${poiId}/guide?lang=${lang}`);
        if (guideRes.ok) {
            currentAudioGuide = await guideRes.json();
            // Show speaker but update its state
            const speakerBtn = document.getElementById('sheet-speaker-btn');
            if (speakerBtn) {
                speakerBtn.classList.remove('hidden');
                if (isAudioSpeaking && !isAudioPaused) speakerBtn.classList.add('playing');
                else speakerBtn.classList.remove('playing');
            }
        } else {
            currentAudioGuide = null;
            // Fallback: If POI has description, still show speaker button
            const speakerBtn = document.getElementById('sheet-speaker-btn');
            if (speakerBtn) {
                if (currentPoiDescription && currentPoiDescription.trim().length > 0) {
                    speakerBtn.classList.remove('hidden');
                } else {
                    speakerBtn.classList.add('hidden');
                }
            }
        }
    } catch (e) { console.error("Error fetching details", e); }
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

    // 2. Lọc danh sách QUÁN TRONG PHẠM VI RIÊNG
    let poisInRange = allDistances.filter(f => f.distance <= (f.range_meters || 50));
    
    // Sắp xếp quán trong phạm vi theo khoảng cách (Gần nhất lên đầu)
    poisInRange.sort((a, b) => a.distance - b.distance);

    if (poisInRange.length > 0) {
        // TÌM QUÁN CHƯA NGHE GẦN NHẤT ĐỂ KÍCH HOẠT (Tránh trigger đồng thời gây nhiễu hàng đợi)
        const nextTarget = poisInRange.find(food => !playedAudioPois.has(food.id) && !poiAudioTimers[food.id]);
        
        if (nextTarget) {
            console.log(`DEBUG: Priority target found in range: ${nextTarget.name}`);
            poiAudioTimers[nextTarget.id] = setTimeout(() => {
                if (!playedAudioPois.has(nextTarget.id)) {
                    console.log(`DEBUG: Triggering auto-audio (Priority): ${nextTarget.name}`);
                    playedAudioPois.add(nextTarget.id);
                    triggerAutoAudio(nextTarget.id);
                }
                delete poiAudioTimers[nextTarget.id];
            }, 5000);
        }
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

        // Nếu người dùng đi ra khỏi phạm vi quán RIÊNG trước khi hết 5 giây -> Hủy bộ đếm
        const stillInRange = poisInRange.some(p => p.id === food.id);
        if (!stillInRange && poiAudioTimers[food.id]) {
            console.log(`DEBUG: User left range of ${food.name}, cancelling countdown`);
            clearTimeout(poiAudioTimers[food.id]);
            delete poiAudioTimers[food.id];
        }
    });
}

async function triggerAutoAudio(poiId) {
    try {
        const guideRes = await fetch(`${platformApiBase}/${poiId}/guide?lang=${selectedLanguage}`);
        if (guideRes.ok) {
            const guide = await guideRes.json();
            // AUTO requests do NOT have manual=true
            playAudioGuide(`${guide.title}. ${guide.description || ''}`, guide.language || selectedLanguage, poiId, false);
        } else {
            const detailsRes = await fetch(`${platformApiBase}/${poiId}?lang=${selectedLanguage}`);
            if (detailsRes.ok) {
                const data = await detailsRes.json();
                if (data.description) playAudioGuide(`${data.name}. ${data.description}`, selectedLanguage, poiId, false);
            }
        }
    } catch (e) { console.error("Auto-audio error", e); }
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
    
    // UI Updates
    const speakerBtn = document.getElementById('sheet-speaker-btn');
    if (speakerBtn) speakerBtn.classList.add('playing');
    
    const statusText = document.getElementById('audio-status-text');
    if (statusText) statusText.innerText = "Đang phát...";

    startAudioTimer();
    playCurrentAudio(true); // MANUAL trigger
}

function pauseAudioProfessional() {
    isAudioPaused = true;
    
    const speakerBtn = document.getElementById('sheet-speaker-btn');
    if (speakerBtn) speakerBtn.classList.remove('playing');
    
    const statusText = document.getElementById('audio-status-text');
    if (statusText) statusText.innerText = "Đã tạm dừng";

    stopAudioTimer();
    // Pause doesn't need to specify manual as it doesn't enqueue
    window.location.href = `app-tts://stop?id=${currentPoiId}&reset=false`;
}

function stopAudioProfessional() {
    const wasActive = isAudioSpeaking || isAudioPaused;
    isAudioSpeaking = false;
    isAudioPaused = false;
    
    const speakerBtn = document.getElementById('sheet-speaker-btn');
    if (speakerBtn) speakerBtn.classList.remove('playing');
    
    const statusText = document.getElementById('audio-status-text');
    if (statusText) statusText.innerText = "Bản dịch Audio";
    
    resetAudioTimer();
    const progressFill = document.getElementById('audio-progress-fill');
    if (progressFill) progressFill.style.width = '0%';

    if (wasActive) {
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
    
    // Start fresh - MANUAL
    setTimeout(() => {
        resumeAudioProfessional();
    }, 100);
}

function playCurrentAudio(isManual = true) {
    let text = "";
    if (currentAudioGuide) {
        text = `${currentAudioGuide.title}. ${currentAudioGuide.description || ''}`;
    } else if (currentPoiDescription) {
        const title = document.getElementById('sheet-title').innerText;
        text = `${title}. ${currentPoiDescription}`;
    }

    if (text) {
        window.location.href = `app-tts://speak?id=${currentPoiId}&text=${encodeURIComponent(text)}&lang=${selectedLanguage}&manual=${isManual}`;
    }
}

function playAudioGuide(text, language = 'vi-VN', poiId = currentPoiId, isManual = true) {
    if (text) {
        isAudioSpeaking = true;
        isAudioPaused = false;
        
        // Sync speaker btn if available
        const speakerBtn = document.getElementById('sheet-speaker-btn');
        if (speakerBtn) speakerBtn.classList.add('playing');

        startAudioTimer();
        window.location.href = `app-tts://speak?id=${poiId}&text=${encodeURIComponent(text)}&lang=${language}&manual=${isManual}`;
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
    // If NOT playing, reset everything. 
    // If ALREADY playing, we keep the mini-player visible but the new sheet will sync in openDetails
    if (!isAudioSpeaking && !isAudioPaused) {
        resetAudioTimer();
        const statusBar = document.getElementById('audio-status-bar');
        if (statusBar) statusBar.classList.add('hidden');
        const progressFill = document.getElementById('audio-progress-fill');
        if (progressFill) progressFill.style.width = '0%';
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
