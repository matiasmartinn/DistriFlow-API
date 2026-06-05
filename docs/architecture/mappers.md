# Mapper Rules

Mappings are manual.

Do not use:

- AutoMapper
- mapping frameworks
- generic mapping abstractions

---

## Structure

Mappings belong to Application layer.

Infrastructure must not map DTOs.

Each module that requires DTO mapping contains its own mapper file.

Example:

Orders/
└── Mappers/
└── OrderMapper.cs

Catalogs do not need mappers if they do not use DTOs.

---

## Mapper Style

Correct approach:

internal static class OrderMapper
{
internal static OrderDto ToDto(this Order order)
{
return new OrderDto(
order.Id.Value,
order.CustomerId.Value,
order.Status,
order.Total.Amount);
}
}

---

## Rules

Mappings should be:

- explicit
- readable
- internal
- static

Complex mappings must receive dependencies explicitly.

Avoid hidden magic.
