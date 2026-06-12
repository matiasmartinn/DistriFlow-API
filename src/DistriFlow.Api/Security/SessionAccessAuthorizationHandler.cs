using DistriFlow.Application.Auth;
using Microsoft.AspNetCore.Authorization;

namespace DistriFlow.Api.Security;

internal sealed class SessionAccessAuthorizationHandler
    : AuthorizationHandler<SessionAccessRequirement>
{
    private readonly IAuthService _authService;

    public SessionAccessAuthorizationHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SessionAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var result = await _authService.ValidateCurrentSessionAsync(
            requirement.RequireActiveDistributor);

        if (result.IsSuccess)
            context.Succeed(requirement);
    }
}
