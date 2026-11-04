using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.Orders.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

// Run from the API repository against an explicitly started local development API/database.
var baseUrl = Environment.GetEnvironmentVariable("OPERATIONAL_API_URL") ?? "http://localhost:5116/api/";
if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var apiUri) || !apiUri.IsLoopback)
    throw new InvalidOperationException("Only a local API is allowed.");
var configuration = new ConfigurationBuilder().SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "src/DistriFlow.Api"))
    .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables().Build();
var connectionString = configuration.GetConnectionString("DistriFlowDb");
var database = new NpgsqlConnectionStringBuilder(connectionString);
if (database.Host is not ("localhost" or "127.0.0.1" or "::1"))
    throw new InvalidOperationException("Only a local development database is allowed.");
await using var db = new DistriFlowDbContext(new DbContextOptionsBuilder<DistriFlowDbContext>()
    .UseNpgsql(connectionString).UseSnakeCaseNamingConvention().Options);
using var http = new HttpClient { BaseAddress = apiUri, Timeout = TimeSpan.FromSeconds(20) };
var run = $"operational-{Guid.NewGuid():N}";
var password = $"Test-{Guid.NewGuid():N}!";
var checks = 0;

void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}; received {actual}.");
}
void Pass(string name) { checks++; Console.WriteLine($"PASS {name}"); }
string Text(JsonNode node, string key) => node[key]!.GetValue<string>();
int Number(JsonNode node, string key) => node[key]!.GetValue<int>();
Guid Id(JsonNode node) => Guid.Parse(Text(node, "id"));
async Task<JsonNode> Request(HttpMethod method, string path, object? body = null, string? token = null,
    int expected = 200, bool idempotent = false)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null) request.Content = JsonContent.Create(body);
    if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (idempotent) request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
    using var response = await http.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    if ((int)response.StatusCode != expected && !(expected == 200 && (int)response.StatusCode == 204))
        throw new InvalidOperationException($"{method} {path}: {(int)response.StatusCode}, expected {expected}; {content}");
    return string.IsNullOrEmpty(content) ? new JsonObject() : JsonNode.Parse(content)!;
}
Task<JsonNode> Get(string path, string token) => Request(HttpMethod.Get, path, token: token);
async Task<(Guid Id, Guid UserId, string Token)> Tenant(string suffix)
{
    var email = $"{run}-{suffix}@example.test";
    var result = await Request(HttpMethod.Post, "Distributors", new {
        distributor = new { name = $"{run}-{suffix}", email, phone = "1123456789" },
        administrator = new { name = "Test Administrator", email, password }
    }, expected: 201);
    var login = await Request(HttpMethod.Post, "Auth/login", new { email, password });
    return (Id(result["distributor"]!), Id(result["administrator"]!), Text(login, "accessToken"));
}
async Task<(Guid Id, string Token)> Rep(string suffix, string adminToken)
{
    var email = $"{run}-{suffix}@example.test";
    var user = await Request(HttpMethod.Post, "Users", new { name = suffix, email, password, role = "SalesRep" }, adminToken, 201);
    var login = await Request(HttpMethod.Post, "Auth/login", new { email, password });
    return (Id(user), Text(login, "accessToken"));
}

var a = await Tenant("a");
var b = await Tenant("b");
var one = await Rep("one", a.Token);
var two = await Rep("two", a.Token);
var customer = await Request(HttpMethod.Post, "Customers", new {
    name = "Original customer", email = $"{run}-customer@example.test", phone = "1123456789", taxCondition = "FinalConsumer"
}, a.Token, 201);
var customerId = Id(customer);
var tenantId = DistributorId.From(a.Id);
await Request(HttpMethod.Get, "Orders/dashboard", expected: 401);
var empty = await Get("Orders/dashboard", b.Token);
Equal(0, Number(empty, "createdToday")); Equal(0, Number(empty, "draftCount"));
Equal(0, Number(empty, "confirmedToday")); Equal(0, empty["recentOrders"]!.AsArray().Count);
Pass("Empty dashboard and authentication");

await Request(HttpMethod.Put, "Auth/me/name", new { name = "  Corrected Rep  ", userId = two.Id }, one.Token);
Equal("Corrected Rep", Text(await Get("Auth/me", one.Token), "name"));
Equal("two", Text(await Get("Auth/me", two.Token), "name"));
Equal("SalesRep", Text(await Get("Auth/me", one.Token), "role"));
foreach (var invalid in new[] { " ", new string('a', 201) })
    await Request(HttpMethod.Put, "Auth/me/name", new { name = invalid }, one.Token, 400);
