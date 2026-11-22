# Lab-01 — TaskTracker API

A small ASP.NET Core (.NET 10) Web API for a to-do list, backed by SQL Server via
Entity Framework Core. This part runs **locally**; containerizing it with Docker is
the next part of the assignment.

## Project structure

```
Lab-01/
├── Lab-01.slnx                # Solution (API project + test project)
├── README.md
├── Lab-01/                    # The Web API project
│   ├── Lab-01.csproj
│   ├── Program.cs             # DI, migrate-on-startup (with retry), minimal-API endpoints
│   ├── Lab-01.http            # Sample requests for every endpoint (Visual Studio)
│   ├── appsettings.json       # ConnectionStrings:Default is empty here
│   ├── appsettings.Development.json  # Local connection string (LocalDB)
│   ├── Controllers/
│   │   └── WeatherForecastController.cs   # Original sample endpoint (kept)
│   ├── Data/
│   │   ├── TaskItem.cs        # EF Core entity (Id, Title, IsDone, CreatedAt)
│   │   └── AppDbContext.cs    # DbContext with DbSet<TaskItem> Tasks
│   ├── Validation/
│   │   └── TaskValidator.cs   # Static title validation (required, max 100)
│   └── Migrations/            # EF Core migrations (InitialCreate)
└── tests/
    └── Lab-01.Tests/          # xUnit tests for TaskValidator
        └── TaskValidatorTests.cs
```

## Endpoints

| Method | Route                   | Description                                    |
|--------|-------------------------|------------------------------------------------|
| POST   | `/api/tasks`            | Create a task; body `{ "title": "..." }`; 400/201 |
| GET    | `/api/tasks`            | View all tasks, ordered by Id                  |
| GET    | `/api/tasks/{id}`       | One task, or 404                               |
| PUT    | `/api/tasks/{id}/done`  | Mark a task as done                            |
| GET    | `/health`               | Liveness: `{ "status": "healthy" }`            |
| GET    | `/health/db`            | 200 if the DB is reachable, otherwise 503      |
| GET    | `/weatherforecast`      | Original sample endpoint                        |

## Database

Uses your local **SQL Server LocalDB** instance — no password, Windows authentication.
The connection string lives in `appsettings.Development.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=TaskTracker;Trusted_Connection=True;TrustServerCertificate=True
```

The schema is managed by EF Core migrations. The database is created/updated two ways:
- automatically on app startup (`Database.Migrate()` in `Program.cs`), and
- manually with the EF tools (below).

### Run / update migrations manually

```powershell
# create a new migration after changing the model
dotnet ef migrations add <Name> --project Lab-01

# apply migrations to the database
dotnet ef database update --project Lab-01
```

## Run the API

```powershell
dotnet run --project Lab-01
```

It listens on **http://localhost:5184** (the `http` launch profile, Development environment).

## Test it (PowerShell)

Use `curl.exe` on PowerShell (the bare `curl` is an alias for `Invoke-WebRequest`).

```powershell
# Create a todo
curl.exe -X POST http://localhost:5184/api/tasks -H "Content-Type: application/json" -d "{\"title\":\"Buy milk\"}"

# View all todos
curl.exe http://localhost:5184/api/tasks

# Health checks
curl.exe http://localhost:5184/health
curl.exe http://localhost:5184/health/db
```

You can also open `Lab-01/Lab-01.http` in Visual Studio and send each request from there.

## View the data in SSMS

- **Server name:** `(localdb)\MSSQLLocalDB`
- **Authentication:** Windows Authentication
- **Database:** `TaskTracker` → table `dbo.Tasks`

## Run the tests

```powershell
dotnet test
```
