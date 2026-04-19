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

// Global Translations Cache
window.uiTranslations = {
    explore: "Khám phá",
    directions: "Chỉ đường đến quán",
    search_ph: "Tìm quán ăn, địa chỉ...",
    cancel: "Hủy",
    navigating_to: "Đang đến:",
    select_lang: "Chọn ngôn ngữ",
    done: "Xong",
    loading_lang: "Đang tải ngôn ngữ...",
    hours_not_available: "Không có giờ mở cửa",
    unknown_loc: "Vị trí chưa xác định",
    no_addr: "Không có địa chỉ",
    locating: "Đang xác định vị trí của bạn, vui lòng đợi chốc lát..."
};

window.setUiTranslations = function(json) {
    if (typeof json === 'string') json = JSON.parse(json);
    window.uiTranslations = { ...window.uiTranslations, ...json };
    updateStaticUI();
};

function updateStaticUI() {
    const searchInput = document.getElementById('map-search-input');
    if (searchInput) searchInput.placeholder = window.uiTranslations.search_ph;

    const navLabel = document.querySelector('.nav-info');
    if (navLabel) {
        const dist = document.getElementById('nav-distance')?.innerText || "0.0 km";
        navLabel.innerHTML = `${window.uiTranslations.navigating_to} <span id="nav-distance">${dist}</span>`;
    }

    const navCancel = document.querySelector('.nav-cancel-btn');
    if (navCancel) navCancel.innerText = window.uiTranslations.cancel;

    const pickerTitle = document.querySelector('.lang-picker-header h3');
    if (pickerTitle) pickerTitle.innerText = window.uiTranslations.select_lang;

    const pickerClose = document.querySelector('.lang-picker-header button');
    if (pickerClose) pickerClose.innerText = window.uiTranslations.done;

    const directionsBtn = document.getElementById('get-directions-btn');
    if (directionsBtn) directionsBtn.innerText = window.uiTranslations.directions;
}

