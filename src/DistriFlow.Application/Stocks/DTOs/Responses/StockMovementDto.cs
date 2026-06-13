namespace DistriFlow.Application.Stocks.DTOs.Responses;

public sealed record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string Type,
    decimal Quantity,
    decimal PreviousQuantity,
    decimal ResultingQuantity,
    string SaleUnit,
    string ReferenceType,
    Guid? ReferenceId,
    string? Reason,
    DateTime CreatedAtUtc,
    Guid CreatedByUserId);
