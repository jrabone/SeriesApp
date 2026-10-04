# SeriesApp

A monorepo containing the SeriesApp web client, mobile client, and backend API.

```
SeriesApp/
├── apps/
│   ├── web/      Next.js 16 · React 19 · TypeScript · Tailwind CSS v4
│   └── mobile/   Expo SDK 57 · React Native 0.86 · TypeScript · Expo Router
├── backend/      ASP.NET Core Web API · .NET 10 · modular monolith
└── docs/         Architecture notes
```

This is a project scaffold. There is no product functionality yet — see
[Current state](#current-state).

## Prerequisites

| Tool | Version | Needed for |
|---|---|---|
| [Node.js](https://nodejs.org) | 20.9+ (verified on 24.18) | web, mobile |
| npm | 10+ (verified on 11.16) | web, mobile |
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ (verified on 10.0.302) | backend |
| [Expo Go](https://expo.dev/go) or a dev build | latest | running mobile on a device |

`apps/web` and `apps/mobile` are **independent npm projects** — there is no root
`package.json` and no workspace. Install dependencies inside each app directory.

Building the mobile app for iOS additionally requires macOS with Xcode; Android requires
Android Studio. Neither is needed to run the app through Expo Go.

## Run the web app

```bash
cd apps/web
npm install
cp .env.example .env.local   # optional, sets the API base URL
npm run dev
```

Open <http://localhost:3000>.

| Command | Purpose |
|---|---|
| `npm run dev` | Dev server with hot reload |
| `npm run build` | Production build |
| `npm run start` | Serve the production build |
| `npm run lint` | ESLint |
| `npm run typecheck` | `tsc --noEmit` |

## Run the mobile app

```bash
cd apps/mobile
npm install
npx expo start
```

Metro starts on <http://localhost:8081> and prints a QR code. Scan it with Expo Go, or
press `a` for an Android emulator, `i` for an iOS simulator (macOS only), or `w` for web.

| Command | Purpose |
|---|---|
| `npm run start` | Start the Metro dev server |
| `npm run android` / `npm run ios` / `npm run web` | Start and open that platform |
| `npm run lint` | ESLint (Expo config) |
| `npm run typecheck` | `tsc --noEmit` |
| `npx expo-doctor` | Check project and dependency health |

Install new native packages with `npx expo install <pkg>` rather than `npm install <pkg>`,
so versions stay compatible with the Expo SDK.

## Run the backend

```bash
cd backend
dotnet run --project src/SeriesApp.Api
```

| URL | |
|---|---|
| <http://localhost:5080/health> | Health check — returns `Healthy` |
| <https://localhost:7080> | HTTPS profile |
| <http://localhost:5080/openapi/v1.json> | OpenAPI document (Development only) |

For the HTTPS profile, trust the local development certificate once:
`dotnet dev-certs https --trust`.

| Command | Purpose |
|---|---|
| `dotnet run --project src/SeriesApp.Api` | Run the API |
| `dotnet build SeriesApp.slnx` | Build the solution |
| `dotnet test SeriesApp.slnx` | Run the test suite |

The solution uses `SeriesApp.slnx`, the XML solution format that is the default in the
.NET 10 SDK. Open it with .NET SDK 10+, Visual Studio 2022 17.14+, or Rider 2025.1+.

### Backend layout

```
backend/
├── SeriesApp.slnx
├── Directory.Build.props          shared compiler settings
├── NuGet.config                   restore pinned to nuget.org
├── src/
│   ├── SeriesApp.Api/             host and composition root
│   └── SeriesApp.SharedKernel/    IModule contract shared by all modules
└── tests/
    └── SeriesApp.Api.Tests/       integration tests against the real host
```

The API is a **modular monolith**: one deployable host into which vertical feature modules
register themselves. See [docs/architecture.md](docs/architecture.md).

## Run everything together

Three terminals:

```bash
cd backend     && dotnet run --project src/SeriesApp.Api   # :5080
cd apps/web    && npm run dev                              # :3000
cd apps/mobile && npx expo start                           # :8081
```

## Current state

Deliberately absent, to be added as explicit decisions:

- **Database** — PostgreSQL is planned but not configured. No EF Core, migrations, or entities.
- **Authentication** — none.
- **Domain features** — no movie/series functionality. `/health` is the only endpoint.
- **Infrastructure** — no Docker, CI, or deployment configuration.
