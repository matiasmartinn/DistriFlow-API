namespace DistriFlow.Domain.StockMovements;

public enum StockMovementType
{
    Entry = 1,
    Exit = 2,
    AdjustmentIncrease = 3,
    AdjustmentDecrease = 4,
    OrderCancellationReversal = 5
}
