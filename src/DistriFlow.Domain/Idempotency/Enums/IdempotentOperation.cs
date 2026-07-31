namespace DistriFlow.Domain.Idempotency.Enums;

public enum IdempotentOperation
{
    StartOrder = 1,
    ConfirmOrder = 2,
    CancelOrder = 3,
    IncreaseStock = 4,
    AdjustStock = 5,
    ActivatePriceListVersion = 6
}
