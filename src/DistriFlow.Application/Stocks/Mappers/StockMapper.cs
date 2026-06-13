using DistriFlow.Application.Stocks.DTOs.Responses;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.StockMovements;
using DistriFlow.Domain.Stocks;

namespace DistriFlow.Application.Stocks.Mappers;

public static class StockMapper
{
    public static StockDto ToDto(this Stock stock)
    {
        return new StockDto(
            stock.ProductId.Value,
            stock.Available.Amount,
            stock.Available.Unit.Value);
    }

    public static StockDto ToEmptyStockDto(this Product product)
    {
        return new StockDto(
            product.Id.Value,
            0,
            product.SaleUnit.Value);
    }

    public static StockDto ToDto(this StockChange change, ProductId productId)
    {
        return new StockDto(
            productId.Value,
            change.ResultingQuantity,
            change.SaleUnit.Value);
    }

    public static StockMovementDto ToDto(this StockMovement movement)
    {
        return new StockMovementDto(
            movement.Id.Value,
            movement.ProductId.Value,
            movement.Type.ToString(),
            movement.Quantity,
            movement.PreviousQuantity,
            movement.ResultingQuantity,
            movement.SaleUnit.Value,
            movement.ReferenceType.ToString(),
            movement.ReferenceId,
            movement.Reason,
            movement.CreatedAtUtc,
            movement.CreatedByUserId.Value);
    }
}
