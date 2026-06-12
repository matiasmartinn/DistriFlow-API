# Infrastructure

> Infrastructure implements persistence and technical integrations for DistriFlow.
> This guide extracts reusable EF Core patterns from VETE, but adapts them to
> DistriFlow's shared-schema multi-tenant model and documented domain boundaries.

Infrastructure depends on Domain only. It must not depend on Application, contain
business rules, map API DTOs, or normalize values already owned by the domain.

The persistence baseline is PostgreSQL through EF Core and Npgsql. Migrations live
in Infrastructure.

---

## 1. Intended Structure

Use the following structure unless an existing implementation establishes a more
specific golden path:

```text
DistriFlow.Infrastructure/
|-- DependencyInjection.cs
|-- Persistence/
|   |-- DistriFlowDbContext.cs
|   |-- Configurations/
|   `-- Migrations/
`-- Repositories/
```

- Keep one `IEntityTypeConfiguration<T>` per aggregate root, catalog entity, or
  independently mapped persistence type.
- Keep repository implementations in `Repositories/`.
- Keep EF Core types and provider-specific behavior out of Domain.
- Do not create a repository or `DbSet` for an entity that only exists inside an
  aggregate, such as `OrderLine` or `ProductPrice`.

---

## 2. DbContext

`DistriFlowDbContext` is the EF Core session and the implementation of the Domain
`IUnitOfWork`.

It should:

- expose `DbSet` properties for aggregate roots and read-only catalogs
- apply configurations from the Infrastructure assembly
- persist all changes for one application use case atomically
- remain scoped to one request or application operation
- contain persistence mechanics, not business decisions

Expected root sets:

- `Distributors`
- `Users`
- `Products`
- `PriceLists`
- `Customers`
- `Orders`
- `Stocks`
- `Provinces`
- `Cities`

Do not expose root sets for `OrderLine` or `ProductPrice`. They are loaded and
persisted through `Order` and `PriceList`.

Repositories add, update, and remove tracked entities. They do not call
`SaveChanges`. The Application layer completes the use case through `IUnitOfWork`.

One `SaveChanges` call is the default. EF Core already makes a single save atomic.
Add an explicit transaction only when a use case genuinely requires multiple saves
or another database operation in the same transaction.

Order confirmation and stock deduction must share the same scoped DbContext and
Unit of Work so the order and affected stock records commit or fail together.

---

## 3. Unit Of Work

The `IUnitOfWork` interface belongs to Domain. Infrastructure implements it through
`DistriFlowDbContext`.

Rules:

- Register `IUnitOfWork` by resolving the already scoped `DistriFlowDbContext`.
- Never register a second DbContext instance for the Unit of Work.
- Repositories never commit independently.
- Application services decide when the use case is ready to commit.
- Do not add a generic transaction manager until a real use case needs it.

Database constraint violations caused by a race may need translation into a stable
application failure. The translation policy is still pending; do not leak raw
PostgreSQL messages to the API.

---

## 4. Entity Configurations

Entity configurations are the single place for relational persistence rules:

- table and column names
- keys and strongly typed ID conversions
- required and optional fields
- string lengths and decimal precision
- indexes and unique constraints
- value object mappings
- aggregate-owned collections
- relationship delete behavior

Do not place EF Core attributes in Domain entities. Keep mappings explicit and
reviewable in Infrastructure.

### Tables And Columns

- Use plural, `snake_case` table names.
- Enable the EF Core snake-case naming convention globally.
- Add explicit column names when flattening a value object or when the default name
  would not express the persisted meaning clearly.
- Use PostgreSQL-compatible types selected by the provider. Add explicit types only
  when precision, size, or provider behavior requires them.
- Configure requiredness and maximum lengths from the domain's actual constraints.
  Do not invent stricter persistence validation.

### Strongly Typed IDs

Aggregate roots use strongly typed IDs backed by `Guid`. Catalogs use `int`.

For every strongly typed ID property:

- configure an explicit conversion between the ID and its underlying `Guid`
- mark domain-generated root IDs as never database-generated
- configure conversions for foreign/reference ID properties too
- handle nullable strongly typed IDs with a nullable conversion
- reconstruct through the domain-supported constructor or rehydration factory
- do not normalize or apply unrelated validation in the EF conversion

Keep conversions close to each entity configuration. Do not introduce a global,
reflection-based converter convention unless repeated mappings demonstrate a clear
need and the convention remains easy to inspect.

### Enums And Closed States

Persist stable closed sets such as `Role`, `OrderStatus`, and aggregate statuses as
strings unless a documented decision requires another representation.

String persistence keeps rows readable, but renaming a code value becomes a data
migration. Never rename a persisted value casually.

### Value Objects

Choose the simplest EF Core mapping that preserves the domain shape:

