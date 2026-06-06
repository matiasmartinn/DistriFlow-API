using DistriFlow.Application.Orders.Commands;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize]
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
    public async Task<IActionResult> Start(
        [FromBody] StartOrderRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.StartAsync(request, cancellationToken),
            StatusCodes.Status201Created);
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
    public async Task<IActionResult> AddLine(
        [FromRoute] Guid id,
        [FromBody] AddOrderLineRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.AddLineAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/lines/{productId:guid}")]
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
    public async Task<IActionResult> RemoveLine(
        [FromRoute] Guid id,
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.RemoveLineAsync(
                id,
                productId,
                cancellationToken));
    }

    [HttpPatch("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _commandService.ConfirmAsync(id, cancellationToken));
    }
}
