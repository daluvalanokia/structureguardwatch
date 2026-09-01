// structurewatch.js — Main frontend using LeafletThreeLayer for 3D sync
// Uses: leaflet-three-layer.js (the 3D sync module), collision.js (drag-arrow AABB)

// ═══════════════════════════════════════════════════════════════
// 1. MAP INITIALIZATION
// ═══════════════════════════════════════════════════════════════

const DEFAULT_CENTER = [40.7589, -73.9851]; // Manhattan
const DEFAULT_ZOOM = 15;

const map = L.map('map', { zoomControl: true, preferCanvas: true }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors',
    maxZoom: 19,
}).addTo(map);

// ═══════════════════════════════════════════════════════════════
// 2. 3D LAYER (LeafletThreeLayer — synced on every pan/zoom)
// ═══════════════════════════════════════════════════════════════

const threeLayer = L.leafletThreeLayer(map, { heightScale: 0.8 });
threeLayer.addTo(map);

threeLayer.on('buildingHover', (e) => {
    if (e.footprint) {
        showTooltip(e.containerPoint, e.footprint);
    } else {
        hideTooltip();
    }
});

threeLayer.on('buildingClick', async (e) => {
    if (!e.footprint) {
        document.getElementById('inspector').classList.add('hidden');
        return;
    }
    await showInspector(e.footprint);
});

// ═══════════════════════════════════════════════════════════════
// 3. INCREMENTAL ADDRESS SEARCH (Nominatim autocomplete)
// ═══════════════════════════════════════════════════════════════

let searchTimer = null;
let currentLocation = null;

const searchInput = document.getElementById('searchBox');
const searchDropdown = document.getElementById('searchDropdown');
const searchSpinner = document.getElementById('searchSpinner');
const searchClear = document.getElementById('searchClear');

searchInput.addEventListener('input', (e) => {
    const query = e.target.value.trim();
    clearTimeout(searchTimer);

    if (query.length < 3) {
        searchDropdown.classList.add('hidden');
        searchClear.classList.add('hidden');
        return;
    }

    searchClear.classList.remove('hidden');
    searchSpinner.classList.remove('hidden');

    searchTimer = setTimeout(async () => {
        try {
            const resp = await fetch(`/api/search?q=${encodeURIComponent(query)}`);
            const results = await resp.json();
            renderSearchResults(results);
        } catch (err) {
            console.error('Address search failed:', err);
            searchDropdown.innerHTML = '<div class="p-3 text-sm text-red-400">Search failed</div>';
            searchDropdown.classList.remove('hidden');
        }
        searchSpinner.classList.add('hidden');
    }, 300);
});

function renderSearchResults(results) {
    if (!results || results.length === 0) {
        searchDropdown.innerHTML = '<div class="p-3 text-sm text-gray-500">No results found</div>';
        searchDropdown.classList.remove('hidden');
        return;
    }

    searchDropdown.innerHTML = results.map((r, idx) => `
        <div class="search-result p-3 hover:bg-teal-900/40 cursor-pointer border-b border-gray-700 last:border-0" data-idx="${idx}">
            <div class="text-sm font-medium text-gray-200">${r.displayName.split(',').slice(0, 3).join(',')}</div>
            <div class="text-xs text-gray-500">${r.label}</div>
        </div>
    `).join('');
    searchDropdown.classList.remove('hidden');

    searchDropdown.querySelectorAll('.search-result').forEach(el => {
        el.addEventListener('click', () => {
            selectSearchResult(results[parseInt(el.dataset.idx)]);
        });
    });
}

function selectSearchResult(result) {
    const lat = result.lat;
    const lng = result.lon;
    const name = result.label;

    currentLocation = { lat, lng, name };
    searchInput.value = name;
    searchDropdown.classList.add('hidden');

    // Fly to location
    map.flyTo([lat, lng], 16, { duration: 1.5 });

    // After flight: scan animation + fetch
    map.once('moveend', () => {
        triggerScan(name);
    });
}

searchInput.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        searchInput.value = '';
        searchDropdown.classList.add('hidden');
        currentLocation = null;
    }
});

searchClear.addEventListener('click', () => {
    searchInput.value = '';
    searchDropdown.classList.add('hidden');
    searchClear.classList.add('hidden');
    currentLocation = null;
});

document.addEventListener('click', (e) => {
    if (!e.target.closest('#searchBox') && !e.target.closest('#searchDropdown')) {
        searchDropdown.classList.add('hidden');
    }
});

// ═══════════════════════════════════════════════════════════════
// 4. SCAN ANIMATION
// ═══════════════════════════════════════════════════════════════

function triggerScan(locationName) {
    const scanOverlay = document.getElementById('scanOverlay');
    const scanLabel = document.getElementById('scanLabel');

    scanLabel.textContent = `Scanning ${locationName}...`;
    scanOverlay.classList.remove('hidden');

    setTimeout(async () => {
        await fetchFootprints();
        scanLabel.textContent = `Found ${threeLayer.getBuildings().length} buildings in ${locationName}`;
        updateStatusBar();

        setTimeout(() => scanOverlay.classList.add('hidden'), 1000);
    }, 1500);
}

// ═══════════════════════════════════════════════════════════════
// 5. FOOTPRINT FETCH
// ═══════════════════════════════════════════════════════════════

async function fetchFootprints() {
    const b = map.getBounds();
    const bbox = `${b.getSouth().toFixed(6)},${b.getWest().toFixed(6)},${b.getNorth().toFixed(6)},${b.getEast().toFixed(6)}`;
    showLoading(true);
    try {
        const resp = await fetch(`/api/footprints?bbox=${bbox}`);
        const data = await resp.json();
        threeLayer.setBuildings(data);
        updateStatusBar();
    } catch (e) {
        console.error('Footprint fetch failed:', e);
        showToast('Failed to fetch building footprints');
    }
    showLoading(false);
}

