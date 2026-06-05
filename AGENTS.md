# AGENTS.md

DistriFlow is a multi-tenant SaaS for order-taking, built for small distributors and their field sales reps. This file is the entry point for any agent working on the codebase.

## Domain Source

Before implementing business features, read:

- docs/domain/Domain - Distribuidoras-Preventistas.md

This document is conceptual domain documentation (written in Spanish — the business ubiquitous language).
It does not define database schema, endpoints, EF Core mappings or folder structure.
Do not invent features outside the documented baseline.

## Product Context

For business scope, priorities and the meaning of domain terms, read the product docs:

- docs/product/README.md — index and recommended reading order
- docs/product/glossary.md — Spanish domain term ↔ English code name mapping (read this first to avoid VETE/legacy naming)
- docs/product/mvp.md — what is in scope for v1
- docs/product/open-decisions.md — decisions still open; do not resolve them unilaterally

Product docs are written in Spanish. Code is written in English (see Language Conventions).

# Project Rules

This project follows strict implementation and folder conventions.

Do not invent new architecture or patterns.

Always search existing implementations before generating new code.

Consistency is more important than optimization.

---

# Language Conventions

Code is written in **English**: classes, entities, value objects, namespaces, properties, methods, routes.

API error and validation messages returned to the client are written in **Spanish**, because they may be shown to the end user.

Domain and product documentation stays in **Spanish** (business ubiquitous language). Architecture documentation stays in **English**.

When a Spanish domain term appears in docs, map it to its English code name using docs/product/glossary.md (e.g. Pedido → Order, Cliente → Customer, ListaDePrecios → PriceList).

---

# Required Reading

Before implementing backend features, read:

- docs/architecture/technical-foundations.md
- docs/architecture/application-services.md
- docs/architecture/repositories.md
- docs/architecture/mappers.md
- docs/architecture/controllers.md
- docs/architecture/code-style.md
- docs/architecture/catalog-module.md
- docs/architecture/technical-decisions.md

If the feature uses command/query separation, also read:

- docs/architecture/cqrs-style.md

---

# Existing Patterns First

Before creating new code:

1. Search similar aggregate/module
2. Replicate existing structure
3. Reuse naming conventions
4. Keep implementation style consistent

Golden paths always have priority over new ideas.

---

# Architecture Rules

Application:

- orchestrates use cases
- maps DTOs when DTOs are required
- coordinates repositories through Domain repository interfaces
- contains application services and service interfaces

Domain:

- owns business rules and invariants
- contains entities, value objects and domain repository interfaces
- does not depend on Application or Infrastructure

Infrastructure:

- persists data
- contains EF Core implementations
- implements repository interfaces defined in Domain
- must not depend on Application

API:

- receives requests
- returns responses
- calls Application services

Do not move responsibilities between layers.

---

# Forbidden Dependencies

Do not introduce:

- AutoMapper
- MediatR
- FluentValidation
- specification pattern
- generic service abstractions
- unnecessary helper layers

Unless explicitly requested.

---

# Validation Rules

Validation must not be duplicated across layers.

If a Value Object already validates or normalizes data:

- do not repeat validation
- do not normalize again
- do not trim/lowercase again

Trust the domain model.

---

# Code Style

Prefer:

- explicit code
- direct flow
- small methods
- readability
- minimal abstractions

Avoid:

- defensive programming excess
- abstraction chains
- enterprise boilerplate
- hypothetical future abstractions

---

# Scope Rules

Keep minimal diff.

Do not:

- refactor unrelated files
- rename files unnecessarily
- introduce broad architectural changes

---

# Before Finishing

Always:

- remove unused usings
- keep implementation minimal
- avoid dead code
- build project if possible