- Scalar wrappers such as `Sku` or an email value map through value converters.
- Multi-property value objects stored in the owner's row, such as `ContactInfo`,
  `Address`, `Money`, `Quantity`, or `SaleUnit`, map as complex/owned inline values.
- Aggregate-owned entity collections such as `OrderLine` and `ProductPrice` map to
  separate tables owned by their aggregate root.

For inline value objects:

- flatten columns with explicit meaningful names
- configure requiredness per the domain property, including an optional `Address`
- configure decimal precision explicitly for monetary and quantity values
- trust domain factories for validation and normalization
- do not create repositories or independent lifecycle operations

Use a complex property by default for an identityless required value stored inline.
Use owned mapping only when EF Core ownership behavior, optionality, indexing, or
collection mapping requires it. Do not mix both styles for the same value object
without a documented reason.

Do not add a currency column until `Money` actually contains currency in the domain.
Multicurrency is outside the v1 baseline.

### Aggregate-Owned Collections

`PriceList.ProductPrices` and `Order.OrderLines` belong to their roots.

- Map each collection to its own table.
- Use the root ID as the owner foreign key.
- Cascade delete is allowed from a root to its owned children.
- Load the collection when a command needs to enforce aggregate invariants.
- Do not modify owned rows directly from an application service or repository.
- Add uniqueness constraints only for documented invariants.

For `ProductPrice`, enforce at most one price for a product within a price list with
a unique constraint on the owner and product reference.

Stock movements are outside v1. Persist only the current stock balance.

### Cross-Aggregate References

Aggregates reference other aggregates by ID, not by object composition.

- Persist `DistributorId`, `CustomerId`, `ProductId`, `PriceListId`, and `UserId`
  as scalar strongly typed ID properties.
- Index reference IDs used by repository queries.
- Do not add navigation properties to Domain merely to satisfy EF Core.
- Do not configure cascade delete across aggregate roots.
- Prefer restrict/no-action behavior for cross-aggregate database foreign keys.

Whether every scalar cross-aggregate reference must also have a database foreign
key is pending. Until decided, do not introduce navigation-based object graphs.

---

## 5. Multi-Tenancy

`Distributor` is the tenant root. `User`, `Product`, `PriceList`, `Customer`,
`Order`, and `Stock` must contain a required `DistributorId`.

Tenant isolation is not optional:

- Every tenant-owned repository query must include the current `DistributorId`.
- A lookup by ID alone is not valid for a tenant-owned aggregate.
- Generic unscoped `GetById` and `GetAll` methods are allowed only for global
  catalogs such as `Province` and `City`.
- Every tenant-owned index intended for lookups should begin with or include
  `DistributorId`, according to the query shape.
- Unique business values that are tenant-scoped must use composite unique indexes.
- Database constraints complement repository scoping; they do not replace it.

Required uniqueness baseline:

| Aggregate | Constraint |
|---|---|
| `Distributor` | system-wide unique business name/email only where defined by Domain |
| `User` | system-wide unique `Email`; `DistributorId` still scopes tenant operations |
| `Product` | unique `(DistributorId, Sku)` when SKU is present |
| `ProductPrice` | unique `(PriceListId, ProductId)` |
| `Stock` | unique `(DistributorId, ProductId)` |

Do not make customer, price-list, or product names unique unless the domain
explicitly requires it.

The enforcement mechanism for tenant scoping, explicit repository predicates versus
global query filters, is pending. Whichever mechanism is selected must also cover
`Find`, existence checks, includes, paginated queries, and mutation lookups.

---

## 6. Repository Implementations

Repository interfaces belong to Domain. EF Core implementations belong to
Infrastructure.

Repositories may:

- query and persist aggregate roots
- expose simple domain-oriented lookup methods
- use `AsNoTracking` for read-only entity queries
- eagerly load aggregate-owned collections required by a command
- project efficient read models only when the repository contract explicitly calls
  for that shape

Repositories must not:

- contain business rules
- map API/Application DTOs
- normalize value objects
- call `SaveChanges`
- return tenant-owned data without tenant scoping
- expose internal aggregate entities for direct mutation

A small generic repository is acceptable only for genuinely shared mechanics.
Tenant-owned aggregates normally need explicit repository methods because an
unscoped generic lookup is unsafe.

Use tracked queries when the aggregate will be changed. Use no-tracking queries for
read-only flows. Avoid `Update` on a detached aggregate graph unless the use case
specifically requires it and the effect on owned collections is understood.

### Repository Query Baseline

- `User`: login email lookup is global; ID and tenant-management lookups are scoped by `DistributorId`.
- `Product`: scope catalog queries and SKU existence checks by `DistributorId`.
- `PriceList`: load product prices for price resolution and edits.
- `Customer`: scope by `DistributorId`; its optional `PriceListId` is a reference.
- `Order`: load lines for commands; support tenant-scoped history by customer.
- `Stock`: query by `(DistributorId, ProductId)` for order confirmation and
  inventory operations.