await Request(HttpMethod.Put, "Auth/me/name", new { name = "Corrected Administrator" }, a.Token);
Equal("Corrected Administrator", Text(await Get("Auth/me", a.Token), "name"));
Pass("Own-name updates, normalization, validation and session-derived identity");

await Request(HttpMethod.Put, $"Customers/{customerId}/commercial-info", new {
    name = "  Corrected Customer  ", zone = "Centro", taxCondition = "FinalConsumer"
}, a.Token);
Equal("Corrected Customer", Text(await Get($"Customers/{customerId}", a.Token), "name"));
await Request(HttpMethod.Put, $"Customers/{customerId}/commercial-info", new { zone = "Norte", taxCondition = "FinalConsumer" }, a.Token);
Equal("Corrected Customer", Text(await Get($"Customers/{customerId}", a.Token), "name"));
await Request(HttpMethod.Put, $"Customers/{customerId}/commercial-info", new { name = "Forbidden", taxCondition = "FinalConsumer" }, one.Token, 403);
await Request(HttpMethod.Put, $"Customers/{customerId}/commercial-info", new { name = "Forbidden", taxCondition = "FinalConsumer" }, b.Token, 404);
foreach (var invalid in new[] { " ", new string('a', 201) })
    await Request(HttpMethod.Put, $"Customers/{customerId}/commercial-info", new { name = invalid, taxCondition = "FinalConsumer" }, a.Token, 400);
Pass("Customer rename, old request compatibility, validation and role/tenant permissions");

var settings = await Get("DistributorConfigurations/current", a.Token);
settings["timeZone"] = "America/Argentina/Buenos_Aires";
await Request(HttpMethod.Put, "DistributorConfigurations/current", settings, a.Token);
var summary = await Get("Orders/dashboard", a.Token);
var today = DateOnly.Parse(Text(summary, "today"));
var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
var dayStart = TimeZoneInfo.ConvertTimeToUtc(today.ToDateTime(TimeOnly.MinValue), timeZone);
var nextDay = dayStart.AddDays(1);
Equal(nextDay, DateTime.Parse(Text(summary, "nextDayStartsAtUtc")).ToUniversalTime());
var sequence = 0;
Order Fixture(Guid owner, DateTime created)
{
    var order = Order.Start(tenantId, CustomerId.From(customerId), UserId.From(owner),
        $"FIXTURE-{++sequence:D6}", OrderOrigin.Online, created);
    db.Orders.Add(order);
    return order;
}
for (var index = 0; index < 23; index++) Fixture(one.Id, dayStart.AddHours(12).AddMinutes(index));
var boundary = Fixture(one.Id, dayStart);
Fixture(one.Id, dayStart.AddTicks(-10));
Fixture(one.Id, nextDay);
var anotherOwner = Fixture(two.Id, dayStart.AddHours(10));
Fixture(a.UserId, dayStart.AddDays(-2));
await db.SaveChangesAsync();
summary = await Get("Orders/dashboard", a.Token);
Equal(25, Number(summary, "createdToday")); Equal(28, Number(summary, "draftCount"));
Equal(10, summary["recentOrders"]!.AsArray().Count);
var own = await Get("Orders/dashboard", one.Token);
Equal(24, Number(own, "createdToday")); Equal(26, Number(own, "draftCount"));
foreach (var order in own["recentOrders"]!.AsArray()) Equal(one.Id.ToString(), Text(order!, "salesRepId"));
Equal(0, Number(await Get("Orders/dashboard", b.Token), "createdToday"));
Pass("Dashboard counts all pages, caps recent orders, scopes tenant and owner");

var localRange = $"localDateFrom={today:yyyy-MM-dd}&localDateTo={today:yyyy-MM-dd}";
var filtered = await Get($"Orders?{localRange}&pageSize=100", one.Token);
Equal(24, Number(filtered, "totalCount"));
if (!filtered["items"]!.AsArray().Any(item => Text(item!, "id") == boundary.Id.Value.ToString()))
    throw new InvalidOperationException("Start-of-day boundary was excluded.");
Equal(24, Number(await Get($"Orders?{localRange}&salesRepId={two.Id}", one.Token), "totalCount"));
Equal(25, Number(await Get($"Orders?{localRange}", a.Token), "totalCount"));
await Request(HttpMethod.Get, $"Orders/{anotherOwner.Id.Value}", token: one.Token, expected: 404);
foreach (var path in new[] { $"Orders?{localRange}&dateFrom={dayStart:O}", "Orders?dateField=Unknown", "Orders?localDateFrom=2026-03-02&localDateTo=2026-03-01" })
    await Request(HttpMethod.Get, path, token: a.Token, expected: 400);
