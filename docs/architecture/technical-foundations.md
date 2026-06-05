# Technical Foundations (EPIC-0)

> Cross-cutting technical decisions that every module inherits. Settle these before building
> business verticals. Business-scope decisions live in `technical-decisions.md`; these are
> implementation foundations. Code is English; API error messages are Spanish (see `AGENTS.md`).

---

## 1. Persistence — PostgreSQL + EF Core

- Provider: **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL`.
- The `DbContext` lives in **Infrastructure**. EF Core migrations live in Infrastructure too.
- Connection string comes from configuration: `appsettings.json` for defaults, environment variables
  or user-secrets for local/real credentials. Never hardcode credentials.
- Repository interfaces stay in **Domain**; EF Core implementations in **Infrastructure** (see `repositories.md`).
- One database per deployment; tenants share the schema and are isolated by `DistributorId` (row-level scoping).

## 2. Identity & multi-tenancy — minimal JWT

Authentication is not a **domain** concern (the domain doc keeps it out of baseline), but the API still
needs to know *who* is calling and *which tenant* they belong to. The baseline mechanism:

- Requests carry a **JWT** with claims: `userId`, `distributorId`, `role` (`Administrator` | `SalesRep`).
- JWT authentication populates the request principal and a scoped **`ICurrentUserContext`** reads the
  current `DistributorId`, `UserId`, and `Role` for the Application layer.
- Tenant-owned Application services obtain `DistributorId` from `ICurrentUserContext` and pass it to
  repository queries. Tenant-owned request bodies must not provide it (RNF-01). Public login may receive
  it only to disambiguate an email that is unique inside a distributor.
- Role-based authorization gates endpoints: `SalesRep` takes orders; `Administrator` manages catalog,
  prices, stock and users.
- **Token issuance (login)** is a thin Infrastructure/Api concern. What stays OUT of baseline (per domain):
  user invitations, password reset, email verification, refresh-token rotation.

## 3. Identifiers — strongly-typed IDs

- Aggregate roots use **strongly-typed IDs**: `readonly record struct` wrapping a `Guid`
  (e.g. `OrderId`, `CustomerId`, `ProductId`, `DistributorId`, `UserId`, `PriceListId`, `StockId`).
- `.Value` exposes the underlying `Guid` (matches the existing mapper examples: `order.Id.Value`).
- **Catalogs use `int`** (`Province`, `City`) — they are seeded, static lookup tables.
- EF Core persists strongly-typed IDs via **value converters** configured in the entity mappings.
- IDs are generated **in the domain** (new `Guid`) when the aggregate is created. Note: how IDs are
  generated when offline is reintroduced is open decision O-04 (`product/open-decisions.md`); do not
  design for offline now, but do not block it either.

## 4. Results & error handling

- **No validation/result libraries** (FluentValidation, MediatR, etc. are forbidden — see `AGENTS.md`).
- Application services return a lightweight **`Result`** / **`Result<T>`** (project-owned, no dependency).
- A failure carries a stable error **code** + a **Spanish message** meant for the end user (RNF-02).
- Expected failures (not found, invalid state, validation) are returned as `Result` failures, not thrown.
- Domain invariants are guarded inside the aggregate; a true invariant breach may throw a domain
  exception as a last line of defense, but the Application layer is responsible for returning clean
  `Result` failures for the cases it can anticipate.
- `BaseController.HandleResult` maps results to HTTP: success → 200/201; failure → 400/404/409 using
  `ProblemDetails`, with the Spanish message in the response body (see `controllers.md`).

## 5. Testing

- **No test project in v1.** Verification gate is a successful `dotnet build`.
- This is a deliberate baseline choice to reduce initial scaffolding; revisit when the Order/Stock
  rules stabilize. When added, prioritize Domain invariant tests first.

## 6. Solution layout & dependency rule

- Layers: `Domain` (no deps) ← `Application` (→ Domain) ← `Infrastructure` (→ Domain) and
  `Api` (→ Application, → Infrastructure). Dependencies only point toward Domain.
- `Infrastructure` must **not** reference `Application` (enforced: its `.csproj` references only Domain).
- Each module mirrors the structure in `application-services.md` / `repositories.md`. Reuse the
  existing golden path before inventing a new one.
