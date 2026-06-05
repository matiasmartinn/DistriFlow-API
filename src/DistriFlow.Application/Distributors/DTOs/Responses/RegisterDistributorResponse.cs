namespace DistriFlow.Application.Distributors.DTOs.Responses;

public sealed record RegisterDistributorResponse(
    DistributorDto Distributor,
    AdministratorDto Administrator);
