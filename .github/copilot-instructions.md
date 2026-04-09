# Copilot Instructions

## Build & Run

This project uses [Task](https://taskfile.dev/) as its task runner. All commands are defined in `Taskfile.yml`.

```sh
task run          # run API + frontend in parallel
task run-api      # API only (dotnet run, port 5062)
task run-web      # frontend only (vite dev, port 5173)
task build        # build both
task build-api    # dotnet build (from src/api/)
task build-web    # npm install + npm run build (from src/web/)
task db           # create and apply EF Core migrations
```

### Frontend lint

```sh
cd src/web && npx eslint .                  # full lint
cd src/web && npx eslint src/pages/StatsPage.tsx  # single file
```

There are no tests in this project.

## Architecture

Single-container full-stack app: .NET 10 API + React 19 SPA served from `wwwroot`.

- **Backend** (`src/api/`): .NET 10 Web API, EF Core with SQLite, JWT + API key auth.
- **Frontend** (`src/web/`): React 19, TypeScript, Vite, PWA via `vite-plugin-pwa`.
- **Production**: multi-stage Docker build copies the Vite build output into the .NET `wwwroot/`. The API serves the SPA via `MapFallbackToFile("index.html")` — no separate web server.
- **Dev**: Vite dev server proxies `/api` and `/uploads` to the .NET backend (port 5209 in vite.config.ts).

### API structure

Controllers are thin — business logic lives in `Services/`. Each service has an interface (`IWatchService` → `WatchService`) registered in `Program.cs`. Controllers use primary-constructor DI.

```
Controllers/    → API endpoints, derive UserId from JWT claims
Services/       → Business logic, one interface + implementation per feature
Models/         → EF Core entities
DTOs/           → Request/response objects, grouped by feature (AuthDtos.cs, AdminDtos.cs, etc.)
Data/           → AppDbContext with SQLite config
Authentication/ → Custom ApiKeyAuthenticationHandler for X-API-Key header auth
```

### Authentication

The API supports two auth schemes via a policy scheme (`JwtOrApiKey`):
- **JWT Bearer** — primary auth for the SPA. Token stored in `localStorage`, claims include NameIdentifier, Name, Email, Role.
- **API Key** — `X-API-Key` header, keys stored hashed (BCrypt). Used for programmatic access.

Admin endpoints require `[Authorize(Roles = "Admin")]`.

### Frontend structure

```
src/api/        → Axios-based API client modules, one per feature
src/components/ → Reusable components (WatchCard, WatchForm, ProtectedRoute, etc.)
src/context/    → AuthContext (JWT state) and PreferencesContext (theme, timezone, view)
src/pages/      → Page components, one per route
src/types/      → Shared TypeScript types in index.ts
```

State management uses React Context + `localStorage` — no Redux or external state library. The shared Axios client (`src/api/client.ts`) attaches the bearer token and handles 401 → redirect to `/login`.

## Key Conventions

- **Controller routes**: `[Route("api/[controller]")]` — all endpoints are under `/api/`.
- **User scoping**: Controllers extract the user ID from `ClaimTypes.NameIdentifier` to scope all data access.
- **DTOs per feature**: Multiple DTOs live in a single file when related (e.g., `AuthDtos.cs` has login request, register request, and response DTOs).
- **Action results**: Controllers return `ActionResult<T>` with standard HTTP status codes (`Ok`, `CreatedAtAction`, `NotFound`, `BadRequest`, `Conflict`).
- **Error handling**: Local try/catch in services, no global exception middleware.
- **EF Migrations**: tracked in `src/api/Migrations/` and committed to git. The app runs `Database.MigrateAsync()` on startup.
- **Frontend API modules**: Each feature has a dedicated file in `src/web/src/api/` (e.g., `watches.ts`, `auth.ts`, `admin.ts`) that exports functions calling the shared Axios client.
- **PWA behavior**: The app detects standalone PWA mode via `useIsPwa()` hook and adjusts the UI (compact header, bottom nav bar).
- **Build metadata**: Vite injects `__BUILD_DATE__` and `__BUILD_SHA__` globals at build time via `define` in `vite.config.ts`.
