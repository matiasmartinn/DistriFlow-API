using DistriFlow.Application.Auth.DTOs;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Distributors.Enums;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Users.Enums;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IDistributorRepository _distributorRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IDistributorRepository distributorRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        ICurrentUserContext currentUserContext,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _distributorRepository = distributorRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _currentUserContext = currentUserContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        Email email;

        try
        {
            email = Email.Create(request.Email);
        }
        catch (DomainException)
        {
            return InvalidCredentials();
        }

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null
            || user.PasswordHash is null
            || !_passwordHasher.Verify(request.Password, user.PasswordHash.Value))
        {
            return InvalidCredentials();
        }

        if (user.Status != UserStatus.Active)
        {
            return Error.Forbidden(
                "auth.user_inactive",
                "El usuario no se encuentra activo.");
        }

        var distributor = await _distributorRepository.GetByIdAsync(
            user.DistributorId,
            cancellationToken);

        if (distributor is null)
            return InvalidCredentials();

        if (distributor.Status != DistributorStatus.Active)
        {
            return Error.Forbidden(
                "auth.distributor_suspended",
                "La distribuidora se encuentra suspendida.");
        }

        user.RegisterSuccessfulLogin(DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _tokenGenerator.Generate(user);
        return new LoginResponse(
            token.Value,
            token.ExpiresAtUtc,
            user.Id.Value,
            user.DistributorId.Value,
            user.Role.Value);
    }

    public async Task<Result<CurrentUserDto>> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            _currentUserContext.UserId,
            cancellationToken);

        var distributor = await _distributorRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        if (user is null || distributor is null)
        {
            return Error.NotFound(
                "auth.current_user_not_found",
                "No se encontró la sesión actual.");
        }

        return new CurrentUserDto(
            user.Id.Value,
            distributor.Id.Value,
            distributor.Name,
            user.Role.Value,
            user.Email.Value,
            user.Name);
    }

    public async Task<Result<CurrentUserDto>> UpdateCurrentNameAsync(
        UpdateCurrentUserNameRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                _currentUserContext.UserId,
                cancellationToken);

            if (user is null)
                return Error.NotFound("auth.current_user_not_found", "No se encontró la sesión actual.");

            user.Rename(request.Name);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await GetCurrentAsync(cancellationToken);
        }
        catch (DomainException exception)
        {
            return Error.Domain("user.invalid_name", exception.Message);
        }
    }

    public async Task<Result> ValidateCurrentSessionAsync(
        bool requireActiveDistributor,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            _currentUserContext.UserId,
            cancellationToken);

        if (user is null
            || user.Status != UserStatus.Active
            || user.Role != _currentUserContext.Role)
        {
            return Error.Forbidden(
                "auth.session_invalid",
                "La sesión actual ya no se encuentra habilitada.");
        }

        var distributor = await _distributorRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        if (distributor is null)
        {
            return Error.Forbidden(
                "auth.distributor_not_found",
                "La distribuidora de la sesión actual no existe.");
        }

        if (requireActiveDistributor
            && distributor.Status != DistributorStatus.Active)
        {
            return Error.Forbidden(
                "auth.distributor_suspended",
                "La distribuidora se encuentra suspendida.");
        }

        return Result.Success();
    }

    private static Error InvalidCredentials()
    {
        return Error.Unauthorized(
            "auth.invalid_credentials",
            "Credenciales inválidas.");
    }
}
