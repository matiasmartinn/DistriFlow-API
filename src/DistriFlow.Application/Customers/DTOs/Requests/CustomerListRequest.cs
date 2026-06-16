namespace DistriFlow.Application.Customers.DTOs.Requests;

public sealed record CustomerListRequest
{
    public bool IncludeInactive { get; init; }
    public string? Status { get; init; }
    public string? TaxCondition { get; init; }
    public string? Zone { get; init; }
}
