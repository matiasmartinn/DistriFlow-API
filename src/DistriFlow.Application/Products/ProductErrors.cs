using DistriFlow.Application.Common.Results;

namespace DistriFlow.Application.Products;

public static class ProductErrors
{
    public static Error NotFound()
    {
        return Error.NotFound("product.not_found", "Producto no encontrado.");
    }
}
