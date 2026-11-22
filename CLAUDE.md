# Lab-01 — Todo (monorepo)

A small full-stack to-do app, kept as **one git repository** with two top-level parts:

| Folder        | What it is                        | Stack                                  | Dev URL                 |
|---------------|-----------------------------------|----------------------------------------|-------------------------|
| `API/`        | Todo REST API (backend)           | ASP.NET Core (.NET 10) + EF Core + SQL Server | http://localhost:5184   |
| `UI-Angular/` | Todo web client (frontend)        | Angular 21 + Tailwind CSS v4           | http://localhost:4200   |

Each folder has its **own `CLAUDE.md`** with stack-specific detail — read that before
working inside it. This file is the shared/global context.

## How the two halves connect

- The Angular app calls the API over HTTP at `http://localhost:5184/api/tasks`
  (configured in `UI-Angular/src/app/task.service.ts`, constant `API_BASE`).
- The API enables **CORS** for the Angular dev origin `http://localhost:4200`
  (policy `AllowAngularDev` in `API/Lab-01/Program.cs`). If you change either port,
  update both sides.

### Task API contract (shared)

| Method | Route                   | Body              | Returns                                  |
|--------|-------------------------|-------------------|------------------------------------------|
| GET    | `/api/tasks`            | —                 | `TaskItem[]` (oldest first)              |
| GET    | `/api/tasks/{id}`       | —                 | `TaskItem` or 404                        |
| POST   | `/api/tasks`            | `{ "title": … }`  | 201 `TaskItem`, or 400 `{ errors: [] }`  |
| PUT    | `/api/tasks/{id}/done`  | —                 | updated `TaskItem`, or 404               |
| GET    | `/health`, `/health/db` | —                 | `{ "status": "healthy" }`                |

`TaskItem` shape: `{ id: number, title: string, isDone: boolean, createdAt: string }`.
The TypeScript mirror lives in `UI-Angular/src/app/task.model.ts`; the C# source of
truth is `API/Lab-01/Data/TaskItem.cs`.

## Repository layout & git

- **Single repo, rooted here** (`D:\Devops\Lab-01`). There is no nested repo inside
  `UI-Angular/` or `API/` — both are tracked as plain folders.
- **`.gitignore` is layered**: a root `.gitignore` for OS/editor/common junk, plus a
  **per-folder `.gitignore`** (`API/.gitignore` for `bin/`, `obj/`, `.vs/`;
  `UI-Angular/.gitignore` for `node_modules/`, `dist/`, `.angular/`). Keep
  stack-specific ignores in the folder they belong to.
- Push the **whole root folder** as one repository.

## Running the full stack locally

1. **API** — from `API/Lab-01/`: `dotnet run` (serves http://localhost:5184). Needs the
   SQL Server connection string; see `API/CLAUDE.md`.
2. **UI** — from `UI-Angular/`: `npm install` then `npm start` (serves http://localhost:4200).
3. Open http://localhost:4200. The API must be running or the UI shows a load error.

## Conventions

- Keep the API contract above in sync across `TaskItem.cs` and `task.model.ts`.
- Backend validation (title required, ≤ 100 chars) lives in
  `API/Lab-01/Validation/TaskValidator.cs` and is the authority; the UI mirrors the
  `maxlength` for UX but trusts the API's 400 response for the real error message.