Pass("Local-day filters match cards, include midnight, exclude next day and prevent owner override");

var product = await Request(HttpMethod.Post, "Products", new { name = "Test Product", saleUnit = "unidad", basePrice = 10 }, a.Token, 201);
await Request(HttpMethod.Post, $"Stocks/by-product/{Id(product)}/increase", new { amount = 10 }, a.Token, idempotent: true);
var live = await Request(HttpMethod.Post, "Orders", new { customerId }, one.Token, 201, true);
live = await Request(HttpMethod.Post, $"Orders/{Id(live)}/lines", new { productId = Id(product), amount = 1, rowVersion = Number(live, "rowVersion") }, one.Token);
// Backdate only this suite's new draft so creation and confirmation fall on different days.
var liveId = OrderId.From(Id(live));
var liveEntity = await db.Orders.SingleAsync(order => order.DistributorId == tenantId && order.Id == liveId);
db.Entry(liveEntity).Property(order => order.CreatedAtUtc).CurrentValue = dayStart.AddDays(-2);
await db.SaveChangesAsync();
live = await Request(HttpMethod.Patch, $"Orders/{Id(live)}/confirm", new { rowVersion = Number(live, "rowVersion") }, one.Token, idempotent: true);
summary = await Get("Orders/dashboard", one.Token);
Equal(1, Number(summary, "confirmedToday")); Equal(24, Number(summary, "createdToday"));
var confirmed = await Get($"Orders?{localRange}&status=Confirmed&dateField=Confirmed", one.Token);
Equal(1, Number(confirmed, "totalCount"));
Equal(0, Number(await Get($"Orders?{localRange}&status=Confirmed&dateField=Created", one.Token), "totalCount"));
var detail = await Get($"Orders/{Id(live)}", a.Token);
Equal("Corrected Customer", Text(detail, "customerName")); Equal("Corrected Rep", Text(detail, "salesRepName"));
await Request(HttpMethod.Patch, $"Orders/{Id(live)}/cancel", new { rowVersion = Number(live, "rowVersion"), cancellationReason = "Test reversal" }, one.Token, idempotent: true);
Equal(0, Number(await Get("Orders/dashboard", one.Token), "confirmedToday"));
Pass("Confirmation date differs from creation, names refresh and cancellation updates summary");

foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch })
{
    var path = method == HttpMethod.Post ? $"Orders/{anotherOwner.Id.Value}/lines"
        : method == HttpMethod.Put || method == HttpMethod.Delete ? $"Orders/{anotherOwner.Id.Value}/lines/{Id(product)}?rowVersion=1"
        : $"Orders/{anotherOwner.Id.Value}/confirm";
    await Request(method, path, method == HttpMethod.Delete ? null : new { productId = Id(product), amount = 1, rowVersion = 1 }, one.Token, 404, method == HttpMethod.Patch);
}
await Request(HttpMethod.Patch, $"Orders/{anotherOwner.Id.Value}/cancel", new { rowVersion = 1, cancellationReason = "Forbidden" }, one.Token, 404, true);
Pass("Other preventista cannot add/change/remove lines, confirm or cancel");

// Santiago advances at midnight on this date; the local calendar must handle the skipped hour.
settings = await Get("DistributorConfigurations/current", a.Token);
settings["timeZone"] = "America/Santiago";
await Request(HttpMethod.Put, "DistributorConfigurations/current", settings, a.Token);
Equal(0, Number(await Get("Orders?localDateFrom=2026-09-06&localDateTo=2026-09-06", a.Token), "totalCount"));
Pass("Local date filters handle midnight daylight-saving transitions");
await Request(HttpMethod.Patch, $"Customers/{customerId}/deactivate", new { reason = "Test" }, a.Token);
await Request(HttpMethod.Put, $"Customers/{customerId}/commercial-info", new { name = "Inactive", taxCondition = "FinalConsumer" }, a.Token, 400);
await Request(HttpMethod.Patch, $"Users/{one.Id}/deactivate", new { reason = "Test" }, a.Token);
await Request(HttpMethod.Put, "Auth/me/name", new { name = "Disabled" }, one.Token, 403);
Pass("Inactive customers cannot be renamed and disabled sessions cannot edit profile");
Console.WriteLine($"{checks} operational integration checks passed. Synthetic tenants: {a.Id}, {b.Id}.");
