# Lab-01 — Todo (full-stack monorepo)

A small full-stack to-do application kept as **one git repository**, with a CI/CD
pipeline that builds, tests, containerizes, and deploys both halves.

| Folder        | What it is            | Stack                                         | URL                   |
|---------------|-----------------------|-----------------------------------------------|-----------------------|
| `API/`        | Todo REST API         | ASP.NET Core (.NET 10) + EF Core + SQL Server | http://localhost:5184 |
| `UI-Angular/` | Todo web client       | Angular 21 + Tailwind CSS v4 (served by nginx)| http://localhost:4200 |

> **Keep this file current.** Whenever the Docker setup, the GitHub Actions workflows,
> the deploy, the secrets, or the rollback steps change, update this README **and**
> [`ASSIGNMENTS.md`](./ASSIGNMENTS.md) in the same change. See the "Maintaining the docs"
> section at the bottom.

---

## Status tracker

At-a-glance progress. Full task-by-task detail and evidence live in
[`ASSIGNMENTS.md`](./ASSIGNMENTS.md). **Last updated: 2026-11-23.**

**Legend:** ✅ Done · 🟡 Partly done · ❌ Not done · ❓ Cannot verify from here

| Assignment | Progress | Status |
|------------|----------|--------|
| 1 — Docker | 4 / 4 | ✅✅✅✅ |
| 2 — CI with GitHub Actions | 4 / 7 | ✅✅✅🟡✅❌❓ |
| 3 — Automated testing in CI | 1 / 3 | ✅🟡❓ |
| 4 — Staging deploy | 1 / 4 | 🟡❓❌✅ |

**Gate 2:** test-in-CI ✅ · staging deploy via pipeline ❓ · build success rate ≥ 90% ❓

**Top open gaps:** ❌ Slack notification (2.6) · ❌ post-deploy health check (4.3) ·
🟡 frontend tests not in CI (3.2) · ❓ branch protection / required check (2.7).
→ See the "Open gaps → next actions" list in [`ASSIGNMENTS.md`](./ASSIGNMENTS.md).

---

## 1. Architecture

```
Browser ──HTTP──▶ UI-Angular (nginx :4200)
                       │ calls http://localhost:5184/api/tasks
                       ▼
                   API (.NET :5184 ⇄ container :8080)
                       │ EF Core
                       ▼
                   SQL Server 2022 (:1433)
```

- The Angular app calls the API at `http://localhost:5184/api/tasks`
  (`UI-Angular/src/app/task.service.ts`, constant `API_BASE`).
- The API enables **CORS** for `http://localhost:4200`
  (policy `AllowAngularDev` in `API/Lab-01/Program.cs`). Change one port → change both.
- The API runs EF Core **migrations on startup** (`Database.MigrateAsync`, with retries),
  so the schema is created automatically once the database is reachable. There is no
  separate "apply migrations" step to run by hand in normal operation.

### Task API contract

| Method | Route                   | Body              | Returns                                 |
|--------|-------------------------|-------------------|-----------------------------------------|
| GET    | `/api/tasks`            | —                 | `TaskItem[]` (oldest first)             |
| GET    | `/api/tasks/{id}`       | —                 | `TaskItem` or 404                       |
| POST   | `/api/tasks`            | `{ "title": … }`  | 201 `TaskItem`, or 400 `{ errors: [] }` |
| PUT    | `/api/tasks/{id}/done`  | —                 | updated `TaskItem`, or 404              |
| GET    | `/health`               | —                 | `{ "status": "healthy" }` (liveness)    |
| GET    | `/health/db`            | —                 | 200 if DB reachable, else 503           |

`TaskItem` = `{ id: number, title: string, isDone: boolean, createdAt: string }`.
C# source of truth: `API/Lab-01/Data/TaskItem.cs`; TS mirror: `UI-Angular/src/app/task.model.ts`.

---

## 2. Run locally (without Docker)

