# Application Services

## Purpose

Application services orchestrate use cases.

Application may:

- query repositories through Domain repository interfaces
- create aggregates
- call domain methods
- map responses when DTOs are required
- return results

Application must not:

- contain repository interfaces
- duplicate domain rules
- normalize entities already normalized by Value Objects
- contain infrastructure logic
- create unnecessary helper layers

Repository interfaces belong to Domain.

---

## Aggregate Structure

Complex aggregates may use lightweight command/query separation.

Example:

Orders/
├── Commands/
├── Queries/
├── DTOs/
├── Mappers/
├── IOrderCommandService.cs
└── IOrderQueryService.cs

Use command/query separation only when it improves clarity.

Do not use this structure for simple catalogs.

---

## Catalog Structure

Catalog modules intentionally use a simplified architecture.

Example:

Cities/
├── CityService.cs
└── ICityService.cs

If DTOs are required:

Cities/
├── DTOs/
├── Mappers/
├── CityService.cs
└── ICityService.cs

Catalogs:

- are mostly static
- may be seeded from CSV/API
- contain minimal business logic

Do not apply full CQRS structure to catalogs.

Avoid unnecessary commands/queries separation.

---

## Application Interfaces

Application may contain service interfaces.

Examples:

- ICityService
- IOrderQueryService
- IOrderCommandService

Application must not contain repository interfaces.

Repository interfaces belong to Domain.

---

## Commands

Command services mutate state.

Examples:

- CreateOrder
- ConfirmOrder
- CancelOrder

Commands should not contain complex query logic.

---

## Queries

Query services are read-only.

Query services may contain:

- filtering
- pagination
- projections
- lookup operations

Queries must not mutate state.

---

## DTO Conventions

DTOs are not required for every catalog.

Simple read-only catalogs may return simple domain entities directly when:

- the entity shape is stable
- there are no sensitive/internal fields
- the API response matches the domain shape
- the existing golden path does the same

Use DTOs when:

- the API shape differs from the domain entity
- the entity has internal fields that should not be exposed
- the response combines data from multiple sources
- the response is a projection

---

## Output DTOs

Output DTOs use positional records.

Example:

public sealed record ProductDto(
Guid Id,
string Name,
decimal BasePrice,
bool Active
);

Nullable values must use explicit nullable syntax.

Examples:

string?
Guid?
DateOnly?

Avoid:

- mutable output DTOs
- class-based response DTOs
- unnecessary setters

---

## Input DTOs

Input DTOs use init-only properties.

Example:

public sealed record CreateProductDto
{
[Required]
public string Name { get; init; } = null!;

    public string? Sku { get; init; }

}

Required fields:

- use [Required]
- remain non-nullable

Optional fields:

- use nullable syntax
- do not use [Required]

Avoid positional records for input DTOs.

Do not use the null-forgiving operator (!) unless required for framework compatibility.
