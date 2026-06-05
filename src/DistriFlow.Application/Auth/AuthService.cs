using DistriFlow.Application.Auth.DTOs;
using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;

namespace DistriFlow.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IDistributorRepository _distributorRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IDistributorRepository distributorRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _distributorRepository = distributorRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        DistributorId distributorId;
        Email email;

        try
        {
            distributorId = DistributorId.From(request.DistributorId);
            email = Email.Create(request.Email);
        }
        catch (DomainException)
        {
            return InvalidCredentials();
        }

        var user = await _userRepository.GetByEmailAsync(
            distributorId,
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
            distributorId,
            cancellationToken);

        if (distributor is null)
            return InvalidCredentials();

        if (distributor.Status != DistributorStatus.Active)
        {
            return Error.Forbidden(
                "auth.distributor_suspended",
                "La distribuidora se encuentra suspendida.");
        }

        var token = _tokenGenerator.Generate(user);

        return new LoginResponse(
            token.Value,
            token.ExpiresAtUtc,
            user.Id.Value,
            user.DistributorId.Value,
            user.Role.Value);
    }

    private static Error InvalidCredentials()
    {
        return Error.Unauthorized(
            "auth.invalid_credentials",
            "Credenciales inválidas.");
    }
}
