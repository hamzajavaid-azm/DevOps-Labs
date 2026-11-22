# UI-Angular — TaskTracker (frontend)

Angular 21 (standalone, signals, zoneless-style) + Tailwind CSS v4 client for the
TaskTracker API. Part of the Lab-01 monorepo — see the root `CLAUDE.md` for the shared
API contract and how this connects to the backend.

## Layout

```
UI-Angular/
├── .gitignore               # Frontend ignores: node_modules/, dist/, .angular/, etc.
├── package.json             # Angular 21, Tailwind v4, Vitest
├── .postcssrc.json          # Wires @tailwindcss/postcss
└── src/
    ├── styles.css           # Global styles — just `@import 'tailwindcss';`
    ├── index.html
    └── app/
        ├── app.config.ts    # App providers — includes provideHttpClient(withFetch())
        ├── app.routes.ts    # Routes (currently empty — single-page app)
        ├── app.ts           # Root component: todo state (signals) + API calls
        ├── app.html         # Tailwind UI: add form, task list, done toggle, states
        ├── app.spec.ts      # Vitest component tests (HttpClient mocked)
        ├── task.model.ts    # TaskItem / CreateTaskRequest — mirrors the API's C# model
        └── task.service.ts  # HttpClient wrapper over /api/tasks (all four endpoints)
```

## How it talks to the API

- `task.service.ts` holds the base URL: `const API_BASE = 'http://localhost:5184/api/tasks'`.
  **This is the single place to change the API location.**
- The API must allow this origin via CORS (it does — `AllowAngularDev` for
  `http://localhost:4200`). If you change the dev port, update the API's CORS policy too.
- `HttpClient` is provided in `app.config.ts`; without it the service cannot be injected.

## Patterns in use

- **Signals for all UI state** (`tasks`, `loading`, `error`, `newTitle`, `adding`) with
  `computed` for derived counts. Prefer signals over `BehaviorSubject`/manual change
  detection when adding features.
- **Standalone components** with explicit `imports: [...]` — no NgModules. The root
  component imports `FormsModule` (for `ngModel`) and `DatePipe`.
- **Error handling**: `readError()` surfaces the API's `400 { errors: [] }` message when
  present, else a friendly fallback. Reuse it for new calls.
- **Control flow**: use the new `@if` / `@for` / `@else` template syntax (not `*ngIf`).
- **Styling**: Tailwind utility classes in templates. Palette is slate + indigo accent;
  done = emerald, pending = amber. Keep it clean and minimal; no component CSS files are
  needed (`app.css` is empty).

## Common commands (run from `UI-Angular/`)

```bash
npm install        # restore dependencies
npm start          # ng serve → http://localhost:4200
npm run build      # production build into dist/
npm test           # Vitest
```

Start the API first (see `API/CLAUDE.md`) — with it down, the task list shows a load error.
