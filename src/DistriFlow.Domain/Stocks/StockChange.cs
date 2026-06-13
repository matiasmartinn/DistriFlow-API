using DistriFlow.Domain.Common.ValueObjects;

namespace DistriFlow.Domain.Stocks;

public sealed record StockChange(
    decimal PreviousQuantity,
    decimal ResultingQuantity,
    SaleUnit SaleUnit);
