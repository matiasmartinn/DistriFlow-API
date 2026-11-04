# DistriFlow API

DistriFlow API is the backend for a multi-tenant order-taking system for small distributors and their sales representatives.

## Objective

Provide the core API needed for a distributor to register, authenticate, manage its catalog and users, control stock, and operate orders while preserving tenant isolation and inventory integrity.

## Main Features

- Distributor onboarding with an initial administrator.
- JWT authentication with user, distributor and role claims.
- Tenant isolation by authenticated distributor.
- User administration with roles `Administrator` and `SalesRep`.
- Customer, product, price list, stock and order management.
- Draft, confirmation and cancellation flows for orders.
- Stock discount and replenishment according to the configured stock policy.
- EF Core migrations for PostgreSQL.

## Architecture

The solution follows a layered architecture:

- `DistriFlow.Api`: HTTP controllers, authentication, authorization, middleware and API startup.
- `DistriFlow.Application`: use cases, DTOs, service interfaces, command/query orchestration and mappers.
- `DistriFlow.Domain`: entities, value objects, domain rules and repository contracts.
- `DistriFlow.Infrastructure`: EF Core persistence, migrations and repository implementations.

## Tech Stack

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Bearer authentication
- BCrypt password hashing

## Repository Structure

```text
src/
  DistriFlow.Api/
  DistriFlow.Application/
  DistriFlow.Domain/
  DistriFlow.Infrastructure/
DistriFlow-API.sln
appsettings.Example.json
```

## Requirements

- .NET SDK 9
- PostgreSQL
- EF Core tools, if you want to run migrations from the CLI:

```bash
dotnet tool install --global dotnet-ef
```

## Local Configuration

Create a local `src/DistriFlow.Api/appsettings.Development.json` using `appsettings.Example.json` as reference.

Required configuration:

- `ConnectionStrings:DistriFlowDb`: PostgreSQL connection string.
- `Jwt:Issuer`: JWT issuer.
- `Jwt:Audience`: JWT audience.
- `Jwt:Secret`: local signing secret. Use a long value and do not commit it.
- `Jwt:ExpirationMinutes`: token lifetime.
- `Cors:AllowedOrigins`: allowed frontend origins, for example `http://localhost:5173`.

## Commands

Restore and build:

```bash
dotnet restore
dotnet build
```

Run the API:

```bash
dotnet run --project src/DistriFlow.Api
```

Apply database migrations:

```bash
dotnet ef database update --project src/DistriFlow.Infrastructure --startup-project src/DistriFlow.Api
```

## Verification

```bash
dotnet run --project verification/DistriFlow.Domain.Checks
node scripts/verify-stage-three.mjs
```

The first command runs Domain invariants without external test packages. The second requires a
local API and PostgreSQL and creates synthetic data; it defaults to `http://localhost:5116/api`.
Set `STAGE3_API_URL` for another local instance. See [validation](docs/validation/local-validation.md).
CI builds the API, checks Domain rules and runs integration against ephemeral PostgreSQL.
The movement ledger is internal inventory infrastructure; a stock-history UI is outside the current MVP.

## Related Repository

The frontend lives in the separate `DistriFlow-WEB` repository. It consumes this API through the configured base URL.
