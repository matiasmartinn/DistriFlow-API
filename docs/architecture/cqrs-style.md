# CQRS Style

This project uses lightweight CQRS separation only when useful.

The goal is organizational clarity.

This is not full academic CQRS.

Do not introduce:

- MediatR
- IRequest
- IRequestHandler
- pipeline behaviors
- notification systems

---

## Structure

Complex modules may use:

Commands/
Queries/

Only two clear separations:

- writes
- reads

---

## Commands

Commands mutate state.

Examples:

- CreateOrder
- AddOrderLine
- ConfirmOrder
- CancelOrder

---

## Queries

Queries read data only.

Examples:

- GetOrderById
- GetOrdersByCustomer
- GetPagedOrders

Queries must not mutate state.

---

## Catalogs

Do not use CQRS for simple catalog modules.

Catalogs should use a simple service unless explicitly requested otherwise.
