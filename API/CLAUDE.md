# API — TaskTracker (backend)

ASP.NET Core (.NET 10) Web API for the to-do list, backed by SQL Server via EF Core.
Part of the Lab-01 monorepo — see the root `CLAUDE.md` for how this connects to the
Angular UI. A fuller human-facing write-up is in `README.md`.

## Layout

```
API/
├── Lab-01.slnx                 # Solution (API + tests)
├── README.md                   # Human-facing docs
├── Dockerfile                  # Multi-stage build (sdk:10.0 -> aspnet:10.0, non-root, port 8080)
├── .dockerignore               # Keeps bin/obj/.vs/.git/tests out of the build context
├── docker-compose.yml          # API + SQL Server; injects ConnectionStrings__Default
├── .env.example                # Template for SA_PASSWORD (copy to .env, git-ignored)
├── .gitignore                  # Backend ignores: bin/, obj/, .vs/, *.user, .env
└── Lab-01/
    ├── Program.cs              # DI, CORS, migrate-on-startup, minimal-API endpoints
    ├── appsettings*.json       # ConnectionStrings:Default (empty in base; LocalDB in Development)
    ├── Controllers/            # WeatherForecastController (original sample, kept)
    ├── Data/
    │   ├── TaskItem.cs         # EF entity: Id, Title, IsDone, CreatedAt
    │   └── AppDbContext.cs     # DbContext with DbSet<TaskItem> Tasks
    ├── Validation/TaskValidator.cs  # Title rules (required, ≤ 100) — the source of truth
    └── Migrations/             # EF Core migrations
└── tests/Lab-01.Tests/        # xUnit tests for TaskValidator
```

## Endpoints

All task endpoints are minimal APIs defined directly in `Program.cs`:

| Method | Route                   | Notes                                       |
|--------|-------------------------|---------------------------------------------|
| GET    | `/api/tasks`            | All tasks, ordered by Id                     |
| GET    | `/api/tasks/{id:int}`   | One task, or 404                             |
| POST   | `/api/tasks`            | Body `{ title }`; validated → 201 or 400 `{ errors }` |
| PUT    | `/api/tasks/{id:int}/done` | Mark done → updated task, or 404          |
| GET    | `/health`               | Liveness                                     |
| GET    | `/health/db`            | Readiness — 200 if DB reachable, else 503    |

## Key conventions

- **CORS**: policy `AllowAngularDev` permits `http://localhost:4200` (the Angular dev
  server). Added in `Program.cs` via `AddCors` + `UseCors`. Add new front-end origins
  there.
- **Connection string** is read from `ConnectionStrings:Default` and supplied at runtime
  (env var `ConnectionStrings__Default` in containers) — never hard-code it. Local dev
  uses LocalDB from `appsettings.Development.json`.
- **Migrate on startup**: `Program.cs` runs `Database.MigrateAsync()` with retries so the
  app survives SQL Server not being ready yet.
- **`CreatedAt` is always UTC.**
- Validation lives only in `TaskValidator` — keep business rules there, not in endpoints.

## Common commands (run from `API/`)

```bash
dotnet run --project Lab-01             # start API on http://localhost:5184
dotnet test                             # run xUnit tests
dotnet ef migrations add <Name> --project Lab-01   # new migration
```

The default `dotnet run` uses the `http` profile (port 5184). The `https` profile
(7287) also exists but requires a trusted dev cert; the UI is wired to the http port.

## Docker (run from `API/`)

```bash
cp .env.example .env            # then set a strong SA_PASSWORD
docker compose up --build       # API on http://localhost:5184, SQL Server on 1433
```

- The image runs Kestrel on HTTP **8080** inside the container; compose maps it to host
  **5184** so the Angular UI's `API_BASE` keeps working unchanged.
- `ConnectionStrings__Default` is injected by compose (service `db`) — never baked into
  the image. The API migrates on startup once the DB is healthy.
- Build the image alone: `docker build -t tasktracker-api .` (context is `API/`).
