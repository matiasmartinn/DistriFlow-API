namespace DistriFlow.Application.Orders.DTOs.Requests;

public sealed record ChangeOrderLineQuantityRequest
{
    public decimal Amount { get; init; }

    public long RowVersion { get; init; }
}