1. **API** — from `API/Lab-01/`: `dotnet run` → http://localhost:5184
   (uses LocalDB from `appsettings.Development.json`; see `API/CLAUDE.md`).
2. **UI** — from `UI-Angular/`: `npm install` then `npm start` → http://localhost:4200.
3. Open http://localhost:4200. The API must be running or the UI shows a load error.

---

## 3. Run the full stack with Docker Compose

`docker-compose.yml` lives at the repo root and orchestrates **db + api + web**.

```bash
cp .env.example .env        # set a strong SA_PASSWORD
docker compose up --build
```

- `db` — SQL Server 2022, data persists in the `mssql-data` volume, with a `sqlcmd`
  healthcheck. The `api` waits for `db` to be healthy (`depends_on: condition: service_healthy`).
- `api` — built from `API/Dockerfile`, host `5184` → container `8080`. The connection
  string is injected at runtime via `ConnectionStrings__Default` (never baked into the image).
- `web` — built from `UI-Angular/Dockerfile` (nginx), host `4200` → container `80`.

Endpoints after start: UI http://localhost:4200, API http://localhost:5184,
health http://localhost:5184/health.

### Images

| Image                               | Built from              | Runtime                         |
|-------------------------------------|-------------------------|---------------------------------|
| `<dockerhub-user>/todo-api`         | `API/Dockerfile`        | `aspnet:10.0`, **non-root**, :8080 |
| `<dockerhub-user>/todo-web`         | `UI-Angular/Dockerfile` | `nginx:alpine`, :80             |

Both Dockerfiles are multi-stage, and each folder has a `.dockerignore` to keep the
build context small and secrets out of the image.

---

## 4. CI/CD pipeline (GitHub Actions)

Two workflows under `.github/workflows/`, split by folder with `paths:` filters so each
only runs when its half changes:

| Workflow        | Triggers                                  | Jobs                                   |
|-----------------|-------------------------------------------|----------------------------------------|
| `backend.yml`   | push + PR to `dev`, paths `API/**`        | `build` → `docker` → `deploy`          |
| `frontend.yml`  | push + PR to `dev`, paths `UI-Angular/**` | `build` → `docker` → `deploy`          |

### `build` job (runs on every PR and push to `dev`)

- **Backend:** restore → **lint** (`dotnet format --verify-no-changes --severity warn`)
  → build (Release) → **test** (`dotnet test`, xUnit).
- **Frontend:** `npm ci` → **lint** (`npm run lint`, ESLint) → build (`npm run build`).

A lint or test failure fails the job and blocks the merge (when the check is required —
see branch protection below).

### `docker` job (only on push to `dev`)

Condition: `github.event_name == 'push' && github.ref == 'refs/heads/dev'`.
Logs in to Docker Hub and pushes two tags per image:

- `:${{ github.sha }}` — immutable, one per commit (**use this for rollback**).
- `:${{ github.ref_name }}` — the moving `dev` tag.

### `deploy` job (only on push to `dev`)

Runs on a **self-hosted** runner bound to the GitHub **`development`** environment
(backend). It pulls `:dev`, writes `appsettings.Production.json` from a secret, and
restarts the container.

### Required secrets

Set these in the repo (Settings → Secrets and variables → Actions) and, where
environment-scoped, under the `development` environment:

| Secret                | Used for                                              |
|-----------------------|-------------------------------------------------------|
| `DOCKERHUB_USERNAME`  | Docker Hub login + image namespace                    |
| `DOCKERHUB_TOKEN`     | Docker Hub access token (not a password)              |
| `APPSETTINGS_JSON`    | Contents of `appsettings.Production.json` (conn string) on the deploy runner |

### Branch protection

`dev` should require the `build` checks to pass before merge. Because the workflows use
`paths:` filters, be careful making a check "required": a PR that touches only the *other*
folder will not trigger that workflow, and a required-but-never-run check stays pending.

---

## 5. Deploy to staging

