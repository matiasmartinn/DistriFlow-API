using DistriFlow.Application.Products.DTOs.Responses;
using DistriFlow.Domain.Products;

namespace DistriFlow.Application.Products.Mappers;

internal static class ProductMapper
{
    internal static ProductDto ToDto(this Product product)
    {
        return new ProductDto(
            product.Id.Value,
            product.Name,
            product.Sku?.Value,
            product.SaleUnit.Value,
            product.BasePrice.Amount,
            product.Status.ToString());
    }
}
