# StructureWatch — .NET Solution Architecture Spec

> **Real-Map Mode**: Live OSM building footprints, 3D extrusion over Leaflet, incremental address search, hover details, click-to-analyze (TokenSaver agent), drag-arrow AABB collision validation, scan animation.

---

## Solution Overview

| Attribute | Value |
|-----------|-------|
| Solution | `StructureWatch.sln` |
| Framework | .NET 8 (ASP.NET Core MVC) |
| Language | C# 12 |
| ORM | Entity Framework Core 8 |
| DB | SQL Server (default) / PostgreSQL (configurable) |
| Frontend | Razor Views (.cshtml) + Three.js + Leaflet (served via `wwwroot`) |
| AI Gateway | `TokenSaverAgent` — OpenAI-compatible gateway |
| Address Search | `NominatimService` — OpenStreetMap Nominatim API |
| Deployment | Azure App Service or Docker |

---

## Project Layout

```
StructureWatch.sln
├── src/
│   ├── StructureWatch.Web/          # ASP.NET Core MVC — Controllers + Razor Views
│   │   ├── Controllers/
│   │   │   ├── MapController.cs       # Map page, viewport bbox endpoint
│   │   │   ├── StructureController.cs # Footprint CRUD, collision check
│   │   │   ├── AnalysisController.cs # TokenSaver agent integration
│   │   │   └── SearchController.cs   # Nominatim address autocomplete
│   │   ├── Views/
│   │   │   ├── Map/
│   │   │   │   └── Index.cshtml       # Leaflet map + Three.js overlay + search + scan
│   │   │   ├── Shared/
│   │   │   │   └── _Layout.cshtml     # Dark-themed layout with CDN links
│   │   │   └── _ViewImports.cshtml
│   │   ├── wwwroot/
│   │   │   ├── js/
│   │   │   │   ├── structurewatch.js  # Map init, 3D sync, hover, select, search, scan
│   │   │   │   ├── collision.js       # Drag-arrow, AABB check, ghost box
│   │   │   │   └── libs/              # Three.js + Leaflet bundles
│   │   │   └── css/
│   │   │       └── structurewatch.css # Dark theme + scan radar + search dropdown
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── StructureWatch.Core/          # Domain model + logic
│   │   ├── Models/
│   │   │   ├── Building.cs            # Domain entity
│   │   │   ├── BuildingFootprint.cs   # Geometry + tags
│   │   │   ├── BoundingBox.cs         # AABB (minLat, maxLat, minLng, maxLng)
│   │   │   ├── CollisionResult.cs     # Interference list
│   │   │   └── AnalysisResult.cs      # TokenSaver output DTO
│   │   ├── Services/
│   │   │   ├── IOverpassService.cs    # OSM footprint fetch interface
│   │   │   ├── OverpassService.cs     # HttpClient + caching + rate-limit
│   │   │   ├── INominatimService.cs   # Address search interface
│   │   │   ├── NominatimService.cs    # Nominatim autocomplete client
│   │   │   └── CollisionValidator.cs # AABB collision engine
│   │   └── Extensions/
│   │       └── GeoExtensions.cs      # Lat/lng → WebMercator, height calc
│   │
│   ├── StructureWatch.Data/          # EF Core 8 persistence
│   │   ├── StructureWatchDbContext.cs
│   │   ├── Entities/
│   │   │   └── BuildingAnalysisEntity.cs
│   │   ├── Configurations/
│   │   │   └── BuildingAnalysisConfiguration.cs
│   │   └── Migrations/
│   │
│   └── StructureWatch.Agents/        # AI gateway
│       ├── TokenSaverAgent.cs         # OpenAI-compatible analysis loop
│       ├── ITokenSaverAgent.cs
│       ├── Tools/
│       │   ├── LoadCalculatorTool.cs
│       │   ├── SeismicAssessmentTool.cs
│       │   └── OccupancyClassifierTool.cs
│       └── Dtos/
│           └── AnalysisResponse.cs
│
└── tests/
    ├── StructureWatch.Core.Tests/
    │   ├── CollisionValidatorTests.cs
    │   └── OverpassServiceTests.cs
    └── StructureWatch.Web.Tests/
        └── MapControllerTests.cs
```

---

## Core Flows

### 1. Navigate Map
Open → real Leaflet map (default Manhattan) → pan/zoom → 3D buildings reproject to screen on every move/zoom → footprints fetched for new viewport → buildings extrude as 3D boxes (height from tags or levels×3.5m).