Staging is the self-hosted Ubuntu runner. On push to `dev`, after the image is pushed,
the `deploy` job runs on that box:

**API** (`backend.yml`):
```bash
docker pull  $DOCKERHUB_USERNAME/todo-api:dev
docker rm -f todo-api || true
docker run -d --name todo-api --network todo-net --restart unless-stopped \
  -p 5184:8080 -e ASPNETCORE_ENVIRONMENT=Production \
  -v ~/todo-api/config/appsettings.Production.json:/app/appsettings.Production.json:ro \
  $DOCKERHUB_USERNAME/todo-api:dev
```

**UI** (`frontend.yml`):
```bash
docker pull  $DOCKERHUB_USERNAME/todo-web:dev
docker rm -f todo-web || true
docker run -d --name todo-web --network todo-net --restart unless-stopped \
  -p 4200:80 $DOCKERHUB_USERNAME/todo-web:dev
```

Prerequisites on the runner (one-time): the `todo-net` Docker network and a reachable
`db` container must exist.

### Verify the deploy (smoke test)

```bash
curl --fail http://localhost:5184/health      # API liveness  → {"status":"healthy"}
curl --fail http://localhost:5184/health/db   # DB readiness   → 200 when DB reachable
curl --fail http://localhost:4200             # UI responds
```

---

## 6. Rollback

Images are tagged per commit, so rolling back is re-running the previous **`:<sha>`** tag.

1. Find the last good commit SHA (from the green run in the Actions history, or
   `docker images <user>/todo-api`).
2. Re-point the running container at that tag:

```bash
# API
docker pull  $DOCKERHUB_USERNAME/todo-api:<good-sha>
docker rm -f todo-api || true
docker run -d --name todo-api --network todo-net --restart unless-stopped \
  -p 5184:8080 -e ASPNETCORE_ENVIRONMENT=Production \
  -v ~/todo-api/config/appsettings.Production.json:/app/appsettings.Production.json:ro \
  $DOCKERHUB_USERNAME/todo-api:<good-sha>

# UI
docker pull  $DOCKERHUB_USERNAME/todo-web:<good-sha>
docker rm -f todo-web || true
docker run -d --name todo-web --network todo-net --restart unless-stopped \
  -p 4200:80 $DOCKERHUB_USERNAME/todo-web:<good-sha>
```

3. Re-run the smoke test in section 5.
4. **Database:** migrations are additive; a code rollback does not undo an applied
   migration. If a bad migration must be reverted, do it deliberately with
   `dotnet ef database update <PreviousMigration> --project API/Lab-01` against the DB.

> Tip: to make rollback one command, keep the current and previous `:<sha>` known — the
> Actions run page shows the SHA for every deploy.

---

## 7. Repository layout & git

- Single repo rooted at `D:\Devops\Lab-01`; no nested repos. `API/` and `UI-Angular/`
  are plain folders.
- Layered `.gitignore`: root for OS/editor junk and `.env`; `API/.gitignore` for
  `bin/ obj/ .vs/`; `UI-Angular/.gitignore` for `node_modules/ dist/ .angular/`.
- `.env` is git-ignored; only `.env.example` (placeholder password) is committed.
- Default branch `main`; the pipeline targets the development branch `dev`.

---

## 8. Maintaining the docs

This README and [`ASSIGNMENTS.md`](./ASSIGNMENTS.md) are **living documents**. When you
change any of the following, update both in the same commit:

- Dockerfiles, `.dockerignore`, or `docker-compose.yml`.
- Any `.github/workflows/*.yml` (jobs, triggers, secrets, deploy, notifications).
- The deploy target, environment, runner, or rollback procedure.
- Added/removed tests, or how tests run in CI.

`ASSIGNMENTS.md` additionally tracks the status of each assignment task — update its
status table and evidence whenever a gap is closed. When you do, also update the
**Status tracker** table near the top of this README (and its "Last updated" date) so the
at-a-glance view stays consistent with `ASSIGNMENTS.md`.
