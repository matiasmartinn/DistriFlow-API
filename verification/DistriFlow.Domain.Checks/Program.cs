using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.DistributorConfigurations.Enums;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.Orders.Enums;
using DistriFlow.Domain.Orders.Services;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Products.ValueObjects;
using DistriFlow.Domain.Stocks;
using DistriFlow.Domain.Users.ValueObjects;

var tenant = DistributorId.New();
var actor = UserId.New();
var unit = SaleUnit.Create("unidad");
var checks = 0;

void Check(string name, Action test)
{
    test();
    checks++;
    Console.WriteLine($"PASS {name}");
}

void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected}; received {actual}.");
}

void Reject<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

Order Draft() => Order.Start(tenant, CustomerId.New(), actor, "TEST-1", OrderOrigin.Online, DateTime.UtcNow);
void Add(Order order, ProductId product, decimal amount = 2) => order.AddLine(
    product, "Test product", Quantity.Create(amount, unit), Money.Create(10), Money.Create(10),
    PriceOrigin.LastValidPrice, null, null, order.RowVersion);

Check("Empty order cannot be confirmed", () => {
    var order = Draft();
    Reject<DomainException>(() => order.Confirm(actor, DateTime.UtcNow, order.RowVersion));
    Equal(OrderStatus.Draft, order.Status);
});
Check("Zero quantity is rejected without adding a line", () => {
    var order = Draft();
    Reject<DomainException>(() => Add(order, ProductId.New(), 0));
    Equal(0, order.OrderLines.Count);
});
Check("Duplicate products are rejected", () => {
    var order = Draft(); var product = ProductId.New(); Add(order, product);
    Reject<DomainException>(() => Add(order, product));
    Equal(20m, order.Total.Amount);
});
Check("Stale order versions cannot overwrite quantity", () => {
    var order = Draft(); var product = ProductId.New(); var version = order.RowVersion;
    Add(order, product);
    Reject<ConcurrencyException>(() => order.ChangeLineQuantity(product, Quantity.Create(5, unit), version));
    Equal(20m, order.Total.Amount);
});
Check("Quantity changes recalculate the order total", () => {
    var order = Draft(); var product = ProductId.New(); Add(order, product);
    order.ChangeLineQuantity(product, Quantity.Create(3, unit), order.RowVersion);
    Equal(30m, order.Total.Amount);
});
Check("Confirmed orders cannot change lines", () => {
    var order = Draft(); var product = ProductId.New(); Add(order, product);
    order.Confirm(actor, DateTime.UtcNow, order.RowVersion);
    Reject<DomainException>(() => order.RemoveLine(product, order.RowVersion));
    Equal(20m, order.Total.Amount);
});
Check("Cancellation requires a reason and cannot repeat", () => {
    var order = Draft();
    Reject<DomainException>(() => order.Cancel(actor, DateTime.UtcNow, " ", order.RowVersion));
    Equal(OrderStatus.Draft, order.Status);
    order.Cancel(actor, DateTime.UtcNow, "Test cancellation", order.RowVersion);
    Reject<DomainException>(() => order.Cancel(actor, DateTime.UtcNow, "Again", order.RowVersion));
});
Check("Strict stock cannot become negative", () => {
    var stock = Stock.Create(tenant, ProductId.New(), unit);
    stock.Increase(Quantity.Create(2, unit));
    Reject<DomainException>(() => stock.Decrease(Quantity.Create(3, unit)));
    Equal(2m, stock.Available.Amount);
});
Check("Stock rejects incompatible units", () => {
    var stock = Stock.Create(tenant, ProductId.New(), unit);
    Reject<DomainException>(() => stock.Increase(Quantity.Create(1, SaleUnit.Create("caja"))));
    Equal(0m, stock.Available.Amount);
});
Check("Stock increment and decrement preserve quantity", () => {
    var stock = Stock.Create(tenant, ProductId.New(), unit);
    stock.Increase(Quantity.Create(10, unit)); stock.Decrease(Quantity.Create(3, unit));
    Equal(7m, stock.Available.Amount);
});
Check("Configured price fallback resolves or rejects", () => {
    var product = Product.Create(tenant, "Test", null, unit, Money.Create(100));
    Equal(100m, OrderLinePricing.Resolve(null, PriceFallbackPolicy.UseLastValidPrice, product).AppliedUnitPrice.Amount);
    Equal(OrderLinePriceStatus.Unavailable, OrderLinePricing.Resolve(null, PriceFallbackPolicy.RejectOrder, product).Status);
});
Check("Customer list price wins over base price", () => {
    var product = Product.Create(tenant, "Test", null, unit, Money.Create(100));
    var list = PriceList.Create(tenant, "Test list", actor);
    list.SetPrice(product.Id, Money.Create(80), actor);
    var price = OrderLinePricing.Resolve(list, PriceFallbackPolicy.RejectOrder, product);
    Equal(80m, price.AppliedUnitPrice.Amount);
    Equal(PriceOrigin.PriceList, price.PriceOrigin);
    Equal(list.Id, price.PriceListId!.Value);
});
Console.WriteLine($"{checks} domain checks passed.");
