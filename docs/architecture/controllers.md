# Controller Rules

Controllers stay thin.

Controllers:

- receive request
- call application service
- return response

Controllers must not:

- contain business logic
- access DbContext directly
- orchestrate workflows
- map entities manually

Keep controllers minimal.

---

## Naming

Controllers use plural names.

Examples:

- ProvincesController
- CitiesController
- ProductsController
- CustomersController
- OrdersController

Routes should represent collections.

Examples:

- GET /api/provinces
- GET /api/provinces/{id:int}
- GET /api/cities/by-province/{provinceId:int}
- GET /api/customers/{id:int}/orders
- POST /api/orders

---

## Route Rules

Use relative route templates.

Correct:

[HttpGet("{id:int}")]

Incorrect:

[HttpGet("/{id:int}")]

Do not start action routes with /.

Use BaseController and HandleResult when returning application results.
