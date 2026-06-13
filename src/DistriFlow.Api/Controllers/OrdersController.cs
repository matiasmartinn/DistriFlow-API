using DistriFlow.Api.Security;
using DistriFlow.Application.Orders.Commands;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.ActiveDistributor)]
public sealed class OrdersController : BaseController
{
    private readonly IOrderCommandService _commandService;
    private readonly IOrderQueryService _queryService;

    public OrdersController(
        IOrderCommandService commandService,
        IOrderQueryService queryService)
    {
        _commandService = commandService;
        _queryService = queryService;
    }

    [HttpPost]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> Start(
        [FromBody] StartOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.StartAsync(
                request,
                idempotencyKey,
                cancellationToken),
            StatusCodes.Status201Created);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] OrderListRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _queryService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _queryService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/lines")]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> AddLine(
        [FromRoute] Guid id,
        [FromBody] AddOrderLineRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.AddLineAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/lines/{productId:guid}")]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> ChangeLineQuantity(
        [FromRoute] Guid id,
        [FromRoute] Guid productId,
        [FromBody] ChangeOrderLineQuantityRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.ChangeLineQuantityAsync(
                id,
                productId,
                request,
                cancellationToken));
    }

    [HttpDelete("{id:guid}/lines/{productId:guid}")]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> RemoveLine(
        [FromRoute] Guid id,
        [FromRoute] Guid productId,
        [FromQuery] long rowVersion,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.RemoveLineAsync(
                id,
                productId,
                rowVersion,
                cancellationToken));
    }

    [HttpPatch("{id:guid}/confirm")]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid id,
        [FromBody] ConfirmOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.ConfirmAsync(
                id,
                request,
                idempotencyKey,
                cancellationToken));
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = "SalesRep")]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.CancelAsync(
                id,
                request,
                idempotencyKey,
                cancellationToken));
    }
}
