using DistriFlow.Api.Security;
using DistriFlow.Application.Auth;
using DistriFlow.Application.Auth.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

public sealed class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _authService.LoginAsync(request, cancellationToken));
    }

    [HttpPut("me/name")]
    [Authorize(Policy = AuthorizationPolicies.ActiveUser)]
    public async Task<IActionResult> UpdateCurrentName(
        [FromBody] UpdateCurrentUserNameRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(await _authService.UpdateCurrentNameAsync(request, cancellationToken));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ActiveUser)]
    public async Task<IActionResult> GetCurrent(
        CancellationToken cancellationToken)
    {
        return HandleResult(
            await _authService.GetCurrentAsync(cancellationToken));
    }
}
