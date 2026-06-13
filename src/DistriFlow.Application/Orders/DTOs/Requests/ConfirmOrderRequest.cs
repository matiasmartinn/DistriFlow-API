namespace DistriFlow.Application.Orders.DTOs.Requests;

public sealed record ConfirmOrderRequest
{
    public long RowVersion { get; init; }
}
