using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Users;

namespace DistriFlow.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderLine> _orderLines = [];

    public OrderId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public UserId SalesRepId { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines.AsReadOnly();
    public Money Total => Money.Create(_orderLines.Sum(line => line.Subtotal.Amount));

    private Order() { }

    private Order(
        OrderId id,
        DistributorId distributorId,
        CustomerId customerId,
        UserId salesRepId,
        DateTime startedAtUtc)
    {
        Id = id;
        DistributorId = distributorId;
        CustomerId = customerId;
        SalesRepId = salesRepId;
        StartedAtUtc = startedAtUtc;
        Status = OrderStatus.Draft;
    }

    public static Order Start(
        DistributorId distributorId,
        CustomerId customerId,
        UserId salesRepId,
        DateTime startedAtUtc)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del pedido es requerida.");

        if (customerId.Value == Guid.Empty)
            throw new DomainException("El cliente del pedido es requerido.");

        if (salesRepId.Value == Guid.Empty)
            throw new DomainException("El preventista del pedido es requerido.");

        if (startedAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha del pedido debe estar expresada en UTC.");

        return new Order(
            OrderId.New(),
            distributorId,
            customerId,
            salesRepId,
            startedAtUtc);
    }

    public void AddLine(
        ProductId productId,
        string productDescription,
        Quantity quantity,
        Money unitPrice)
    {
        EnsureDraft();

        if (_orderLines.Any(line => line.ProductId == productId))
            throw new DomainException("El producto ya fue agregado al pedido.");

        _orderLines.Add(
            OrderLine.Create(productId, productDescription, quantity, unitPrice));
    }

    public void ChangeLineQuantity(ProductId productId, Quantity quantity)
    {
        EnsureDraft();
        GetLine(productId).ChangeQuantity(quantity);
    }

    public void RemoveLine(ProductId productId)
    {
        EnsureDraft();
        _orderLines.Remove(GetLine(productId));
    }

    public void Confirm()
    {
        EnsureDraft();

        if (_orderLines.Count == 0)
            throw new DomainException("El pedido debe tener al menos una línea para confirmarse.");

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("El pedido ya se encuentra cancelado.");

        Status = OrderStatus.Cancelled;
    }

    public OrderLine? FindLine(ProductId productId)
    {
        return _orderLines.FirstOrDefault(line => line.ProductId == productId);
    }

    private OrderLine GetLine(ProductId productId)
    {
        return FindLine(productId)
            ?? throw new DomainException("La línea del pedido no fue encontrada.");
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Solo se pueden editar pedidos en borrador.");
    }
}
