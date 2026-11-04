# Operational realtime updates

## Files added or modified for this change

- `.gitignore`
- `src/DistriFlow.Api/Program.cs`
- `src/DistriFlow.Api/Realtime/OperationsHub.cs`
- `src/DistriFlow.Api/Realtime/SignalROperationalNotifier.cs`
- `src/DistriFlow.Application/Common/Notifications/IOperationalNotifier.cs`
- `src/DistriFlow.Application/Orders/Commands/OrderDraftService.cs`
- `src/DistriFlow.Application/Orders/Commands/OrderLifecycleService.cs`
- `src/DistriFlow.Application/Stocks/StockService.cs`
- `docs/architecture/realtime.md`

The API exposes the authenticated SignalR endpoint `/api/hubs/operations`.
There are no client-invoked business commands. Controllers and HTTP contracts remain unchanged.

## Responsibilities

- Application defines `IOperationalNotifier` and small notification records, without SignalR dependencies.
- Order draft/lifecycle and stock services publish only after successful persistence. Explicit transactions commit before publication. Idempotent replays do not publish again.
- API implements the contract using a typed `IHubContext<OperationsHub, IOperationsClient>`.
- Domain and Infrastructure have no realtime dependencies. Infrastructure still does not reference Application.

## Events and recipients

| Event | Payload | Recipients |
| --- | --- | --- |
| `OrderChanged` | `orderId`, `rowVersion`, `status` | Active administrators and the order's responsible user in its tenant |
| `StockChanged` | `productIds` | Active users in its tenant |

Draft creation/line edits, confirmation and cancellation publish order updates. Confirmation deducts stock and publishes the affected product IDs. Only cancellation of a confirmed order publishes a stock update. Manual entry and adjustment also publish stock updates.

The hub authorizes the current persisted session when connecting. Routing uses server-derived claims, not user-supplied tenant/group names. Before each publication, a fresh repository scope checks distributor status, user status and whether the persisted role matches the connection role. Invalid connections are removed and aborted. Each notification attempt is bounded to three seconds. Authentication expiration closes the connection.

Browser WebSocket/SSE access tokens are read from the query string only under the hub endpoint. Production requires HTTPS; proxies and request logging must not record the `access_token` query parameter.

## Delivery and deployment limits

These are advisory updates, not durable business events. A failed publication is logged and never changes a committed HTTP operation into a failure. A process crash between commit and publication can lose an update. Clients recover by fetching authoritative HTTP data after connecting/reconnecting and through ordinary query refresh. Guaranteed delivery would require a transactional outbox, outside this implementation.

The connection registry and routing support **one API instance**. Before running multiple replicas, replace local connection routing with shared routing and cross-instance session revocation; adding a backplane alone does not share this registry. No broker, hosted service, extra project or database migration is introduced here.

## Validation

Build with `dotnet build`. If a running API locks the normal output, use `dotnet build --artifacts-path artifacts/realtime --disable-build-servers`.

Integration exercised a separately compiled local instance on port 5117 with five connections, two tenants and two preventistas. Verified authentication, tenant/owner isolation, stock entry, order creation/line edits, confirmation/cancellation, versions, stock restitution, idempotent replay and revocation of an already connected user. This does not replace browser UI validation or validate multiple replicas.
