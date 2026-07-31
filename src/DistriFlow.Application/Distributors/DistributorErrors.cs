using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.Distributors;

public static class DistributorErrors
{
    public static Error NotFound()
    {
        return Error.NotFound("distributor.not_found", "Distribuidora no encontrada.");
    }
}
