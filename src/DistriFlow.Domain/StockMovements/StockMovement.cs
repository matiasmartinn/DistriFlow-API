using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Products.ValueObjects;
using DistriFlow.Domain.StockMovements.Enums;
using DistriFlow.Domain.StockMovements.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Domain.StockMovements;

public sealed class StockMovement
{
    public StockMovementId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public ProductId ProductId { get; private set; }
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal PreviousQuantity { get; private set; }
    public decimal ResultingQuantity { get; private set; }
    public SaleUnit SaleUnit { get; private set; } = null!;
    public StockMovementReferenceType ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public UserId CreatedByUserId { get; private set; }

    private StockMovement() { }

    private StockMovement(
        StockMovementId id,
        DistributorId distributorId,
        ProductId productId,
        StockMovementType type,
        decimal quantity,
        decimal previousQuantity,
        decimal resultingQuantity,
        SaleUnit saleUnit,
        StockMovementReferenceType referenceType,
        Guid? referenceId,
        string? reason,
        DateTime createdAtUtc,
        UserId createdByUserId)
    {
        Id = id;
        DistributorId = distributorId;
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        PreviousQuantity = previousQuantity;
        ResultingQuantity = resultingQuantity;
        SaleUnit = saleUnit;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
    }

    public static StockMovement Create(
        DistributorId distributorId,
        ProductId productId,
        StockMovementType type,
        decimal quantity,
        decimal previousQuantity,
        decimal resultingQuantity,
        SaleUnit saleUnit,
        StockMovementReferenceType referenceType,
        Guid? referenceId,
        string? reason,
        DateTime createdAtUtc,
        UserId createdByUserId)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del movimiento es requerida.");

        if (productId.Value == Guid.Empty)
            throw new DomainException("El producto del movimiento es requerido.");

        if (!Enum.IsDefined(type))
            throw new DomainException("El tipo de movimiento es invalido.");

        if (!Enum.IsDefined(referenceType))
            throw new DomainException("El tipo de referencia del movimiento es invalido.");

        if (quantity <= 0)
            throw new DomainException("La cantidad del movimiento debe ser mayor a cero.");

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha del movimiento debe estar expresada en UTC.");

        if (createdByUserId.Value == Guid.Empty)
            throw new DomainException("El usuario responsable del movimiento es requerido.");

        EnsureReference(type, referenceType, referenceId);
        EnsureReason(type, reason);
        EnsureQuantities(type, quantity, previousQuantity, resultingQuantity);

        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim();

        if (normalizedReason?.Length > 500)
            throw new DomainException("El motivo del movimiento no puede superar 500 caracteres.");

        return new StockMovement(
            StockMovementId.New(),
            distributorId,
            productId,
            type,
            quantity,
            previousQuantity,
            resultingQuantity,
            saleUnit,
            referenceType,
            referenceId,
            normalizedReason,
            createdAtUtc,
            createdByUserId);
    }

    private static void EnsureReference(
        StockMovementType type,
        StockMovementReferenceType referenceType,
        Guid? referenceId)
    {
        var requiresReference = type is StockMovementType.Exit
            or StockMovementType.OrderCancellationReversal;

        if (requiresReference && (!referenceId.HasValue || referenceId == Guid.Empty))
            throw new DomainException("La referencia del movimiento es requerida.");

        if (type == StockMovementType.Exit
            && referenceType != StockMovementReferenceType.Order)
        {
            throw new DomainException("La salida debe referenciar un pedido.");
        }

        if (type == StockMovementType.OrderCancellationReversal
            && referenceType != StockMovementReferenceType.OrderCancellation)
        {
            throw new DomainException("La reversa debe referenciar una cancelacion de pedido.");
        }

        if (type == StockMovementType.Entry
            && referenceType is not StockMovementReferenceType.ManualEntry
                and not StockMovementReferenceType.InitialLoad)
        {
            throw new DomainException("La entrada debe referenciar una carga manual o inicial.");
        }

        if (type is StockMovementType.AdjustmentIncrease
                or StockMovementType.AdjustmentDecrease
            && referenceType != StockMovementReferenceType.ManualAdjustment)
        {
            throw new DomainException("El ajuste debe referenciar una operacion manual.");
        }

        if (!requiresReference && referenceId.HasValue)
            throw new DomainException("La referencia indicada no corresponde al tipo de movimiento.");
    }

    private static void EnsureReason(StockMovementType type, string? reason)
    {
        if ((type is StockMovementType.AdjustmentIncrease
                or StockMovementType.AdjustmentDecrease)
            && string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("El motivo del ajuste de stock es requerido.");
        }
    }

    private static void EnsureQuantities(
        StockMovementType type,
        decimal quantity,
        decimal previousQuantity,
        decimal resultingQuantity)
    {
        var expectedResult = type is StockMovementType.Entry
            or StockMovementType.AdjustmentIncrease
            or StockMovementType.OrderCancellationReversal
                ? previousQuantity + quantity
                : previousQuantity - quantity;

        if (expectedResult != resultingQuantity)
            throw new DomainException("Las cantidades del movimiento de stock son inconsistentes.");
    }
}
