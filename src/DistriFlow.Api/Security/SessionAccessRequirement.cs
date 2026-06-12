using Microsoft.AspNetCore.Authorization;

namespace DistriFlow.Api.Security;

internal sealed record SessionAccessRequirement(
    bool RequireActiveDistributor) : IAuthorizationRequirement;
