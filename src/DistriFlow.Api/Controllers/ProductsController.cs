using DistriFlow.Application.Products;
using DistriFlow.Application.Products.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize]
public sealed class ProductsController : BaseController
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.CreateAsync(request, cancellationToken),
            StatusCodes.Status201Created);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return HandleResult(await _service.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(await _service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:guid}/discontinue")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Discontinue(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.DiscontinueAsync(id, cancellationToken));
    }

    [HttpPatch("{id:guid}/reactivate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Reactivate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.ReactivateAsync(id, cancellationToken));
    }
}
