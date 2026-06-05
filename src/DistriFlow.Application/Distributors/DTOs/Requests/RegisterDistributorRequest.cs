using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Distributors.DTOs.Requests;

public sealed record RegisterDistributorRequest
{
    [Required]
    public CreateDistributorRequest Distributor { get; init; } = null!;

    [Required]
    public CreateAdministratorRequest Administrator { get; init; } = null!;
}