### 2. Incremental Address Search
Type city/state in search bar (debounced 300ms) → Nominatim autocomplete dropdown → click result → `map.flyTo()` at zoom 16 → radar scan animation (1.5s) → footprints fetch → 3D buildings fade in.

### 3. Hover
Mouse over building → raycaster hit → highlight yellow + show tooltip (name, address, levels, height, type).

### 4. Select & Analyze
Click building → selection (wireframe + reduced opacity) → inspector panel (OSM tags) → Analyze button → TokenSaver agent returns physical properties.

### 5. Drag-Arrow Collision
With building selected → toggle drag mode → drag arrow → ghost box follows vector → real-time AABB collision → red (interference list) or green (clear).

---

## 3D Sync Architecture (CRITICAL)

The Three.js overlay must stay perfectly aligned with Leaflet tiles:

```
Leaflet map event          Three.js action
─────────────────────────  ──────────────────────────────
zoomstart                   Hide 3D layer (opacity 0)
zoomend                     syncScene() + reprojectBuildings() + show
move (continuous)           syncScene() + reprojectBuildings()
moveend                     syncScene() + reprojectBuildings() + fetchFootprints()
resize                      syncScene()
```

`syncScene()`: Resize renderer to map size, position DOM element, update camera aspect.

`reprojectBuildings()`: For each building, convert all polygon vertices via `map.latLngToLayerPoint()`, rebuild `THREE.Shape`, recreate `ExtrudeGeometry`, re-add to scene, render.

---

## Data Flow

```
┌──────────────┐  query   ┌────────────────┐  Nominatim  ┌──────────────┐
│ Search input  │────────▶│ SearchController │──────────▶│  Nominatim    │
│ (typeahead)  │◀────────│ /api/search      │◀── JSON ───│  API          │
└──────────────┘  results └────────────────┘             └──────────────┘
       │ click result
       ▼
┌──────────────┐  flyTo   ┌────────────────┐  bbox       ┌──────────────────┐  Overpass  ┌──────────────┐
│ Leaflet map  │────────▶│ MapController   │───────────▶│  OSM Overpass    │
│ (flyTo z16)  │         │ /api/footprints  │◀── JSON ───│  API             │
└──────┬───────┘         └────────────────┘            └──────────────────┘
       │  scan animation (1.5s)
       │  3D extrude (Three.js synced to Leaflet)
       │
       ├─ hover ──▶ raycast → tooltip (name, levels, height, type)
       ├─ click ──▶ select → inspector (tags) → Analyze → TokenSaver → results
       └─ drag ───▶ ghost box → POST /api/collisions → AABB check → red/green
```

---

## Controllers Summary

| Controller | Endpoints | Responsibility |
|------------|-----------|----------------|
| `MapController` | `GET /` → view, `GET /api/footprints?bbox=` | Renders map page; proxies footprint fetch |
| `SearchController` | `GET /api/search?q=` | Nominatim address autocomplete (5 results) |
| `StructureController` | `GET /api/structures/{osmId}`, `POST /api/collisions` | Single building tags; AABB collision check |
| `AnalysisController` | `POST /api/analyze` | TokenSaver agent call + EF Core persist |

---

## Key Dependencies (NuGet)

| Package | Project |
|---------|---------|
| `Microsoft.EntityFrameworkCore.SqlServer` 8.x | Data |
| `Npgsql.EntityFrameworkCore.PostgreSQL` 8.x | Data (optional) |
| `Microsoft.EntityFrameworkCore.Tools` 8.x | Data |
| `Microsoft.Extensions.Caching.Memory` 8.x | Core |
| `Microsoft.Extensions.Http.Polly` 8.x | Core (retry for Overpass + Nominatim) |
| `System.Net.Http.Json` 8.x | Core |
| `Microsoft.AspNetCore.Mvc.Testing` 8.x | Web.Tests |
| `xunit` + `Moq` | Tests |

---

## Frontend Bundle (wwwroot)

| Library | Version | Purpose |
|---------|---------|---------|
| Leaflet | 1.9.4 | Base map (OSM tiles) |
| Three.js | r160 | 3D extrusion overlay (synced to Leaflet) |
| Tailwind CSS | 3.4 (CDN) | Dark-themed UI styling |
| Nominatim | API | Address/city/state autocomplete |

---

## Deployment

| Target | Config |
|--------|--------|
| Azure App Service | `dotnet publish -c Release` → zip → deploy. Azure SQL or PostgreSQL. |
| Docker | Multi-stage `Dockerfile` (SDK 8 → runtime 8). `docker-compose.yml` with SQL Server. |
