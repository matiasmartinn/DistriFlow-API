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
    public string OrderNumber { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public UserId CreatedByUserId { get; private set; }
    public OrderOrigin Origin { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public UserId? ConfirmedByUserId { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public UserId? CancelledByUserId { get; private set; }
    public string? CancellationReason { get; private set; }
    public long RowVersion { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> OrderLines => _orderLines.AsReadOnly();
    public Money Total => Money.Create(_orderLines.Sum(line => line.Subtotal.Amount));

    private Order() { }

    private Order(
        OrderId id,
        DistributorId distributorId,
        CustomerId customerId,
        UserId salesRepId,
        string orderNumber,
        OrderOrigin origin,
        DateTime createdAtUtc)
    {
        Id = id;
        DistributorId = distributorId;
        CustomerId = customerId;
        SalesRepId = salesRepId;
        OrderNumber = orderNumber;
        Origin = origin;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = salesRepId;
        RowVersion = 1;
        Status = OrderStatus.Draft;
    }

    public static Order Start(
        DistributorId distributorId,
        CustomerId customerId,
        UserId salesRepId,
        string orderNumber,
        OrderOrigin origin,
        DateTime createdAtUtc)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del pedido es requerida.");

        if (customerId.Value == Guid.Empty)
            throw new DomainException("El cliente del pedido es requerido.");

        if (salesRepId.Value == Guid.Empty)
            throw new DomainException("El preventista del pedido es requerido.");

        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("El número del pedido es requerido.");

        if (!Enum.IsDefined(origin))
            throw new DomainException("El origen del pedido es invalido.");

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha del pedido debe estar expresada en UTC.");

        return new Order(
            OrderId.New(),
            distributorId,
            customerId,
            salesRepId,
            orderNumber.Trim(),
            origin,
            createdAtUtc);
    }

    public void AddLine(
        ProductId productId,
        string productDescription,
        Quantity quantity,
        Money unitPrice,
        long expectedRowVersion)
    {
        EnsureExpectedRowVersion(expectedRowVersion);
        EnsureDraft();

        if (_orderLines.Any(line => line.ProductId == productId))
            throw new DomainException("El producto ya fue agregado al pedido.");

        _orderLines.Add(
            OrderLine.Create(productId, productDescription, quantity, unitPrice));
        AdvanceRowVersion();
    }

    public void ChangeLineQuantity(
        ProductId productId,
        Quantity quantity,
        long expectedRowVersion)
    {
        EnsureExpectedRowVersion(expectedRowVersion);
        EnsureDraft();
        GetLine(productId).ChangeQuantity(quantity);
        AdvanceRowVersion();
    }

    public void RemoveLine(ProductId productId, long expectedRowVersion)
    {
        EnsureExpectedRowVersion(expectedRowVersion);
        EnsureDraft();
        _orderLines.Remove(GetLine(productId));
        AdvanceRowVersion();
    }

    public void Confirm(
        UserId confirmedByUserId,
        DateTime confirmedAtUtc,
        long expectedRowVersion)
    {
        EnsureExpectedRowVersion(expectedRowVersion);
        EnsureDraft();

        if (_orderLines.Count == 0)
            throw new DomainException("El pedido debe tener al menos una línea para confirmarse.");

        EnsureTransitionActorAndDate(confirmedByUserId, confirmedAtUtc);

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc;
        ConfirmedByUserId = confirmedByUserId;
        AdvanceRowVersion();
    }

    public void Cancel(
        UserId cancelledByUserId,
        DateTime cancelledAtUtc,
        string cancellationReason,
        long expectedRowVersion)
    {
        EnsureExpectedRowVersion(expectedRowVersion);

        if (Status == OrderStatus.Cancelled)
            throw new DomainException("El pedido ya se encuentra cancelado.");

        EnsureTransitionActorAndDate(cancelledByUserId, cancelledAtUtc);

        if (string.IsNullOrWhiteSpace(cancellationReason))
            throw new DomainException("El motivo de cancelacion es requerido.");

        if (cancellationReason.Trim().Length > 500)
            throw new DomainException("El motivo de cancelacion no puede superar 500 caracteres.");

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        CancelledByUserId = cancelledByUserId;
        CancellationReason = cancellationReason.Trim();
        AdvanceRowVersion();
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

    private void EnsureExpectedRowVersion(long expectedRowVersion)
    {
        if (expectedRowVersion != RowVersion)
        {
            throw new ConcurrencyException(
                "El pedido fue modificado por otra operacion. Actualiza los datos e intenta nuevamente.");
        }
    }

    private static void EnsureTransitionActorAndDate(
        UserId userId,
        DateTime occurredAtUtc)
    {
        if (userId.Value == Guid.Empty)
            throw new DomainException("El usuario responsable es requerido.");

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha de transicion debe estar expresada en UTC.");
    }

    private void AdvanceRowVersion()
    {
        RowVersion++;
    }
}
