# Renewable Energy Explorer

> **Development branch:** All production-quality improvements (rate limiting, Polly retry, shared contracts, unit tests, CI pipeline, etc.) are on the [`development`](../../tree/development) branch.

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
│   │   └── EnergyModels.cs          # ApiResponse<T>, EnergyDocument, query models
│   ├── Services/
│   │   └── WorldBankService.cs      # World Bank API client + response caching
│   ├── Program.cs                   # Host configuration, DI, middleware pipeline
│   └── RenewableEnergyAPI.csproj
│
├── BlazorApp/
│   ├── Models/
│   │   └── Models.cs                # Client-side models mirroring API contracts
│   ├── Pages/
│   │   └── Index.razor              # Single-page UI: filters, table, pagination
│   ├── Services/
│   │   └── EnergyApiService.cs      # HTTP client wrapper for the API
│   ├── wwwroot/
│   │   └── index.html               # WASM host page
│   ├── Program.cs                   # Blazor DI setup, HttpClient base address
│   └── RenewableEnergyBlazor.csproj
│
├── Dockerfile                       # Multi-stage build (SDK 10 → ASP.NET 8 runtime)
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

## Deployment

### Render.com (free tier)

1. Push this repository to GitHub.
2. Go to [render.com](https://render.com) → **New → Web Service**.
3. Connect your GitHub repository — Render detects `render.yaml` automatically.
4. Click **Deploy**.

Render builds the Docker image, injects a `PORT` environment variable, and provides a public HTTPS URL (e.g. `https://renewable-energy-explorer.onrender.com`).

> **Note:** The free tier spins down after 15 minutes of inactivity. The first request after a cold start takes ~30 seconds; subsequent requests are fast.

### Docker (local)

```bash
docker build -t renewable-energy-explorer .
docker run -e PORT=8080 -p 8080:8080 renewable-energy-explorer
```

---

## Design Decisions

**Single hosted deployment**
Rather than separate frontend and backend deployments, the API project references the Blazor project. At publish time, the WASM output is embedded in the API's `wwwroot`. This eliminates CORS entirely and simplifies deployment to a single URL.

**In-memory caching**
The World Bank Search API has variable latency (~200–800 ms). Identical queries are cached for 5 minutes using `IMemoryCache`, reducing response times for repeated or paginated searches without needing an external cache store.

**Consistent API response envelope**
Every endpoint returns `ApiResponse<T>` with `success`, `data`, `pagination`, and `message` fields. This means the Blazor client never needs to handle different response shapes and error handling is uniform.

**Data annotations for validation**
Numeric query parameters (`Page`, `PageSize`) use `[Range]` attributes. ASP.NET Core's `[ApiController]` attribute automatically returns a `400` with structured errors for annotation failures, removing boilerplate validation code from the controller.

**Relative HTTP client base address**
`EnergyApiService` uses a relative path (`api/energy/search`) against the `HttpClient.BaseAddress`, which is set to `builder.HostEnvironment.BaseAddress` in Blazor's `Program.cs`. This means the same code works in local development and production with no environment-specific configuration.
