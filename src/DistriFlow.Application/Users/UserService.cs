using DistriFlow.Application.Common.Pagination;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Users.DTOs.Requests;
using DistriFlow.Application.Users.DTOs.Responses;
using DistriFlow.Application.Users.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserContext _currentUserContext;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserContext currentUserContext)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = Email.Create(request.Email);

            if (await _userRepository.ExistsByEmailAsync(
                    email,
                    cancellationToken))
            {
                return Error.Conflict(
                    "user.email_conflict",
                    "Ya existe un usuario con ese email.");
            }

            var user = User.Create(
                _currentUserContext.DistributorId,
                request.Name,
                email,
                PasswordHash.From(_passwordHasher.Hash(request.Password)),
                Role.From(request.Role));

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return user.ToDto();
        }
        catch (DomainException exception)
        {
            return Error.Domain("user.invalid", exception.Message);
        }
    }

    public async Task<Result<PagedResponse<UserDto>>> GetPagedAsync(
        UserListRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var role = string.IsNullOrWhiteSpace(request.Role)
                ? null
                : Role.From(request.Role);

            if (!TryParseStatus(request.Status, out var status))
            {
                return Error.Validation(
                    "user.invalid_status",
                    "El estado del usuario es inválido.");
            }

            var (users, totalCount) = await _userRepository.GetPagedAsync(
                _currentUserContext.DistributorId,
                role,
                status,
                request.SearchText?.Trim(),
                request.Page,
                request.PageSize,
                cancellationToken);

            return new PagedResponse<UserDto>(
                users.Select(user => user.ToDto()).ToList(),
                request.Page,
                request.PageSize,
                totalCount,
                TotalPages(totalCount, request.PageSize));
        }
        catch (DomainException exception)
        {
            return Error.Validation("user.invalid_filter", exception.Message);
        }
    }

    public Task<Result> ChangeRoleAsync(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            user => user.ChangeRole(Role.From(request.Role)),
            "user.invalid_role",
            cancellationToken);
    }

    public Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ChangeAsync(
            id,
            user => user.Deactivate(),
            "user.invalid_status",
            cancellationToken);
    }

    private async Task<Result> ChangeAsync(
        Guid id,
        Action<User> change,
        string errorCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                UserId.From(id),
                cancellationToken);

            if (user is null)
            {
                return Error.NotFound(
                    "user.not_found",
                    "Usuario no encontrado.");
            }

            change(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain(errorCode, exception.Message);
        }
    }

    private static bool TryParseStatus(string? value, out UserStatus? status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<UserStatus>(value, true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            status = parsed;
            return true;
        }

        status = null;
        return false;
    }

    private static int TotalPages(int totalCount, int pageSize)
    {
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
