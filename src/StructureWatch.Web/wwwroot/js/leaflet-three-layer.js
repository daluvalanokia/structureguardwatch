// leaflet-three-layer.js — Custom Leaflet layer that syncs Three.js 3D extruded buildings to the map
// This is the CORE module that ensures 3D buildings stay aligned with Leaflet tiles at all zoom/pan states.
//
// Usage:
//   const layer = new LeafletThreeLayer(map, { heightScale: 0.8 });
//   layer.setBuildings(footprints);
//   layer.on('buildingHover', (fp) => { ... });
//   layer.on('buildingClick', (fp) => { ... });

// ═══════════════════════════════════════════════════════════════
// LeafletThreeLayer — extends L.Layer
// ═══════════════════════════════════════════════════════════════

const LeafletThreeLayer = L.Layer.extend({

    initialize: function (map, options) {
        L.setOptions(this, options || {});
        this._map = map;
        this._buildings = [];       // [{ footprint, mesh, edges, colorHex }]
        this._selectedMesh = null;
        this._hoveredMesh = null;

        // Three.js setup
        this._scene = new THREE.Scene();
        this._camera = new THREE.PerspectiveCamera(45, 1, 1, 100000);
        this._renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true });
        this._renderer.domElement.style.position = 'absolute';
        this._renderer.domElement.style.pointerEvents = 'none';
        this._renderer.domElement.style.zIndex = '400';

        // Lighting for depth
        this._scene.add(new THREE.AmbientLight(0x666688, 0.6));
        const dirLight = new THREE.DirectionalLight(0xffffff, 0.8);
        dirLight.position.set(100, 200, 150);
        this._scene.add(dirLight);

        // Raycaster for hover/click
        this._raycaster = new THREE.Raycaster();
        this._pointer = new THREE.Vector2();

        this._heightScale = options.heightScale || 0.8;

        // Bind handlers
        this._onMapMove = this._sync.bind(this);
        this._onMapZoomStart = this._onZoomStart.bind(this);
        this._onMapZoomEnd = this._onZoomEnd.bind(this);
        this._onMouseMove = this._onMousemove.bind(this);
        this._onClick = this._onClick.bind(this);
    },

    // ─── Lifecycle ──────────────────────────────────────────
    onAdd: function (map) {
        map.getPanes().overlayPane.appendChild(this._renderer.domElement);
        map.on('move', this._onMapMove);
        map.on('zoomstart', this._onMapZoomStart);
        map.on('zoomend', this._onMapZoomEnd);
        map.on('resize', this._onMapMove);
        map.on('mousemove', this._onMouseMove);
        map.on('click', this._onClick);
        this._sync();
        return this;
    },

    onRemove: function (map) {
        map.getPanes().overlayPane.removeChild(this._renderer.domElement);
        map.off('move', this._onMapMove);
        map.off('zoomstart', this._onMapZoomStart);
        map.off('zoomend', this._onMapZoomEnd);
        map.off('resize', this._onMapMove);
        map.off('mousemove', this._onMouseMove);
        map.off('click', this._onClick);
        this._clearBuildings();
        return this;
    },

    // ─── Building management ────────────────────────────────
    setBuildings: function (footprints) {
        this._clearBuildings();
        footprints.forEach(fp => this._addBuilding(fp));
        this._sync();
    },

    _addBuilding: function (fp) {
        const colorHex = this._colorByType(fp.buildingType);
        const material = new THREE.MeshLambertMaterial({
            color: colorHex, transparent: true, opacity: 0.75, wireframe: false
        });
        const edgeMat = new THREE.LineBasicMaterial({
            color: 0xffffff, opacity: 0.4, transparent: true
        });

        // Placeholder geometry — rebuilt in _reproject
        const geo = new THREE.ExtrudeGeometry(new THREE.Shape(), { depth: 1, bevelEnabled: false });
        const mesh = new THREE.Mesh(geo, material);
        const edges = new THREE.LineSegments(new THREE.EdgesGeometry(geo), edgeMat);

        this._scene.add(mesh);
        this._scene.add(edges);
        this._buildings.push({ footprint: fp, mesh, edges, colorHex });
    },

    _clearBuildings: function () {
        this._buildings.forEach(b => {
            this._scene.remove(b.mesh);
            this._scene.remove(b.edges);
            b.mesh.geometry.dispose();
            b.mesh.material.dispose();
            b.edges.geometry.dispose();
            b.edges.material.dispose();
        });
        this._buildings = [];
        this._selectedMesh = null;
        this._hoveredMesh = null;
    },

    getBuildings: function () { return this._buildings; },

    // ─── Sync: reposition all meshes to screen coords ────────
    _sync: function () {
        const map = this._map;
        const size = map.getSize();
        const topLeft = map.containerPointToLayerPoint([0, 0]);

        this._renderer.setSize(size.x, size.y);
        this._renderer.domElement.style.left = topLeft.x + 'px';
        this._renderer.domElement.style.top = topLeft.y + 'px';
        this._renderer.domElement.style.width = size.x + 'px';
        this._renderer.domElement.style.height = size.y + 'px';

        this._camera.aspect = size.x / size.y;
        this._camera.position.set(0, 0, 1000);
        this._camera.lookAt(0, 0, 0);
        this._camera.updateProjectionMatrix();

        this._reproject();
    },

    _reproject: function () {
        const map = this._map;
        const size = map.getSize();
        const baseLeft = parseFloat(this._renderer.domElement.style.left) || 0;
        const baseTop = parseFloat(this._renderer.domElement.style.top) || 0;

        this._buildings.forEach(b => {
            const shape = new THREE.Shape();
            const fp = b.footprint;

            fp.geometry.forEach((pt, i) => {
                const [lat, lng] = pt;
                const layerPt = map.latLngToLayerPoint([lat, lng]);
                const x = layerPt.x - baseLeft;
                const yFlipped = -layerPt.y + baseTop + size.y;
                if (i === 0) shape.moveTo(x, yFlipped);
                else shape.lineTo(x, yFlipped);
            });

            const depth = (fp.calculatedHeight || 10) * this._heightScale;

            // Dispose old geometry and rebuild
            this._scene.remove(b.mesh);
            this._scene.remove(b.edges);
            b.mesh.geometry.dispose();
            b.edges.geometry.dispose();

            const geo = new THREE.ExtrudeGeometry(shape, { depth, bevelEnabled: false });
            b.mesh.geometry = geo;
            b.edges.geometry = new THREE.EdgesGeometry(geo);

            this._scene.add(b.mesh);
            this._scene.add(b.edges);
        });

        this._renderer.render(this._scene, this._camera);
    },

    // ─── Zoom handling ───────────────────────────────────────
    _onZoomStart: function () {
        this._renderer.domElement.style.opacity = '0';
    },

    _onZoomEnd: function () {
        this._sync();
        this._renderer.domElement.style.opacity = '1';
    },

    // ─── Hover (raycasting) ─────────────────────────────────
    _onMousemove: function (e) {
        const size = this._map.getSize();
        this._pointer.x = (e.containerPoint.x / size.x) * 2 - 1;
        this._pointer.y = -(e.containerPoint.y / size.y) * 2 + 1;
        this._raycaster.setFromCamera(this._pointer, this._camera);

        const meshes = this._buildings.map(b => b.mesh);
        const hits = this._raycaster.intersectObjects(meshes);

        const hit = hits.length > 0 ? hits[0].object : null;

        if (hit !== this._hoveredMesh) {
            // Unhover previous
            if (this._hoveredMesh) {
                const prev = this._buildings.find(b => b.mesh === this._hoveredMesh);
                if (prev) {
                    prev.mesh.material.color.setHex(prev.colorHex);
                    prev.edges.material.opacity = 0.4;
                }
            }
            this._hoveredMesh = hit;
            if (hit) {
                const curr = this._buildings.find(b => b.mesh === hit);
                if (curr) {
                    curr.mesh.material.color.setHex(0xFFFF00);
                    curr.edges.material.opacity = 0.9;
                }
            }
            this._renderer.render(this._scene, this._camera);
        }

        // Fire hover event
        if (hit) {
            const fp = this._buildings.find(b => b.mesh === hit)?.footprint;
            if (fp) this.fire('buildingHover', { footprint: fp, containerPoint: e.containerPoint });
        } else {
            this.fire('buildingHover', { footprint: null, containerPoint: e.containerPoint });
        }
    },

    // ─── Click (raycasting) ──────────────────────────────────
    _onClick: function (e) {
        const size = this._map.getSize();
        this._pointer.x = (e.containerPoint.x / size.x) * 2 - 1;
        this._pointer.y = -(e.containerPoint.y / size.y) * 2 + 1;
        this._raycaster.setFromCamera(this._pointer, this._camera);

        const meshes = this._buildings.map(b => b.mesh);
        const hits = this._raycaster.intersectObjects(meshes);

        if (hits.length === 0) {
            this._deselect();
            this.fire('buildingClick', { footprint: null });
            return;
        }

        const hitMesh = hits[0].object;
        const building = this._buildings.find(b => b.mesh === hitMesh);
        if (building) {
            this._select(building);
            this.fire('buildingClick', { footprint: building.footprint });
        }
    },

    _select: function (building) {
        if (this._selectedMesh) {
            this._selectedMesh.mesh.material.wireframe = false;
            this._selectedMesh.mesh.material.opacity = 0.75;
        }
        building.mesh.material.wireframe = true;
        building.mesh.material.opacity = 0.5;
        this._selectedMesh = building;
        this._renderer.render(this._scene, this._camera);
    },

    _deselect: function () {
        if (this._selectedMesh) {
            this._selectedMesh.mesh.material.wireframe = false;
            this._selectedMesh.mesh.material.opacity = 0.75;
            this._selectedMesh = null;
        }
        this._renderer.render(this._scene, this._camera);
    },

    getSelected: function () {
        return this._selectedMesh ? this._selectedMesh.footprint : null;
    },

    // ─── Color by building type ──────────────────────────────
    _colorByType: function (type) {
        switch (type) {
            case 'residential': case 'apartments': case 'house': return 0x3B82F6;
            case 'commercial': case 'retail': case 'shop':       return 0xF97316;
            case 'industrial':                                    return 0x6B7280;
            case 'office':                                        return 0x8B5CF6;
            case 'school': case 'hospital': case 'public':       return 0xEF4444;
            default:                                              return 0x14B8A6;
        }
    },
});

// Factory
L.leafletThreeLayer = function (map, options) {
    return new LeafletThreeLayer(map, options);
};
