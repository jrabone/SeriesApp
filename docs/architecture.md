# Architecture

## Shape

SeriesApp is one product with three deployables: a Next.js web client, an Expo mobile
client, and a single ASP.NET Core API. The API is a **modular monolith**.

```
apps/web ────┐
             ├──►  SeriesApp.Api  ──►  [ module ] [ module ] [ module ]
apps/mobile ─┘      (one host)              └──────────┬──────────┘
                                             SeriesApp.SharedKernel
```

## Why a modular monolith

Microservices buy independent deployment and scaling, and charge for it in network failure
modes, distributed transactions, operational surface, and cross-service refactoring cost.
At this stage SeriesApp has one team, no established domain boundaries, and no
differential scaling pressure — so the price is real and the benefit is not.

A modular monolith keeps the thing that actually matters long term — enforced internal
boundaries — while deploying as a single unit. If a module later needs separate scaling or
ownership, a clean boundary is what makes extracting it tractable. The boundaries are the
point; the deployment topology is a consequence.

## Module boundaries

A module is a **vertical slice of business capability** — not a technical layer. It owns
its endpoints, services, and (once persistence lands) its own tables. Split by what the
feature *is*, never into `Controllers` / `Services` / `Repositories` projects.

The rules:

1. A module implements `IModule` (`src/SeriesApp.SharedKernel/IModule.cs`), which exposes
   `RegisterServices` and `MapEndpoints`.
2. It is activated by being added to `ModuleRegistry.Modules` in
   `src/SeriesApp.Api/ModuleRegistry.cs`. The list is explicit rather than
   assembly-scanned, so the active set is compile-time checked and readable at a glance.
3. **Modules do not reference one another.** Anything crossing a boundary is a contract in
   `SeriesApp.SharedKernel`. Two modules that need each other's internals are a sign the
   boundary is drawn in the wrong place.
4. `SeriesApp.Api` is a composition root: host configuration, cross-cutting middleware,
   module registration. No business logic.

### Adding a module

1. Create `src/SeriesApp.Modules.<Capability>/` as a class library referencing
   `SeriesApp.SharedKernel`.
2. Implement `IModule`.
3. Reference it from `SeriesApp.Api` and add it to `ModuleRegistry.Modules`.

## Clients

`apps/web` and `apps/mobile` are independent npm projects — no workspace, separate
lockfiles, and intentionally different React versions (Expo pins its own). This avoids
Metro's monorepo resolution complexity while nothing is actually shared.

When genuine shared code appears (API client, domain types), the move is to introduce npm
workspaces with a `packages/` directory and configure Metro's `watchFolders` and
`nodeModulesPaths` at that point — not pre-emptively.

## Deferred decisions

These are open, not forgotten:

- **Persistence.** PostgreSQL is intended. Unresolved: EF Core vs. Dapper, and whether
  modules share one database with a schema per module (the usual modular-monolith default)
  or get separate databases.
- **Authentication.** No provider or token strategy chosen.
- **API contract sharing.** Whether clients consume a generated client from the OpenAPI
  document or a hand-written one.
- **Deployment.** No container or hosting target chosen.

Record each as an ADR in this directory when decided.
