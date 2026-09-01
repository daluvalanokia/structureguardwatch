# ─── Build stage ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore (layer-cached)
COPY src/StructureWatch.Core/StructureWatch.Core.csproj src/StructureWatch.Core/
COPY src/StructureWatch.Data/StructureWatch.Data.csproj src/StructureWatch.Data/
COPY src/StructureWatch.Agents/StructureWatch.Agents.csproj src/StructureWatch.Agents/
COPY src/StructureWatch.Web/StructureWatch.Web.csproj src/StructureWatch.Web/
RUN dotnet restore src/StructureWatch.Web/StructureWatch.Web.csproj

# Copy everything and build
COPY . .
WORKDIR /src/src/StructureWatch.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# ─── Runtime stage ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "StructureWatch.Web.dll"]
