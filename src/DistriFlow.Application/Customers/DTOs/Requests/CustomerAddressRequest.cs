using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Customers.DTOs.Requests;

public sealed record CustomerAddressRequest
{
    [Required]
    public string Street { get; init; } = null!;

    [Range(1, int.MaxValue)]
    public int CityId { get; init; }

    public string? ZipCode { get; init; }
}
