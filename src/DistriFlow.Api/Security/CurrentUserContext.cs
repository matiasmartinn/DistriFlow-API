using System.Security.Claims;
using DistriFlow.Application.Common.Security;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Api.Security;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public UserId UserId =>
        Guid.TryParse(Principal?.FindFirstValue(DistriFlowClaims.UserId), out var id)
            ? UserId.From(id)
            : throw new UnauthorizedAccessException("No hay un usuario autenticado.");

    public DistributorId DistributorId =>
        Guid.TryParse(Principal?.FindFirstValue(DistriFlowClaims.DistributorId), out var id)
            ? DistributorId.From(id)
            : throw new UnauthorizedAccessException(
                "No hay una distribuidora asociada al usuario autenticado.");

    public Role Role =>
        Role.From(
            Principal?.FindFirstValue(DistriFlowClaims.Role)
            ?? throw new UnauthorizedAccessException(
                "El usuario autenticado no tiene un rol válido."));
}
