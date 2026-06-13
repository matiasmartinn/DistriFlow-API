using DistriFlow.Api.Security;
using DistriFlow.Application.Customers;
using DistriFlow.Application.Customers.DTOs.Requests;
using DistriFlow.Application.Orders.DTOs.Requests;
using DistriFlow.Application.Orders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.ActiveDistributor)]
public sealed class CustomersController : BaseController
{
    private readonly ICustomerService _service;
    private readonly IOrderQueryService _orderQueryService;

    public CustomersController(
        ICustomerService service,
        IOrderQueryService orderQueryService)
    {
        _service = service;
        _orderQueryService = orderQueryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
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

    [HttpPut("{id:guid}/contact")]
    public async Task<IActionResult> UpdateContact(
        [FromRoute] Guid id,
        [FromBody] UpdateCustomerContactRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.UpdateContactAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/price-list")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> AssignPriceList(
        [FromRoute] Guid id,
        [FromBody] AssignPriceListRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.AssignPriceListAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}/price-list")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> RemovePriceList(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.RemovePriceListAsync(id, cancellationToken));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Deactivate(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.DeactivateAsync(id, cancellationToken));
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

    [HttpGet("{id:guid}/products/{productId:guid}/price")]
    public async Task<IActionResult> ResolvePrice(
        [FromRoute] Guid id,
        [FromRoute] Guid productId,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.ResolvePriceAsync(id, productId, cancellationToken));
    }

    [HttpGet("{id:guid}/orders")]
    public async Task<IActionResult> GetOrderHistory(
        [FromRoute] Guid id,
        [FromQuery] CustomerOrderHistoryRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _orderQueryService.GetByCustomerIdAsync(
                id,
                request,
                cancellationToken));
    }
}
