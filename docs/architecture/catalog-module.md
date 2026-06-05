# Catalog Module

Catalog modules intentionally use a simplified architecture.

Catalogs are usually:

- mostly static
- loaded from seed data, CSV files or external APIs
- simple to query
- low in business logic

Do not apply full CQRS structure to catalogs.

Do not implement create, update or delete unless explicitly requested.

Use only the endpoints required by the application.

---

## What is a catalog in DistriFlow

In DistriFlow, catalogs are the small, mostly static reference tables that support the
business aggregates. Today the only catalogs are **geographic** and exist to normalize
the `Address` value object used by `Distributor` and `Customer`:

- **Provinces**
- **Cities** (a city belongs to a province)

Everything else in the domain is a business aggregate or a value object, NOT a catalog.
Do not turn the following into catalog modules:

- `Product`, `PriceList`, `Customer`, `Order`, `Stock` — these are aggregates with business rules.
- `SaleUnit`, `OrderStatus`, `Role`, `Money`, `Quantity` — these are value objects, modeled
  inside their aggregates, not exposed as catalog endpoints.

If a future requirement needs a real lookup table (for example a configurable `SaleUnit`
catalog), add it here only when explicitly requested.

---

## Catalog API Rules

Read-only catalogs should expose only the endpoints needed by the use case.

Do not expose endpoints just because the generic repository supports them.

Example:

If the generic repository has GetOne, that does not automatically mean the catalog should expose GET /api/{resource}/{id}.

---

## Current Catalog Endpoints

### Provinces

Required endpoints:

- GET /api/provinces
- GET /api/provinces/{id:int}

Do not implement create, update or delete unless explicitly requested.

---

### Cities

Cities are consumed through province filtering.

Required endpoint:

- GET /api/cities/by-province/{provinceId:int}

Do not expose unless explicitly requested:

- GET /api/cities
- GET /api/cities/{id:int}
- create
- update
- delete

---

## DTO Rules for Catalogs

Catalog DTOs are optional.

Use DTOs only when:

- the response shape differs from the entity
- the entity exposes fields that should not be returned
- the response combines data from multiple sources
- the existing golden path uses DTOs

For simple catalogs with only fields like Id and Name, returning the domain entity is acceptable if it matches the existing project pattern.
