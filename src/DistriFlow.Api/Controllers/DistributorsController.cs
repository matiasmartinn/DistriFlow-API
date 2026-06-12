using DistriFlow.Api.Security;
using DistriFlow.Application.Distributors;
using DistriFlow.Application.Distributors.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
public sealed class DistributorsController : BaseController
{
    private readonly IDistributorService _service;

    public DistributorsController(IDistributorService service)
    {
        _service = service;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDistributorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.RegisterAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleResult(result);

        return HandleCreated(
            result,
            nameof(GetCurrent),
            new { });
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(
        CancellationToken cancellationToken)
    {
        return HandleResult(await _service.GetCurrentAsync(cancellationToken));
    }

    [HttpPatch("current/suspend")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> SuspendCurrent(
        CancellationToken cancellationToken)
    {
        return HandleResult(await _service.SuspendCurrentAsync(cancellationToken));
    }

    [HttpPatch("current/reactivate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ReactivateCurrent(
        CancellationToken cancellationToken)
    {
        return HandleResult(await _service.ReactivateCurrentAsync(cancellationToken));
    }
}
