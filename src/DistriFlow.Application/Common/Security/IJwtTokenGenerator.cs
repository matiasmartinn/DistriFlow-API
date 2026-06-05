using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Common.Security;

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
