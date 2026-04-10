# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so Docker caches the restore layer separately from source.
COPY API/RenewableEnergyAPI.csproj        API/
COPY BlazorApp/RenewableEnergyBlazor.csproj BlazorApp/
RUN dotnet restore API/RenewableEnergyAPI.csproj

# Copy remaining source and publish.
# Because the API has a <ProjectReference> to the Blazor project, this single
# publish command builds both and copies the WASM output into wwwroot.
COPY Contracts/ Contracts/
COPY API/       API/
COPY BlazorApp/ BlazorApp/
RUN dotnet publish API/RenewableEnergyAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
# Use the net8.0 ASP.NET runtime image — smaller than the SDK image.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# PORT is set by Render at runtime; Program.cs reads it and binds Kestrel.
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/energy/health || exit 1

ENTRYPOINT ["dotnet", "RenewableEnergyAPI.dll"]
