using DistriFlow.Api.Security;
using DistriFlow.Application.Users;
using DistriFlow.Application.Users.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize(
    Roles = "Administrator",
    Policy = AuthorizationPolicies.ActiveDistributor)]
public sealed class UsersController : BaseController
{
    private readonly IUserService _service;

    public UsersController(IUserService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.CreateAsync(request, cancellationToken),
            StatusCodes.Status201Created);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] UserListRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.GetPagedAsync(request, cancellationToken));
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(
        [FromRoute] Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.ChangeRoleAsync(id, request, cancellationToken));
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
