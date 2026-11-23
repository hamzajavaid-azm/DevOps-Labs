# DevOps Assignments — Tracking

This document tracks **only assignment-related work**: each task, its current status, and
the exact evidence. It is a **living document** — update the status and evidence whenever a
gap is closed or the pipeline changes. (Operational docs — how to run, deploy, roll back —
live in [`README.md`](./README.md).)

- **Status legend:** ✅ Done · 🟡 Partly done · ❌ Not done · ❓ Cannot verify from here
- **Last audited:** 2026-11-23 (branch `dev`) · **Last updated:** 2026-11-23 (added Slack `notify` job)
- **Note:** items marked ❓ need the GitHub side (run history, branch protection, registry
  contents) — verify them in the GitHub UI or with the `gh` CLI.

---

## Assignment 1 — Docker

| # | Task | Status | Evidence |
|---|------|--------|----------|
| 1.1 | Dockerfile exists and is understood | ✅ | `API/Dockerfile` (multi-stage, non-root `USER $APP_UID`, :8080), `UI-Angular/Dockerfile` (node→nginx, :80) |
| 1.2 | Image builds locally, correct port mapping | ✅ | `docker ps`: todo-api 5184→8080, todo-web 4200→80, db 1433 |
| 1.3 | Compose runs app + a dependency (DB) | ✅ | `docker-compose.yml`: `db` + `api` + `web`; `depends_on: condition: service_healthy` |
| 1.4 | End to end + image pushed to registry | ✅ local / ❓ remote | Containers up; local images `todo-api:dev`, `todo-web:dev`; push step in workflows. Registry contents not verified from here. |

**Notes / good practices in place:** both `.dockerignore` files present; API runs non-root;
`.env` git-ignored (only `.env.example` committed).

---

## Assignment 2 — CI with GitHub Actions

| # | Task | Status | Evidence |
|---|------|--------|----------|
| 2.1 | Workflow exists under `.github/workflows/` | ✅ | `backend.yml`, `frontend.yml` |
| 2.2 | Runs on PR to dev + push to dev | ✅ | both: `push`/`pull_request`, `branches: [dev]` (with `paths:` filters) |
| 2.3 | Build step + lint step that must pass | ✅ | backend: `dotnet format` + build; frontend: `npm run lint` (ESLint) + `npm run build` |
| 2.4 | DB migrations run as a step where needed | 🟡 | No CI migrate step; API auto-migrates on startup (`Database.MigrateAsync`) |
| 2.5 | Push to dev builds + pushes image | ✅ | `docker` job, `if: push && ref==dev`, tags `:sha` + `:dev` |
| 2.6 | Slack notification when pipeline finishes | ✅ | `notify` job in `backend.yml` + `frontend.yml` (`if: always()`, posts ✅/❌ to Slack via `SLACK_WEBHOOK_URL`). Needs the `SLACK_WEBHOOK_URL` secret set in GitHub. |
| 2.7 | Workflow is a required status check | ❓ | Branch protection not checkable here; also mind the `paths:`-filter + required-check trap |

---

## Assignment 3 — Automated testing in CI

| # | Task | Status | Evidence |
|---|------|--------|----------|
| 3.1 | At least one test I wrote for a feature I own | ✅ | `API/tests/Lab-01.Tests/TaskValidatorTests.cs` (5 xUnit tests); `UI-Angular/src/app/app.spec.ts` (2 tests) |
| 3.2 | Runs on every PR; a failing test blocks merge | 🟡 | Backend `dotnet test` runs in CI; **frontend tests are not run in CI**; "blocks merge" depends on branch protection (❓) |
| 3.3 | A passing run exists in the logs | ❓ | No run history available here |

---

## Assignment 4 — Staging deploy

| # | Task | Status | Evidence |
|---|------|--------|----------|
| 4.1 | Deploy step/job to staging (environment, secrets, runner) | 🟡 | `backend.yml` deploy: `self-hosted` + `environment: development`; frontend deploy has **no** `environment`; env named `development` not `staging` |
| 4.2 | A feature deployed end to end with no hand-off | ❓ | Running `:dev` containers consistent with it, but pipeline-driven deploy not verifiable here |
| 4.3 | Verified in staging (smoke test / health check) | ❌ | No post-deploy health check in the workflows (endpoints exist, but the pipeline doesn't assert them) |
| 4.4 | README documents pipeline, deploy, rollback | ✅ | `README.md` sections 4–6 (added 2026-11-23) |

---

## Gate 2 — final check

| Item | Status | Reason |
|------|--------|--------|
| ≥1 staging deploy through the pipeline that I ran myself | ❓ | Deploy job exists and `:dev` containers run, but a pipeline-driven run can't be confirmed here |
| ≥1 automated test I wrote, running in CI | ✅ | `TaskValidatorTests.cs` runs via `dotnet test` in `backend.yml` |
| Build success rate ≥ 90% | ❓ | No access to recent workflow runs to calculate the rate |

---

## Open gaps → next actions (priority order)

1. **Slack notification** (2.6) — add a notify step (success + failure, `if: always()`) to both workflows.
2. **Post-deploy health check** (4.3) — `curl --fail .../health` and `/health/db` in the deploy jobs.
3. **Frontend tests in CI** (3.2) — add a `npm test` (Vitest, non-watch) step to `frontend.yml`.
4. **Required-check vs path-filter** (2.7) — add an always-running merge gate, then set branch protection.
5. **Align staging environment** (4.1) — add `environment:` to the frontend deploy; consider renaming `development` → `staging`.
6. **Verify on GitHub** (2.7, 3.3, Gate 2) — confirm branch protection, a green run, and build success rate (needs `gh` or the UI).
7. **Harden deploy secret file** — `chmod 600` on `appsettings.Production.json`; ensure `todo-net` / `db` exist on the runner.

> When any gap above is closed, update its row's status + evidence here, reflect the
> change in `README.md` if it affects how the pipeline/deploy/rollback works, **and**
> update the at-a-glance **Status tracker** table near the top of `README.md` (and its
> "Last updated" date) so the two stay consistent.