// ═══════════════════════════════════════════════════════════════
// 6. INSPECTOR + ANALYZE
// ═══════════════════════════════════════════════════════════════

async function showInspector(fp) {
    const panel = document.getElementById('inspector');
    panel.innerHTML = `
        <h2 class="text-xl font-bold mb-3">${fp.name || 'Unknown Building'}</h2>
        <div class="space-y-1 mb-4 text-sm">
            <div><span class="opacity-60">OSM ID:</span> ${fp.osmId}</div>
            <div><span class="opacity-60">Type:</span> ${fp.buildingType}</div>
            <div><span class="opacity-60">Levels:</span> ${fp.tags['building:levels'] || 'N/A'}</div>
            <div><span class="opacity-60">Height:</span> ${fp.tags['height'] || fp.calculatedHeight?.toFixed(1) + 'm (calc)'}</div>
            <div><span class="opacity-60">Address:</span> ${fp.tags['addr:housenumber'] || ''} ${fp.tags['addr:street'] || 'N/A'}</div>
        </div>
        <h3 class="font-semibold mb-2">OSM Tags</h3>
        <div class="bg-gray-800 rounded p-3 text-xs max-h-48 overflow-auto">
            ${Object.entries(fp.tags).map(([k, v]) => `<div><span class="text-teal-400">${k}</span>: ${v}</div>`).join('')}
        </div>
        <button id="analyzeBtn" class="mt-4 w-full bg-teal-600 hover:bg-teal-500 text-white py-2 rounded font-semibold">Analyze Structure</button>
        <div id="analysisResults" class="mt-4"></div>
    `;
    panel.classList.remove('hidden');
    document.getElementById('analyzeBtn').addEventListener('click', () => analyze(fp));
}

async function analyze(fp) {
    const btn = document.getElementById('analyzeBtn');
    btn.textContent = 'Analyzing...';
    btn.disabled = true;
    try {
        const resp = await fetch('/api/analyze', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ osmId: fp.osmId, tags: fp.tags })
        });
        const result = await resp.json();
        document.getElementById('analysisResults').innerHTML = `
            <h3 class="font-semibold mb-2 text-teal-400">Physical Analysis</h3>
            <div class="space-y-2 text-sm">
                <div><span class="opacity-60">Load Capacity:</span> ${result.loadCapacity}</div>
                <div><span class="opacity-60">Structural Integrity:</span> ${result.structuralIntegrity}</div>
                <div><span class="opacity-60">Seismic Risk:</span> ${result.seismicRisk}</div>
                <div><span class="opacity-60">Wind Load:</span> ${result.windLoad}</div>
                <div><span class="opacity-60">Occupancy Class:</span> ${result.occupancyClass}</div>
                <div class="mt-2"><span class="opacity-60">Summary:</span><br>${result.summary}</div>
                <div class="mt-2">
                    <span class="opacity-60">Risk Factors:</span>
                    <ul class="list-disc list-inside text-red-400">
                        ${(result.riskFactors || []).map(r => `<li>${r}</li>`).join('')}
                    </ul>
                </div>
            </div>
        `;
    } catch (e) {
        showToast('Analysis failed: ' + e.message);
    }
    btn.textContent = 'Re-Analyze';
    btn.disabled = false;
}

// ═══════════════════════════════════════════════════════════════
// 7. UTILITY FUNCTIONS
// ═══════════════════════════════════════════════════════════════

function showTooltip(pt, fp) {
    const tooltip = document.getElementById('tooltip');
    tooltip.innerHTML = `
        <div class="font-bold">${fp.name || 'Unknown'}</div>
        <div class="text-sm opacity-80">${fp.tags['addr:housenumber'] || ''} ${fp.tags['addr:street'] || 'N/A'}</div>
        <div class="text-sm">Levels: ${fp.tags['building:levels'] || '?'} | Height: ${fp.tags['height'] || (fp.calculatedHeight?.toFixed(1) + 'm (calc)') || '?'}</div>
        <div class="text-sm">Type: ${fp.buildingType || 'yes'}</div>
    `;
    tooltip.style.left = `${pt.x + 10}px`;
    tooltip.style.top = `${pt.y + 10}px`;
    tooltip.style.display = 'block';
}

function hideTooltip() {
    document.getElementById('tooltip').style.display = 'none';
}

function showLoading(show) {
    document.getElementById('loading').classList.toggle('hidden', !show);
}

function showToast(msg) {
    const el = document.getElementById('toast');
    el.textContent = msg;
    el.classList.remove('hidden');
    setTimeout(() => el.classList.add('hidden'), 3000);
}

function updateCursorCoords(latlng) {
    document.getElementById('cursorCoords').textContent = `Lat: ${latlng.lat.toFixed(5)}, Lng: ${latlng.lng.toFixed(5)}`;
}

function updateStatusBar() {
    document.getElementById('buildingCount').textContent = `Buildings: ${threeLayer.getBuildings().length}`;
    document.getElementById('zoomLevel').textContent = `Zoom: ${map.getZoom()}`;
    document.getElementById('currentLocationLabel').textContent = currentLocation ? currentLocation.name : '';
}

// ═══════════════════════════════════════════════════════════════
// 8. EVENT WIRING + INIT
// ═══════════════════════════════════════════════════════════════

map.on('mousemove', (e) => updateCursorCoords(e.latlng));
map.on('moveend', fetchFootprints);
map.on('zoom', updateStatusBar);

fetchFootprints();
