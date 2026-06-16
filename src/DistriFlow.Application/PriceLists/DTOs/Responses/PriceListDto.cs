namespace DistriFlow.Application.PriceLists.DTOs.Responses;

public sealed record PriceListDto(
    Guid Id,
    Guid SeriesId,
    string Name,
    string Status,
    int Version,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    DateTime? ActivatedAtUtc,
    DateTime? DeactivatedAtUtc,
    Guid CreatedByUserId,
    Guid UpdatedByUserId,
    long RowVersion,
    IReadOnlyList<ProductPriceDto> ProductPrices);
