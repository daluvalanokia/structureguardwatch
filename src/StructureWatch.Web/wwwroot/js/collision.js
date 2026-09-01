// collision.js — Drag-arrow AABB collision detection
// Depends on: leaflet-three-layer.js, structurewatch.js
// Uses the threeLayer's selected building + all loaded footprints

// ═══════════════════════════════════════════════════════════════
// STATE
// ═══════════════════════════════════════════════════════════════

let dragEnabled = false;
let isDragging = false;
let dragOrigin = null;
let ghostLayer = null;
let arrowLine = null;
let collisionTimer = null;

const collisionGroup = L.layerGroup().addTo(map);

// ═══════════════════════════════════════════════════════════════
// TOGGLE DRAG MODE
// ═══════════════════════════════════════════════════════════════

document.getElementById('dragBtn').addEventListener('click', () => {
    dragEnabled = !dragEnabled;
    const btn = document.getElementById('dragBtn');

    if (dragEnabled) {
        btn.textContent = '✋ Drag Mode: ON';
        btn.classList.remove('bg-gray-700');
        btn.classList.add('bg-teal-600', 'text-white');
        document.body.classList.add('drag-mode-active');

        const selected = threeLayer.getSelected();
        if (!selected) {
            document.getElementById('collisionPanel').innerHTML =
                '<p class="text-yellow-400 text-sm">⚠ Select a building first, then drag to check collisions.</p>';
        } else {
            document.getElementById('collisionPanel').innerHTML =
                '<p class="text-teal-400 text-sm">Drag from the selected building to test collision.</p>';
        }
    } else {
        btn.textContent = '✋ Drag Mode';
        btn.classList.remove('bg-teal-600', 'text-white');
        btn.classList.add('bg-gray-700');
        document.body.classList.remove('drag-mode-active');
        resetDrag();
    }
});

function resetDrag() {
    isDragging = false;
    dragOrigin = null;
    if (ghostLayer) { collisionGroup.removeLayer(ghostLayer); ghostLayer = null; }
    if (arrowLine) { collisionGroup.removeLayer(arrowLine); arrowLine = null; }
}

// ═══════════════════════════════════════════════════════════════
// DRAG HANDLERS
// ═══════════════════════════════════════════════════════════════

map.on('mousedown', (e) => {
    if (!dragEnabled || !threeLayer.getSelected()) return;
    isDragging = true;
    dragOrigin = e.latlng;
});

map.on('mousemove', (e) => {
    if (!dragEnabled || !isDragging || !threeLayer.getSelected() || !dragOrigin) return;

    const fp = threeLayer.getSelected();

    // Pixel delta
    const originPx = map.latLngToContainerPoint(dragOrigin);
    const cursorPx = map.latLngToContainerPoint(e.latlng);
    const dx = cursorPx.x - originPx.x;
    const dy = cursorPx.y - originPx.y;

    // Ghost box polygon
    if (ghostLayer) collisionGroup.removeLayer(ghostLayer);
    const movedLatLngs = fp.geometry.map(([lat, lng]) => {
        const pt = map.latLngToContainerPoint([lat, lng]);
        return map.containerPointToLatLng([pt.x + dx, pt.y + dy]);
    });
    ghostLayer = L.polygon(movedLatLngs, {
        color: '#f59e0b', weight: 2, fillOpacity: 0.15, dashArray: '6,4',
    }).addTo(collisionGroup);

    // Arrow line
    if (arrowLine) collisionGroup.removeLayer(arrowLine);
    arrowLine = L.polyline([dragOrigin, e.latlng], {
        color: '#f59e0b', weight: 3, opacity: 0.8, dashArray: '4,4',
    }).addTo(collisionGroup);

    // Debounced collision check
    clearTimeout(collisionTimer);
    collisionTimer = setTimeout(() => checkCollision(fp, dx, dy), 120);
});

map.on('mouseup', () => { isDragging = false; });

// ═══════════════════════════════════════════════════════════════
// COLLISION CHECK (server-side AABB)
// ═══════════════════════════════════════════════════════════════

async function checkCollision(fp, dxPx, dyPx) {
    const refLat = fp.geometry[0][0];
    const refLng = fp.geometry[0][1];
    const refPx = map.latLngToContainerPoint([refLat, refLng]);
    const movedLatLng = map.containerPointToLatLng([refPx.x + dxPx, refPx.y + dyPx]);

    const metersPerDegLat = 110540.0;
    const metersPerDegLng = 111320.0 * Math.cos(refLat * Math.PI / 180);

    const dragDx = (movedLatLng.lng - refLng) * metersPerDegLng;
    const dragDy = (refLat - movedLatLng.lat) * metersPerDegLat;

    const allBuildings = threeLayer.getBuildings().map(b => b.footprint);

    try {
        const resp = await fetch('/api/collisions', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                selected: fp,
                dragDx: dragDx,
                dragDy: dragDy,
                allBuildings: allBuildings,
            }),
        });
        const result = await resp.json();
        renderCollisionResults(result);
    } catch (err) {
        console.error('Collision check failed:', err);
        document.getElementById('collisionPanel').innerHTML =
            '<p class="text-red-400 text-sm">⚠ Collision check failed.</p>';
    }
}

// ═══════════════════════════════════════════════════════════════
// RENDER RESULTS
// ═══════════════════════════════════════════════════════════════

function renderCollisionResults(result) {
    const panel = document.getElementById('collisionPanel');

    if (result.clear) {
        if (ghostLayer) ghostLayer.setStyle({ color: '#22c55e' });
        if (arrowLine) arrowLine.setStyle({ color: '#22c55e' });
        panel.innerHTML = `
            <div class="flex items-center gap-2 mb-2">
                <span class="inline-block w-3 h-3 rounded-full bg-green-500"></span>
                <span class="text-green-400 font-semibold">No interference detected</span>
            </div>
            <p class="text-gray-400 text-xs">The building can be moved here without colliding.</p>
        `;
    } else {
        if (ghostLayer) ghostLayer.setStyle({ color: '#ef4444' });
        if (arrowLine) arrowLine.setStyle({ color: '#ef4444' });
        panel.innerHTML = `
            <div class="flex items-center gap-2 mb-2">
                <span class="inline-block w-3 h-3 rounded-full bg-red-500"></span>
                <span class="text-red-400 font-semibold">${result.interferences.length} interference(s)</span>
            </div>
            <div class="space-y-2">
                ${result.interferences.map(i => `
                    <div class="bg-red-900/30 border-l-2 border-red-500 pl-3 py-1 rounded-r">
                        <div class="text-sm font-medium text-red-300">${i.name || 'Unnamed'}</div>
                        <div class="text-xs text-gray-400">OSM: ${i.osmId} · Overlap: ${i.overlapAreaSqM.toFixed(1)} m²</div>
                    </div>
                `).join('')}
            </div>
        `;
    }
}
