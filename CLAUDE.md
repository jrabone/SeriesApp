# SeriesApp — project rules

## What this is

A monorepo holding three applications that ship as one product:

| Path | Stack | Role |
|---|---|---|
| `apps/web` | Next.js 16 (App Router) · React 19 · TypeScript · Tailwind v4 | Web client |
| `apps/mobile` | Expo SDK 57 · React Native 0.86 · TypeScript · Expo Router | iOS/Android client |
| `backend` | ASP.NET Core · .NET 10 · Minimal APIs | API, a **modular monolith** |
| `docs` | Markdown | Architecture notes and decisions |

PostgreSQL is the intended database. It is **not configured yet** — see "Not present yet".

## Architecture rules

- **Modular monolith, not microservices.** One deployable API host. Features are vertical
  modules inside it, split by business capability, not by technical layer.
- A module implements `IModule` (`backend/src/SeriesApp.SharedKernel`) and is added to the
  explicit list in `backend/src/SeriesApp.Api/ModuleRegistry.cs`. No assembly scanning —
  the active set stays compile-time checked.
- **Modules never reference each other.** Anything that must cross a boundary goes into
  `SeriesApp.SharedKernel` as a contract. If two modules need each other's internals, the
  boundary is wrong.
- `SeriesApp.Api` is the composition root only: host configuration, cross-cutting
  middleware, module registration. Business logic does not live there.
- Do not introduce a second deployable service without an ADR in `docs/`.

## Repo conventions

- **No npm workspaces.** `apps/web` and `apps/mobile` are independent npm projects with
  their own lockfiles; install and run them from their own directory. They intentionally
  resolve different React versions. Revisit only when shared code actually exists.
- Mobile packages install with `npx expo install <pkg>`, never bare `npm install <pkg>`,
  so versions stay compatible with the SDK.
- Backend restore is pinned to nuget.org by `backend/NuGet.config`. Keep it that way so
  builds do not depend on feeds configured on one developer's machine.

## Code conventions

**TypeScript (both clients)** — `strict` is on. Import via the `@/*` alias, not deep
relative paths. Web components live under `apps/web/src/`.

**React Native (`apps/mobile`)** — `src/app/` is routes **only**; every file there is a
route. Shared UI goes in `src/components/`, screen bodies in `src/screens/`, helpers in
`src/utils/`. Files are kebab-case. Keep `StyleSheet.create({...})` at the bottom of the
component file. Colocate tests next to the file they cover.

**C# (`backend`)** — nullable reference types on, file-scoped namespaces, implicit usings.
`TreatWarningsAsErrors` is enabled solution-wide: fix the cause, do not suppress. Shared
settings live in `backend/Directory.Build.props`; keep `.csproj` files to their own
references.

## Commands

```bash
# web      (http://localhost:3000)
cd apps/web     && npm install && npm run dev
cd apps/web     && npm run build && npm run lint && npm run typecheck

# mobile   (http://localhost:8081)
cd apps/mobile  && npm install && npx expo start
cd apps/mobile  && npm run typecheck && npx expo-doctor

# backend  (http://localhost:5080, https://localhost:7080)
cd backend      && dotnet run --project src/SeriesApp.Api
cd backend      && dotnet build SeriesApp.slnx && dotnet test SeriesApp.slnx
```

The solution file is `SeriesApp.slnx` — the XML solution format that is the .NET 10 SDK
default. It needs .NET SDK 10+, VS 2022 17.14+, or Rider 2025.1+.

## Not present yet

Absent **by design**. Their absence is not a bug, and adding one is a deliberate decision,
not incidental cleanup:

- No database, EF Core, migrations, or entities — PostgreSQL is planned, unconfigured.
- No authentication or authorization.
- No movie/series domain logic. `/health` is the only endpoint.
- No Docker, CI pipeline, or deployment configuration.
- No shared `packages/` directory between web and mobile.
