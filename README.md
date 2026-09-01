# StructureWatch — .NET 8 Solution

> **Real-Map Mode**: Live OSM building footprints extruded as 3D boxes over a Leaflet map, with hover details, click-to-analyze via TokenSaver agent, and drag-arrow AABB collision validation.

## Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB for dev, or Docker for full stack)
- Visual Studio 2022 or `dotnet` CLI

### Run with Docker (recommended)
```bash
cd structurewatch-dotnet
docker-compose up --build
# → http://localhost:8080
```

### Run locally
```bash
cd structurewatch-dotnet
dotnet restore
dotnet ef database update --project src/StructureWatch.Data --startup-project src/StructureWatch.Web
dotnet run --project src/StructureWatch.Web
# → http://localhost:5000
```

### Run tests
```bash
dotnet test
```

## Architecture
See [ARCHITECTURE.md](ARCHITECTURE.md) for full solution spec.

## Solution Structure
```
src/
├── StructureWatch.Web/      # ASP.NET Core MVC (Controllers + Razor + wwwroot)
├── StructureWatch.Core/      # Domain models, Overpass service, CollisionValidator
├── StructureWatch.Data/      # EF Core 8 DbContext + entities
└── StructureWatch.Agents/   # TokenSaver AI gateway

tests/
├── StructureWatch.Core.Tests/   # Collision + Overpass unit tests
└── StructureWatch.Web.Tests/    # Controller integration tests
```

## Key Features
1. **Navigate map** — Leaflet + OSM tiles, Manhattan default, pan/zoom triggers live footprint fetch
2. **3D extrusion** — Three.js overlay, height from OSM tags or levels×3.5m, color-coded by type
3. **Hover** — Tooltip with name, address, levels, height, type
4. **Select & analyze** — Click building → inspector panel → Analyze button → TokenSaver agent returns physical properties
5. **Drag-arrow collision** — Drag ghost box → real-time AABB check against all loaded footprints → green/red results