// Main Entry Point from C#
async function loadFoods(foods, userId = 0) {
    console.log("Loading foods into map:", foods, "User:", userId);
    const markers = [];
    allFoodsData = foods;
    currentUserId = userId;



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
                // Prefix with server URL
                const serverBase = platformApiBase.split('/api')[0];
                imgPath = `${serverBase}/${imgPath}`;
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

        const displayTime = food.open_time || window.uiTranslations.hours_not_available;
        const displayAddress = food.address || window.uiTranslations.unknown_loc;

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
                    <button class="resto-btn" onclick="openDetails(${food.id})">${window.uiTranslations.explore}</button>
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
    const clearBtn = document.getElementById('search-clear-btn');
    const resultsDropdown = document.getElementById('search-results');
    const tourBtn = document.getElementById('tour-btn');

    if (tourBtn) {
        tourBtn.onclick = () => {
            window.location.href = "app-tour://open-drawer";
        };
    }

    if (!searchInput || !resultsDropdown) return;

    let debounceTimer;

    searchInput.addEventListener('input', (e) => {
        const query = e.target.value.trim();
        
        // Show/Hide clear button
        if (query.length > 0) {
            clearBtn.classList.remove('hidden');
        } else {
            clearBtn.classList.add('hidden');
            resultsDropdown.classList.add('hidden');
            // Reset markers visibility when search is cleared
            mapMarkers.forEach(({ marker }) => {
                if (!markersGroup.hasLayer(marker)) markersGroup.addLayer(marker);
            });
            return;
        }

        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            performSmartSearch(query);
        }, 300);
    });

    // Clear search button logic
    if (clearBtn) {
        clearBtn.onclick = () => {
            searchInput.value = '';
            clearBtn.classList.add('hidden');
            resultsDropdown.classList.add('hidden');
            mapMarkers.forEach(({ marker }) => {
                if (!markersGroup.hasLayer(marker)) markersGroup.addLayer(marker);
            });
            searchInput.focus();
        };
    }

    // Close dropdown when clicking outside
    document.addEventListener('click', (e) => {
        if (!searchInput.contains(e.target) && !resultsDropdown.contains(e.target)) {
            resultsDropdown.classList.add('hidden');
        }
    });

    function performSmartSearch(query) {
        const normalizedQuery = removeDiacritics(query.toLowerCase());
        const searchTerms = normalizedQuery.split(/\s+/).filter(t => t.length > 0);
        
        const matchedFoods = [];

        mapMarkers.forEach(({ marker, food }) => {
            const foodName = removeDiacritics(food.name.toLowerCase());
            const foodAddr = removeDiacritics((food.address || "").toLowerCase());
            const foodDesc = removeDiacritics((food.description || "").toLowerCase());
            
            // Smart Match: EVERY search term must be found in either name, address or description
            const isMatch = searchTerms.every(term => 
                foodName.includes(term) || foodAddr.includes(term) || foodDesc.includes(term)
            );

            if (isMatch) {
                if (!markersGroup.hasLayer(marker)) markersGroup.addLayer(marker);
                matchedFoods.push(food);
            } else {
                if (markersGroup.hasLayer(marker)) markersGroup.removeLayer(marker);
            }
        });

        renderSearchResults(matchedFoods);
    }

    function renderSearchResults(foods) {
        resultsDropdown.innerHTML = '';
        if (foods.length === 0) {
            resultsDropdown.innerHTML = `<div style="padding: 15px; text-align: center; color: #999; font-size: 14px;">${window.uiTranslations.loading_lang.includes('...') ? '...' : ''}</div>`;
        } else {
            // Limit to top 10 results to keep UI clean
            foods.slice(0, 10).forEach(food => {
                const item = document.createElement('div');
                item.className = 'search-result-item';
                item.innerHTML = `
                    <div class="res-icon">
                        <svg width="20" height="20" fill="currentColor" viewBox="0 0 20 20"><path d="M5 3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V5a2 2 0 00-2-2H5zM5 11a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2v-2a2 2 0 00-2-2H5zM11 5a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V5zM11 13a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z"></path></svg>
                    </div>
                    <div class="res-details">
                        <span class="res-name">${food.name}</span>
                        <span class="res-addr">${food.address || window.uiTranslations.no_addr}</span>
                    </div>
                `;
                item.onclick = () => {
                    resultsDropdown.classList.add('hidden');
                    // Find the existing marker for this food
                    const markerData = mapMarkers.find(mm => mm.food.id === food.id);
                    if (markerData) {
                        map.flyTo(markerData.marker.getLatLng(), 18, { animate: true, duration: 1 });
                        // Delay openDetails slightly so the map movement looks smoother
                        setTimeout(() => openDetails(food.id), 300);
                    }
                };
                resultsDropdown.appendChild(item);
            });
        }
        resultsDropdown.classList.remove('hidden');
    }
}

function removeDiacritics(text) {
    if (!text) return "";
    let str = text;
    // Replace Vietnamese-specific characters first
    str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, "a");
    str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g, "e");
    str = str.replace(/ì|í|ị|ỉ|ĩ/g, "i");
    str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, "o");
    str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, "u");
    str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, "y");
    str = str.replace(/đ/g, "d");
    str = str.replace(/À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ/g, "A");
    str = str.replace(/È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ/g, "E");
    str = str.replace(/Ì|Í|Ị|Ỉ|Ĩ/g, "I");
    str = str.replace(/Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ/g, "O");
    str = str.replace(/Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ/g, "U");
    str = str.replace(/Ỳ|Ý|Ỵ|Ỷ|Ỹ/g, "Y");
    str = str.replace(/Đ/g, "D");
    // Finally use NFD normalization for any remaining diacritics
    return str.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase();
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
                alert(window.uiTranslations.locating);
            }
        }
    } catch(e) { console.error("Error fetching POI for routing", e); }
};
