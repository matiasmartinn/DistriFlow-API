using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Customers.DTOs.Requests;

public sealed record AssignPriceListRequest
{
    [Required]
    public Guid PriceListId { get; init; }
}
