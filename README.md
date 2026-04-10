# Renewable Energy Explorer

A full-stack web application that lets users search and browse World Bank renewable energy research documents (wind & solar). Built with an ASP.NET Core API backend and a Blazor WebAssembly frontend, deployed as a single hosted unit.

---

## Live Demo

> Deploy to Render.com following the [Deployment](#deployment) section to get a shareable URL.

---

## Features

- Search World Bank documents by topic (Wind / Solar / All)
- Filter by keyword, country name, and date range
- Sortable columns (Title, Published Date, Country)
- Paginated results with configurable page size
- In-memory response caching (5 min) to reduce upstream API calls
- Full Swagger / OpenAPI documentation at `/swagger`
- Single-binary deployment — API serves the Blazor WASM client

---

## Architecture

```
Browser
  └─ Blazor WebAssembly (runs in browser)
       │  relative fetch calls (same origin, no CORS)
       ▼
ASP.NET Core API  (:8080)
  ├─ GET /api/energy/search   — paginated document search
  ├─ GET /api/energy/topics   — available topic filters
  ├─ GET /api/energy/health   — health check
  └─ serves /wwwroot          — Blazor WASM static files
       │
       ▼
World Bank Search API
  https://search.worldbank.org/api/v3/wds
```

The Blazor project is referenced by the API project. When published, the WASM output is copied into the API's `wwwroot`, so a single `dotnet run` serves both.

---

## Tech Stack

| Layer      | Technology                                      |
|------------|-------------------------------------------------|
| Frontend   | Blazor WebAssembly (.NET 8)                     |
| Backend    | ASP.NET Core Web API (.NET 8)                   |
| Data       | World Bank Open Data Search API (public, free)  |
| Caching    | `IMemoryCache` (in-process, 5-minute TTL)       |
| Docs       | Swashbuckle / Swagger UI                        |
| Deployment | Docker + Render.com                             |

---

## Project Structure

```
RenewableEnergyExplorer/
├── API/
│   ├── Controllers/
│   │   └── EnergyController.cs      # Search, Topics, Health endpoints
│   ├── Models/
│   │   └── EnergyModels.cs          # EnergySearchQuery with IValidatableObject
│   ├── Services/
│   │   └── WorldBankService.cs      # World Bank API client + Polly retry + caching
│   ├── Program.cs                   # Host config, rate limiting, exception handler
│   └── RenewableEnergyAPI.csproj
│
├── BlazorApp/
│   ├── Models/
│   │   └── Models.cs                # Client-side SearchParameters, SearchResult
│   ├── Pages/
│   │   ├── Index.razor              # Single-page UI: filters, table, pagination
│   │   └── Index.razor.css          # Scoped CSS for the UI
│   ├── Services/
│   │   └── EnergyApiService.cs      # HTTP client wrapper for the API
│   ├── wwwroot/
│   │   └── index.html               # WASM host page
│   ├── Program.cs                   # Blazor DI setup, HttpClient base address
│   └── RenewableEnergyBlazor.csproj
│
├── Contracts/
│   └── EnergyContracts.cs           # Shared models: ApiResponse<T>, EnergyDocument
│
├── API.Tests/
│   └── EnergyControllerTests.cs     # xUnit tests for the API controller
│
├── .github/workflows/
│   └── ci.yml                       # GitHub Actions CI pipeline
│
├── Dockerfile                       # Multi-stage build (SDK 8.0 → ASP.NET 8.0 runtime)
├── render.yaml                      # Render.com deployment configuration
└── README.md
```

---

## Running Locally

### Prerequisites

- [.NET 8 SDK or later](https://dotnet.microsoft.com/download) — check with `dotnet --version`

### Start

```bash
# From the repository root
ASPNETCORE_ENVIRONMENT=Development dotnet run --project API/RenewableEnergyAPI.csproj
```

| URL                              | Description              |
|----------------------------------|--------------------------|
| `http://localhost:8080`          | Blazor UI                |
| `http://localhost:8080/swagger`  | Swagger / OpenAPI docs   |
| `http://localhost:8080/api/energy/health` | Health check    |

> No second terminal needed — the API and Blazor UI run as a single process.

---

## API Reference

All responses follow a consistent envelope:

```json
{
  "success": true,
  "data": [...],
  "pagination": { "page": 1, "pageSize": 10, "totalResults": 342, "totalPages": 35 },
  "message": null
}
```

### GET `/api/energy/search`

| Parameter   | Type    | Default        | Description                                      |
|-------------|---------|----------------|--------------------------------------------------|
| `topic`     | string  | `all`          | `wind`, `solar`, or `all`                        |
| `keyword`   | string  | —              | Extra keyword appended to the title search       |
| `country`   | string  | —              | Full country name, e.g. `India`, `Germany`       |
| `startDate` | string  | —              | Lower date bound (`YYYY-MM-DD`)                  |
| `endDate`   | string  | —              | Upper date bound (`YYYY-MM-DD`)                  |
| `page`      | int     | `1`            | 1-based page number                              |
| `pageSize`  | int     | `10`           | Results per page (max 50)                        |
| `sortBy`    | string  | `DatePublished`| `DatePublished`, `Title`, or `Country`           |
| `sortOrder` | string  | `desc`         | `asc` or `desc`                                  |

**HTTP Status Codes**

| Code | Meaning                                        |
|------|------------------------------------------------|
| 200  | Results returned successfully                  |
| 400  | Invalid query parameters (details in response) |
| 500  | Unexpected server error                        |
| 502  | World Bank API returned an error               |
| 504  | World Bank API timed out                       |

### GET `/api/energy/topics`
Returns the list of available topic filter values.

### GET `/api/energy/health`
Returns `{ "status": "Healthy", "timestamp": "...", "service": "..." }`.

---

### Docker (local)

```bash
docker build -t renewable-energy-explorer .
docker run -e PORT=8080 -p 8080:8080 renewable-energy-explorer
```

---


