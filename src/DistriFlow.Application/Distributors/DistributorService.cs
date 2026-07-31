using DistriFlow.Application.Common.Results;
using DistriFlow.Application.Common.Security;
using DistriFlow.Application.Distributors.DTOs.Requests;
using DistriFlow.Application.Distributors.DTOs.Responses;
using DistriFlow.Application.Distributors.Mappers;
using DistriFlow.Domain.Common;
using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Application.Distributors;

public sealed class DistributorService : IDistributorService
{
    private readonly IDistributorRepository _distributorRepository;
    private readonly IDistributorConfigurationRepository _configurationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserContext _currentUserContext;

    public DistributorService(
        IDistributorRepository distributorRepository,
        IDistributorConfigurationRepository configurationRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserContext currentUserContext)
    {
        _distributorRepository = distributorRepository;
        _configurationRepository = configurationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUserContext = currentUserContext;
    }

    public async Task<Result<RegisterDistributorResponse>> RegisterAsync(
        RegisterDistributorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = ContactInfo.Create(
                request.Distributor.Email,
                request.Distributor.Phone);

            var address = request.Distributor.Address is null
                ? null
                : Address.Create(
                    request.Distributor.Address.Street,
                    request.Distributor.Address.CityId,
                    request.Distributor.Address.ZipCode);

            var distributor = Distributor.Register(
                request.Distributor.Name,
                contact,
                address);

            if (await _distributorRepository.ExistsByNameAsync(
                    distributor.Name,
                    cancellationToken))
            {
                return Error.Conflict(
                    "distributor.name_conflict",
                    "Ya existe una distribuidora con ese nombre.");
            }

            if (await _distributorRepository.ExistsByEmailAsync(
                    distributor.Contact.Email,
                    cancellationToken))
            {
                return Error.Conflict(
                    "distributor.email_conflict",
                    "Ya existe una distribuidora con ese email.");
            }

            var administrator = User.Create(
                distributor.Id,
                request.Administrator.Name,
                Email.Create(request.Administrator.Email),
                PasswordHash.From(_passwordHasher.Hash(request.Administrator.Password)),
                Role.Administrator);

            var configuration = DistributorConfiguration.CreateDefault(
                distributor.Id,
                DateTime.UtcNow);

            if (await _userRepository.ExistsByEmailAsync(
                    administrator.Email,
                    cancellationToken))
            {
                return Error.Conflict(
                    "user.email_conflict",
                    "Ya existe un usuario con ese email.");
            }

            await _distributorRepository.AddAsync(distributor, cancellationToken);
            await _configurationRepository.AddAsync(configuration, cancellationToken);
            await _userRepository.AddAsync(administrator, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RegisterDistributorResponse(
                distributor.ToDto(),
                administrator.ToAdministratorDto());
        }
        catch (DomainException exception)
        {
            return Error.Domain("distributor.invalid", exception.Message);
        }
    }

    public async Task<Result<DistributorDto>> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var distributor = await _distributorRepository.GetByIdAsync(
            _currentUserContext.DistributorId,
            cancellationToken);

        if (distributor is null)
        {
            return Error.NotFound(
                "distributor.not_found",
                "Distribuidora no encontrada.");
        }

        return distributor.ToDto();
    }

    public Task<Result> SuspendCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(
            distributor => distributor.Suspend(),
            cancellationToken);
    }

    public Task<Result> ReactivateCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        return ChangeStatusAsync(
            distributor => distributor.Reactivate(),
            cancellationToken);
    }

    private async Task<Result> ChangeStatusAsync(
        Action<Distributor> changeStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            var distributor = await _distributorRepository.GetByIdAsync(
                _currentUserContext.DistributorId,
                cancellationToken);

            if (distributor is null)
            {
                return Error.NotFound(
                    "distributor.not_found",
                    "Distribuidora no encontrada.");
            }

            changeStatus(distributor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainException exception)
        {
            return Error.Domain("distributor.invalid_status", exception.Message);
        }
    }
}
