# Repository Rules

Repository interfaces belong to Domain.

Repository implementations belong to Infrastructure.

Application consumes repository interfaces from Domain.

Infrastructure implements repository interfaces from Domain.

---

# Location

Domain:

Domain/
└── Cities/
├── City.cs
└── ICityRepository.cs

Infrastructure:

Infrastructure/
└── Repositories/
└── CityRepository.cs

---

# Repository Responsibilities

Repositories persist and query data.

Repositories may:

- query entities
- persist entities
- expose simple lookup methods
- use EF Core inside Infrastructure implementations

Repositories must not:

- contain business rules
- map DTOs
- normalize domain values
- orchestrate workflows
- depend on Application

---

# Generic Repository

Simple generic repositories are allowed for shared persistence operations.

Examples:

- GetById
- GetAll
- Add
- Update
- Remove
- Exists

Avoid:

- complex generic abstractions
- specification-heavy repositories
- generic service/repository chains

---

# Repository Style

Correct approach:

public interface ICityRepository
{
Task<City?> GetByIdAsync(int id, CancellationToken ct = default);
Task<IReadOnlyList<City>> GetByProvinceIdAsync(int provinceId, CancellationToken ct = default);
}

public sealed class CityRepository
: Repository<City, int>,
ICityRepository
{
}
