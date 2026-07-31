using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.PriceLists;

public static class PriceListErrors
{
    public static Error NotFound()
    {
        return Error.NotFound("price_list.not_found", "Lista de precios no encontrada.");
    }
}
