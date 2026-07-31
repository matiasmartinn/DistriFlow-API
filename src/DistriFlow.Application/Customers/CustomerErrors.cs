using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.Customers;

public static class CustomerErrors
{
    public static Error NotFound()
    {
        return Error.NotFound("customer.not_found", "Cliente no encontrado.");
    }

    public static Error Inactive()
    {
        return Error.Domain("customer.inactive", "El cliente se encuentra inactivo.");
    }

    public static Error PriceNotAvailable()
    {
        return Error.Domain(
            "customer.price_not_available",
            "No existe un precio válido para el producto.");
    }

    public static Error InvalidStatus()
    {
        return Error.Validation(
            "customer.invalid_status",
            "El estado del cliente es invalido.");
    }

    public static Error InvalidTaxCondition()
    {
        return Error.Validation(
            "customer.invalid_tax_condition",
            "La condicion fiscal del cliente es invalida.");
    }
}
