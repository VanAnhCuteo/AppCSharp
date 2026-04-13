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

async function openLanguagePicker() {
    const overlay = document.getElementById('lang-picker-overlay');
    const list = document.getElementById('lang-picker-list');
    
    if (!currentBasePoiId) return;

    overlay.classList.remove('hidden');
    list.innerHTML = '<div style="padding: 20px; text-align: center; color: #888;">Đang tải ngôn ngữ...</div>';

    try {
        const res = await fetch(`${platformApiBase}/${currentBasePoiId}/available-languages`);
        if (res.ok) {
            const languages = await res.json();
            list.innerHTML = '';
            
            languages.forEach(lang => {
                const isSelected = lang.language_code === selectedLanguage;
                const item = document.createElement('div');
                item.className = `lang-item ${isSelected ? 'selected' : ''}`;
                
                // Prefix with server URL
                const serverBase = platformApiBase.split('/api')[0];
                const flagPath = `${serverBase}/images/${lang.flag_url || 'vn_flag.png'}`;

                item.innerHTML = `
                    <span class="lang-item-name">${lang.name}</span>
                    ${isSelected ? '<div class="lang-item-check"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg></div>' : ''}
                `;
                
                item.onclick = () => setSelectedLang(lang.language_code, lang.name, lang.flag_url);
                list.appendChild(item);
            });
        }
    } catch (e) {
        list.innerHTML = '<div style="padding: 20px; color: #ff4d4f;">Không thể tải danh sách ngôn ngữ</div>';
    }
}

function closeLanguagePicker() {
    document.getElementById('lang-picker-overlay').classList.add('hidden');
}

async function setSelectedLang(lang, name, flag) {
    console.log(`DEBUG: Language switch requested to: ${lang} (${name})`);
    closeLanguagePicker();
    
    if (lang === selectedLanguage) return;

    let confirmed = true;
    if (isAudioSpeaking || isAudioPaused) {
        confirmed = confirm("Bạn muốn chuyển ngôn ngữ?");
        if (!confirmed) return;
        
        isAudioSpeaking = false;
        isAudioPaused = false;
        const speakerBtn = document.getElementById('sheet-speaker-btn');
        if (speakerBtn) speakerBtn.classList.remove('playing');
    }
    
    selectedLanguage = lang;
    playedAudioPois.clear();

    // Update the button UI
    if (name) document.getElementById('current-lang-name').innerText = name;

    // Request C# to reload markers and STOP current speech
    window.location.href = `app-request-reload://markers?lang=${lang}`;

    if (currentBasePoiId) {
        await openDetails(currentBasePoiId, lang);
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


    // Update UI button to current language if we opened from a fresh state
    try {
        const langRes = await fetch(`${platformApiBase}/${poiId}/available-languages`);
        if (langRes.ok) {
            const languages = await langRes.json();
            const current = languages.find(l => l.language_code === lang) || languages.find(l => l.language_code === 'vi') || languages[0];
            if (current) {
                document.getElementById('current-lang-name').innerText = current.name;
                selectedLanguage = current.language_code;
            }
        }
    } catch(e) {}

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
        if (!stillInRange) {
            if (poiAudioTimers[food.id]) {
                console.log(`DEBUG: User left range of ${food.name}, cancelling countdown`);
                clearTimeout(poiAudioTimers[food.id]);
                delete poiAudioTimers[food.id];
            }

            // MỚI: Reset trạng thái "đã phát" khi người dùng đi xa khỏi quán (+ 30m buffer)
            const exitBuffer = 2; // Khoảng cách đệm để tránh jitter (2m)
            const foodDistanceData = allDistances.find(f => f.id === food.id);
            if (foodDistanceData) {
                const range = (food.range_meters || 50);
                if (foodDistanceData.distance > (range + exitBuffer) && playedAudioPois.has(food.id)) {
                    console.log(`DEBUG: User far enough from ${food.name} (${Math.round(foodDistanceData.distance)}m), resetting play status`);
                    playedAudioPois.delete(food.id);
                }
            }
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
    playCurrentAudio(true); // MANUAL trigger
}

// These are now handled by C# native player
function resumeAudioProfessional() {}
function stopAudioProfessional() {}
function pauseAudioProfessional() {}
function restartAudio() {}

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
        
        window.location.href = `app-tts://speak?id=${poiId}&text=${encodeURIComponent(text)}&lang=${language}&manual=${isManual}`;
    }
}

// Callbacks from C#
window.onTtsProgress = function(index, total) {
    // Progress is now shown on the C# manualMiniPlayer
};

window.onTtsFinished = function() {
    console.log("TTS Finished");
    isAudioSpeaking = false;
    isAudioPaused = false;
};

function resetPlayerForNewPoi() {
    // Mini-player state is now handled natively in C#, no HTML cleanup needed here.
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
