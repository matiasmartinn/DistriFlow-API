# API MVP Feature Contracts for Frontend

This document summarizes the backend features implemented from `docs/domain/08-features-api-mvp.md` so a frontend agent can align screens, DTOs, calls and UX flows.

It is intentionally contract-focused. It does not redefine domain rules.

## Global Rules

- Tenant-owned requests must not send `distributorId`; backend reads it from JWT claims.
- Protected calls require `Authorization: Bearer <token>`.
- Critical mutation retries use `Idempotency-Key`.
- Errors are returned as `ProblemDetails` with `extensions.code`.
- Administrator manages catalog, prices, stock, configuration and users.
- SalesRep operates orders and can read allowed operational data.

## Auth and Session

Endpoints:

- `POST /api/distributors`: public onboarding. Creates Distributor + initial Administrator.
- `POST /api/auth/login`: authenticates existing user by globally unique user email.
- `GET /api/auth/me`: current session summary.

Frontend impact:

- Login must store JWT and derive current tenant/user context from `/api/auth/me`.
- Do not ask for distributor code/id at login.
- Distributor email is contact/informational, not login identity.

Suggested UI:

- Login screen.
- Onboarding screen.
- Current tenant/user display in app shell.

## Distributor Configuration

Endpoints:

- `GET /api/distributorconfigurations/current`
- `PUT /api/distributorconfigurations/current`

Important fields:

- `stockPolicy`: `RequireAvailableStock`, `AllowNegativeStock`
- `priceFallbackPolicy`: `UseLastValidPrice`, `RejectOrder`
- `orderNumberPrefix`
- `nextOrderNumber`
- `timeZone`

Frontend impact:

- Configuration affects order line pricing and stock behavior.
- If `RejectOrder`, adding an order line without a valid price returns `order.price_not_available`.

Suggested UI:

- Settings page for operational configuration.
- Confirmation modal before changing `stockPolicy` or `priceFallbackPolicy`.

## Users

Endpoints:

- `GET /api/users?role=&status=&searchText=&page=&pageSize=`
- `GET /api/users/{id}`
- `POST /api/users`
- `PATCH /api/users/{id}/role`
- `PATCH /api/users/{id}/deactivate`
- `PATCH /api/users/{id}/reactivate`

Important fields:

- `role`: `Administrator`, `SalesRep`
- `status`: `Active`, `Inactive`
- `createdByUserId`
- `lastLoginAtUtc`
- `deactivatedAtUtc`
- `deactivatedByUserId`
- `deactivationReason`

Rules:

- Administrator only.
- Cannot leave tenant without an active Administrator.
- Deactivation requires `reason`.

Suggested UI:

- User administration list with filters.
- User detail drawer/page.
- Create user modal.
- Change role confirmation modal.
- Deactivate modal requiring reason.
- Reactivate action.

## Products and Stock

Product endpoints:

- `POST /api/products`
- `GET /api/products`
- `GET /api/products/with-stock?status=&searchText=&page=&pageSize=`
- `GET /api/products/{id}`
- `PUT /api/products/{id}`
- `PATCH /api/products/{id}/discontinue`
- `PATCH /api/products/{id}/reactivate`

Stock endpoints:

- Stock entry/adjustment endpoints require `Idempotency-Key`.
- Stock movement history is available through stock movement list endpoints.

Rules:

- Product requests never include `distributorId`.
- Discontinued products cannot be used in new order lines.
- Stock decreases on order confirmation in the same transaction.
- Stock movements are ledgered.

Suggested UI:

- Product list with stock consolidated via `/api/products/with-stock`.
- Product create/edit modal.
- Discontinue/reactivate confirmation.
- Stock adjustment modal with required reason/reference.
- Stock movement history panel or tab.

## Customers

Endpoints:

- `POST /api/customers`
- `GET /api/customers?includeInactive=&status=&taxCondition=&zone=`
- `GET /api/customers/{id}`
- `PUT /api/customers/{id}/contact`
- `PUT /api/customers/{id}/commercial-info`
- `PUT /api/customers/{id}/price-list`
- `DELETE /api/customers/{id}/price-list`
- `PATCH /api/customers/{id}/deactivate`
- `PATCH /api/customers/{id}/reactivate`
- `GET /api/customers/{id}/products/{productId}/price`
- `GET /api/customers/{id}/orders`

Create request additions:

```json
{
  "name": "Cliente Norte",
  "email": "cliente@example.com",
  "phone": "1122334455",
  "address": null,
  "priceListId": null,
  "zone": "Norte",
  "taxCondition": "Monotributo"
}
```

Controlled `taxCondition` values:

- `FinalConsumer`
- `Monotributo`
- `RegisteredResponsible`
- `Exempt`

Response additions:

- `zone`
- `taxCondition`
- `deactivatedAtUtc`
- `deactivatedByUserId`
- `deactivationReason`

Rules:

- Invalid tax condition returns validation/business error.
- Deactivation requires `reason`.
- Inactive customers cannot be used in new orders.
- SalesRep list is forced to active customers even if query asks inactive.

Suggested UI:

- Customer list filters: status, zone, tax condition.
- Customer create/edit form should include commercial section.
- Separate "commercial info" edit modal or tab.
- Deactivate customer modal requiring reason.
- Customer detail page should show deactivation metadata.

Decision needed before frontend:

