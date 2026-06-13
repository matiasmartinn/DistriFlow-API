using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.DistributorConfigurations.DTOs.Requests;

public sealed record UpdateDistributorConfigurationRequest
{
    [Required]
    public string StockPolicy { get; init; } = null!;

    [Required]
    public string PriceFallbackPolicy { get; init; } = null!;

    public string? OrderNumberPrefix { get; init; }

    public long NextOrderNumber { get; init; }

    [Required]
    public string TimeZone { get; init; } = null!;
}
