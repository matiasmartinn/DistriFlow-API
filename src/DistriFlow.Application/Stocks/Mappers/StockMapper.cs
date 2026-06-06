using DistriFlow.Application.Stocks.DTOs.Responses;
using DistriFlow.Domain.Products;
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
}
