using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Common.Security;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    UserId UserId { get; }
    DistributorId DistributorId { get; }
    Role Role { get; }
}