- `Province` and `City`: read-only, global catalogs; city queries filter by
  `ProvinceId`.

---

## 7. Infrastructure Dependency Injection

Expose one `AddInfrastructure` extension used by the API composition root.

It should:

- require the configured PostgreSQL connection string
- register `DistriFlowDbContext` as scoped
- configure Npgsql and the snake-case naming convention
- keep sensitive-data logging disabled outside explicitly controlled development
- register `IUnitOfWork` as the same scoped DbContext instance
- register each concrete repository against its Domain interface

Infrastructure's project reference remains limited to Domain. Do not reference
Application from Infrastructure.

Repository registration must be explicit. When adding a repository, verify its
Domain interface and DI registration in the same change.

---

## 8. Domain Persistence Baseline

The following is a mapping guide, not a final schema.

| Domain concept | Persistence guidance |
|---|---|
| `Distributor` (Distribuidora) | Root table; strongly typed ID; inline contact/address; system-level uniqueness only where documented |
| `User` (Usuario) | Required `DistributorId`; email unique system-wide; role/status as strings |
| `Product` (Producto) | Required `DistributorId`; inline base `Money`, `SaleUnit`, optional `Sku`; tenant-scoped SKU uniqueness |
| `PriceList` (ListaDePrecios) | Required `DistributorId`; owned `ProductPrice` rows; one row per product per list |
| `Customer` (Cliente) | Required `DistributorId`; inline contact/address; optional strongly typed `PriceListId` |
| `Order` (Pedido) | Required tenant/customer/sales-rep IDs; status as string; owned `OrderLine` rows with description and price snapshots |
| `Stock` | Required tenant/product IDs; one stock root per product; current balance only; concurrency policy pending |
| `Province` / `City` | Global read-only catalogs using `int`; city belongs to province |

Persist order snapshots as values on `OrderLine`. Never rebuild historical order
prices or descriptions from the current `Product` or `PriceList`.

---

## 9. EF Core And Migration Conventions

- Use Npgsql and the project-compatible EF Core version.
- Use async database APIs and pass `CancellationToken`.
- Keep migrations in Infrastructure.
- Generate migrations from configured mappings; do not hand-author a schema first.
- Seed only stable catalogs such as provinces/cities when required.
- Do not seed tenant business data in production migrations.
- Review every migration before applying it.

Migration review must verify:

- all tenant-owned root tables contain required `distributor_id`
- tenant-scoped unique constraints include `distributor_id`
- strongly typed IDs persist as the intended primitive type
- owned child tables have the intended owner key and cascade behavior
- cross-aggregate deletes do not cascade
- money and quantity columns have explicit suitable precision
- no accidental shadow foreign keys or unexpected join tables were created
- no migration adds a feature outside the documented baseline

---

## 10. Reusable Patterns Observed In VETE

The following VETE patterns are reusable:

- a DbContext in Infrastructure implementing the Domain Unit of Work
- one configuration class per mapped root/type, discovered from the assembly
- explicit strongly typed ID conversions and domain-generated root IDs
- string persistence for closed enum/state values
- inline mappings for value objects and separate owned tables for child collections
- small repositories with no commit responsibility
- `AsNoTracking` for read-only repository queries
- one Infrastructure DI extension registering Npgsql, DbContext, Unit of Work, and
  repositories
- global snake-case naming with explicit names where flattened values need clarity
- migrations colocated with Infrastructure

The following VETE details must not be copied as DistriFlow rules:

- unscoped generic repository lookups for tenant-owned aggregates
- global unique indexes for values that are unique only inside a tenant
- incomplete tenant-specific repository methods
- partial DI registration that leaves existing repositories unreachable
- inconsistent choice between complex and owned mappings without an explicit reason
- audit shadow properties without a complete population and ownership policy
- an explicit transaction around every single `SaveChanges` by default
- provider exception translation without an agreed application error policy

---

## 11. Pending Infrastructure Decisions

These decisions must be agreed before their first implementation:

1. **Tenant enforcement:** explicit repository predicates, EF Core global query
   filters, or a documented combination.
2. **Tenant context dependency:** where the tenant-context contract lives so
   Infrastructure can use it without depending on Application.
3. **Cross-aggregate foreign keys:** which scalar ID references receive database
   foreign keys while Domain remains navigation-free.
4. **Stock concurrency:** optimistic concurrency token, atomic conditional update,
   row locking, or another strategy to prevent concurrent confirmations from making
   stock negative.
5. **Money and quantity precision:** PostgreSQL precision/scale for decimal values.
6. **Audit fields:** whether v1 records created/updated timestamps and actor IDs,
   and how they are populated.
7. **Database error translation:** how unique, foreign-key, and concurrency
   violations become stable `Result` failures with Spanish client messages.
8. **Delete policy:** whether roots are physically deleted at all or only change
   documented statuses. Do not introduce soft-delete query filters by assumption.
