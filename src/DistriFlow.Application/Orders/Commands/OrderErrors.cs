using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.Orders.Commands;

public static class OrderErrors
{
    public static Error OrderNotFound()
    {
        return Error.NotFound("order.not_found", "Pedido no encontrado.");
    }

    public static Error OrderOperatorRequired()
    {
        return Error.Forbidden(
            "order.operator_required",
            "Solo un administrador o preventista puede operar pedidos.");
    }

    public static Error OrderLineNotFound()
    {
        return Error.NotFound(
            "order.line_not_found",
            "Línea del pedido no encontrada.");
    }

    public static Error ProductUnavailable(string productDescription)
    {
        return Error.Domain(
            "order.product_unavailable",
            $"No se puede confirmar porque {productDescription} no se encuentra activo.");
    }

    public static Error DistributorSuspended()
    {
        return Error.Domain(
            "order.distributor_suspended",
            "No se pueden tomar pedidos para una distribuidora suspendida.");
    }

    public static Error CustomerInactive()
    {
        return Error.Domain(
            "order.customer_inactive",
            "No se pueden tomar pedidos para un cliente inactivo.");
    }

    public static Error PriceNotAvailable(string productName)
    {
        return Error.Domain(
            "order.price_not_available",
            $"No existe un precio válido para {productName}.");
    }

    public static Error InsufficientStock(string productDescription)
    {
        return Error.Domain(
            "order.insufficient_stock",
            $"El stock disponible de {productDescription} es insuficiente.");
    }

    public static Error OrderConcurrencyConflict()
    {
        return Error.Conflict(
            "order.concurrency_conflict",
            "El pedido fue modificado por otra operacion. Actualiza los datos e intenta nuevamente.");
    }

    public static Error InvalidOrderOrigin()
    {
        return Error.Validation(
            "order.invalid_origin",
            "El origen del pedido es invalido.");
    }

    public static Error StockNotFound(string productDescription)
    {
        return Error.Conflict(
            "order.stock_not_found",
            $"No se encontró el stock de {productDescription} para revertir el pedido.");
    }

    public static Error StockReversalUnavailable()
    {
        return Error.Conflict(
            "order.stock_reversal_unavailable",
            "No se puede cancelar porque los movimientos de confirmacion no coinciden con el pedido.");
    }
}