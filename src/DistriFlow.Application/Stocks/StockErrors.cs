using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.Stocks;

public static class StockErrors
{
    public static Error NotFound()
    {
        return Error.NotFound("stock.not_found", "Stock no encontrado.");
    }

    public static Error ChangeConflict()
    {
        return Error.Conflict(
            "stock.change_conflict",
            "El stock fue modificado por otra operacion. Actualiza los datos e intenta nuevamente.");
    }
}
