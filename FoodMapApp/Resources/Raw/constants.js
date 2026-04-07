// Global State
let allFoodsData = [];
const visitedFoods = new Set();
let currentUserId = 0;
let currentPoiId = null;
let userMarker = null; // To store the user's location marker
let mapMarkers = [];
let markersGroup = null;
// Using Local Wi-Fi IP to support Physical Devices
// IP settings: These will be overridden by C# dynamically
let platformApiBase = ""; 

// Icons & SVGs
function getMarkerSvg(mainColor, gradId) {
    const secondaryColor = mainColor === '#FB6F92' ? '#FFBA08' : (mainColor === '#3A86FF' ? '#00D2FF' : '#2ecc71');
    return `
    <svg width="46" height="52" viewBox="0 0 46 52" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M23 2C11.954 2 3 10.954 3 22C3 34.5 23 50 23 50C23 50 43 34.5 43 22C43 10.954 34.046 2 23 2Z" fill="url(#${gradId})"/>
        <path d="M23 2C11.954 2 3 10.954 3 22C3 34.5 23 50 23 50C23 50 43 34.5 43 22C43 10.954 34.046 2 23 2Z" fill="${mainColor}" fill-opacity="0.9"/>
        <path d="M23 2C11.954 2 3 10.954 3 22C3 22.463 3.033 22.915 3.09 23.356C4.414 13.064 12.876 5 23 5C33.124 5 41.586 13.064 42.91 23.356C42.967 22.915 43 22.463 43 22C43 10.954 34.046 2 23 2Z" fill="white" fill-opacity="0.4"/>
        <circle cx="23" cy="22" r="9" fill="white"/>
        <defs>
            <linearGradient id="${gradId}" x1="23" y1="2" x2="23" y2="50" gradientUnits="userSpaceOnUse">
                <stop stop-color="${secondaryColor}"/>
                <stop offset="1" stop-color="${mainColor}"/>
            </linearGradient>
        </defs>
    </svg>`;
}

const pinkIcon = L.divIcon({
    className: 'custom-pink-marker',
    html: '<div class="marker-inner"><div class="marker-pulse"></div>' + getMarkerSvg('#FB6F92', 'grad_pink') + '</div>',
    iconSize: [46, 52],
    iconAnchor: [23, 50],
    popupAnchor: [0, -46]
});

const blueIcon = L.divIcon({
    className: 'custom-blue-marker',
    html: '<div class="marker-inner">' + getMarkerSvg('#3A86FF', 'grad_blue') + '</div>',
    iconSize: [46, 52],
    iconAnchor: [23, 50],
    popupAnchor: [0, -46]
});

const greenIcon = L.divIcon({
    className: 'custom-green-marker',
    html: '<div class="marker-inner">' + getMarkerSvg('#27ae60', 'grad_green') + '</div>',
    iconSize: [46, 52],
    iconAnchor: [23, 50],
    popupAnchor: [0, -46]
});

const blueLocationIcon = L.divIcon({
    className: 'user-location-marker',
    html: '<div class="user-pulse"></div><div class="user-dot"></div>',
    iconSize: [24, 24],
    iconAnchor: [12, 12]
});

const iconLocation = `<svg class="info-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"></path></svg>`;
const iconClock = `<svg class="info-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>`;
const iconCamera = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"></path><circle cx="12" cy="13" r="4"></circle></svg>`;

function handleImageError(el) {
    console.error('Failed to load image:', el.src);
    el.onerror = null;
    
    if (el.classList.contains('resto-img')) {
        const wrapper = el.parentElement;
        if (wrapper) {
            wrapper.innerHTML = `<div class="resto-placeholder">${iconCamera}</div>`;
        }
    } else {
        // For array sheets or multi-galleries, just hide the broken image
        // instead of nuking the entire parent container which holds other valid images
        el.style.display = 'none';
    }
}

function parseFirstImage(raw) {
    if (!raw || typeof raw !== 'string') return "";
    raw = raw.trim();
    if (raw.startsWith('[')) {
        try {
            const arr = JSON.parse(raw);
            if (arr && arr.length > 0) return arr[0];
        } catch(e) {}
    }
    const parts = raw.split(/[,;]/);
    if (parts.length > 0) {
        return parts[0].replace(/['"\[\]]+/g, '').trim();
    }
    return raw;
}

function parseAllImages(raw) {
    if (!raw || typeof raw !== 'string') return [];
    raw = raw.trim();
    if (raw.startsWith('[')) {
        try {
            const arr = JSON.parse(raw);
            if (Array.isArray(arr)) return arr.filter(url => url && url.toString().trim() !== '');
        } catch(e) {}
    }
    const parts = raw.split(/[,;]/);
    if (parts.length > 0) {
        return parts.map(p => p.replace(/['"\[\]]+/g, '').trim()).filter(url => url !== '');
    }
    return [];
}
