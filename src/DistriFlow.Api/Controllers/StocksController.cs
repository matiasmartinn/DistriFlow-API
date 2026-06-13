using DistriFlow.Api.Security;
using DistriFlow.Application.Stocks;
using DistriFlow.Application.Stocks.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.ActiveDistributor)]
public sealed class StocksController : BaseController
{
    private readonly IStockService _service;

    public StocksController(IStockService service)
    {
        _service = service;
    }

    [HttpGet("by-product/{productId:guid}")]
    public async Task<IActionResult> GetByProductId(
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.GetByProductIdAsync(productId, cancellationToken));
    }

    [HttpPost("by-product/{productId:guid}/increase")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Increase(
        [FromRoute] Guid productId,
        [FromBody] IncreaseStockRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.IncreaseAsync(productId, request, cancellationToken));
    }

    [HttpPut("by-product/{productId:guid}/adjust")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Adjust(
        [FromRoute] Guid productId,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.AdjustAsync(productId, request, cancellationToken));
    }

    [HttpGet("movements")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] StockMovementListRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.GetMovementsAsync(request, cancellationToken));
    }
}
