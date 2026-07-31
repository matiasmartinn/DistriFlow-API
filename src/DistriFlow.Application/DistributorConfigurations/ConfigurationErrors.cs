using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.DistributorConfigurations;

public static class ConfigurationErrors
{
    public static Error NotFound()
    {
        return Error.NotFound(
            "distributor_configuration.not_found",
            "Configuración de distribuidora no encontrada.");
    }
}