- Whether `zone` is free text, select with suggestions, or locally managed options. Backend currently stores normalized text only.

## Price Lists

Endpoints:

- `POST /api/pricelists`: creates Draft version 1.
- `GET /api/pricelists?includeInactive=`
- `GET /api/pricelists/{id}`
- `GET /api/pricelists/{id}/versions`
- `POST /api/pricelists/{id}/versions`: clones a new Draft version.
- `PUT /api/pricelists/{id}/name`
- `PUT /api/pricelists/{id}/products/{productId}`
- `DELETE /api/pricelists/{id}/products/{productId}`
- `PATCH /api/pricelists/{id}/activate`: requires `Idempotency-Key`.
- `PATCH /api/pricelists/{id}/deactivate`

Important fields:

- `id`: concrete version id.
- `seriesId`: logical reusable list id assigned to customers.
- `status`: `Draft`, `Active`, `Inactive`
- `version`
- `validFromUtc`
- `validUntilUtc`
- `activatedAtUtc`
- `deactivatedAtUtc`
- `rowVersion`
- `productPrices`

Rules:

- Only Draft versions can be edited.
- Active versions cannot be edited.
- Overlapping active versions in the same `seriesId` return `409`.
- Customer assignment should use `seriesId`; backend resolves active effective version.

Suggested UI:

- Price list page grouped by logical list (`seriesId`).
- Version history view.
- Draft editor for product prices.
- "Create new version" action from existing version.
- Activate version modal requiring validity dates.
- Deactivate version confirmation.

Decision needed before frontend:

- How to present `seriesId` vs version `id`: user should see list name + version, not raw ids.
- Whether activation uses date/time picker or date-only UI converted to UTC.

## Orders

Endpoints:

- `POST /api/orders`: SalesRep only, requires `Idempotency-Key`.
- `GET /api/orders?status=&customerId=&salesRepId=&dateFrom=&dateTo=&page=&pageSize=`
- `GET /api/orders/{id}`
- `POST /api/orders/{id}/lines`
- `PUT /api/orders/{id}/lines/{productId}`
- `DELETE /api/orders/{id}/lines/{productId}?rowVersion=`
- `PATCH /api/orders/{id}/confirm`: requires `Idempotency-Key`.
- `PATCH /api/orders/{id}/cancel`: requires `Idempotency-Key`.

Lifecycle fields:

- `status`: `Draft`, `Confirmed`, `Cancelled`
- `createdAtUtc`, `createdByUserId`
- `confirmedAtUtc`, `confirmedByUserId`
- `cancelledAtUtc`, `cancelledByUserId`
- `cancellationReason`
- `rowVersion`

Order line price traceability:

- `priceListId`
- `priceListVersion`
- `sourceUnitPrice`
- `appliedUnitPrice`
- `priceOrigin`: `PriceList`, `LastValidPrice`
- `subtotal`

Rules:

- Draft can be edited.
- Confirmed cannot be edited.
- Confirming discounts stock transactionally and writes stock movements.
- Cancelling a confirmed order reverses stock using compensating movements.
- Cancel requires reason.
- Row version must be sent for draft line edits and transitions.
- Historical line prices do not change when price lists change later.

Suggested UI:

- Order list with status/date/customer/salesRep filters.
- Draft recovery list for SalesRep.
- Order detail page with lifecycle metadata.
- Order creation flow with customer selector and line editor.
- Confirm modal showing stock/total impact.
- Cancel modal requiring reason.
- Conflict handling UI for `409`: refresh order and retry.
- Line price traceability could be shown in detail/tooltip, not necessarily in the main line editor.

Decision needed before frontend:

- How visible price traceability should be for ordinary SalesRep users.
- Whether rowVersion conflicts show a blocking modal or inline refresh prompt.

## Idempotency and Concurrency

Operations requiring `Idempotency-Key`:

- Start order.
- Confirm order.
- Cancel order.
- Stock increases/adjustments.
- Activate price list version.

Frontend behavior:

- Generate a stable key per user action attempt, not per retry.
- Reuse the same key if retrying after network timeout.
- Generate a new key for a new intentional operation.

Common conflict handling:

- `409` can mean stale rowVersion, reused idempotency key with different operation/resource, overlapping price version, insufficient stock, or stock reversal unavailable.
- Show a clear refresh/retry path.

## New or Changed UI Surface Checklist

Likely new pages:

- Price list version management.
- Stock movement history if not already planned.
- Distributor operational settings.

Likely new modals:

- Activate price list version with validity dates.
- Create new price list version.
- Deactivate customer with reason.
- Deactivate user with reason.
- Cancel order with reason.
- Stock adjustment with reason/reference.
- Confirm order.

Likely changed existing forms:

- Customer create/edit: add `zone` and `taxCondition`.
- Customer list: add filters for status, zone and tax condition.
- Order detail: show price traceability fields.
- User list/detail: show audit/login/deactivation metadata.
- Product list: prefer `with-stock` endpoint for stock-aware views.

Questions to define before frontend implementation:

- Is customer `zone` free text, a select fed from existing customers, or a future catalog?
- Should tax condition labels be localized in the frontend while values remain English enum strings?
- Should price list activation use date-only business dates or exact UTC timestamps?
- Should SalesRep see price traceability or only Administrators?
- Should deactivation metadata be visible in lists or only detail pages?
