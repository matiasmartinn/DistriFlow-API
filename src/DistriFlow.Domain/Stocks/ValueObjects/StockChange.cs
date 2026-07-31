using DistriFlow.Domain.Common.ValueObjects;

namespace DistriFlow.Domain.Stocks.ValueObjects;

public sealed record StockChange(
    decimal PreviousQuantity,
    decimal ResultingQuantity,
    SaleUnit SaleUnit,
    long RowVersion);
