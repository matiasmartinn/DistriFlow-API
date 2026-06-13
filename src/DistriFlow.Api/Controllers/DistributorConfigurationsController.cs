using DistriFlow.Api.Security;
using DistriFlow.Application.DistributorConfigurations;
using DistriFlow.Application.DistributorConfigurations.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[Authorize(
    Roles = "Administrator",
    Policy = AuthorizationPolicies.ActiveDistributor)]
public sealed class DistributorConfigurationsController : BaseController
{
    private readonly IDistributorConfigurationService _service;

    public DistributorConfigurationsController(
        IDistributorConfigurationService service)
    {
        _service = service;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(
        CancellationToken cancellationToken)
    {
        return HandleResult(await _service.GetCurrentAsync(cancellationToken));
    }

    [HttpPut("current")]
    public async Task<IActionResult> UpdateCurrent(
        [FromBody] UpdateDistributorConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _service.UpdateCurrentAsync(request, cancellationToken));
    }
}
