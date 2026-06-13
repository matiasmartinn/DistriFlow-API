using DistriFlow.Api.Security;
using DistriFlow.Application.PriceLists;
using DistriFlow.Application.PriceLists.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize(
    Roles = "Administrator",
    Policy = AuthorizationPolicies.ActiveDistributor)]
public sealed class PriceListsController : BaseController
{
    private readonly IPriceListService _service;

    public PriceListsController(IPriceListService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePriceListRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.CreateAsync(request, cancellationToken),
            StatusCodes.Status201Created);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.GetAllAsync(includeInactive, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(await _service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/name")]
    public async Task<IActionResult> Rename(
        [FromRoute] Guid id,
        [FromBody] RenamePriceListRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.RenameAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/products/{productId:guid}")]
    public async Task<IActionResult> SetProductPrice(
        [FromRoute] Guid id,
        [FromRoute] Guid productId,
        [FromBody] SetProductPriceRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.SetProductPriceAsync(
                id,
                productId,
                request,
                cancellationToken));
    }

    [HttpDelete("{id:guid}/products/{productId:guid}")]
    public async Task<IActionResult> RemoveProduct(
        [FromRoute] Guid id,
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.RemoveProductAsync(id, productId, cancellationToken));
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.DeactivateAsync(id, cancellationToken));
    }

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.ReactivateAsync(id, cancellationToken));
    }
}
