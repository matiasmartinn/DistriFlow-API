using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Users.Enums;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Domain.Users;

public sealed class User
{
    public UserId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public string Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public PasswordHash? PasswordHash { get; private set; }
    public Role Role { get; private set; } = null!;
    public UserStatus Status { get; private set; }
    public UserId? CreatedByUserId { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime? DeactivatedAtUtc { get; private set; }
    public UserId? DeactivatedByUserId { get; private set; }
    public string? DeactivationReason { get; private set; }

    private User() { }

    private User(
        UserId id,
        DistributorId distributorId,
        string name,
        Email email,
        PasswordHash passwordHash,
        Role role,
        UserId? createdByUserId)
    {
        Id = id;
        DistributorId = distributorId;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedByUserId = createdByUserId;
        Status = UserStatus.Active;
    }

    public static User Create(
        DistributorId distributorId,
        string name,
        Email email,
        PasswordHash passwordHash,
        Role role,
        UserId? createdByUserId = null)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del usuario es requerida.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del usuario es requerido.");

        if (createdByUserId.HasValue && createdByUserId.Value.Value == Guid.Empty)
            throw new DomainException("El usuario creador es invalido.");

        return new User(
            UserId.New(),
            distributorId,
            name.Trim(),
            email,
            passwordHash,
            role,
            createdByUserId);
    }

    public void ChangeRole(Role role)
    {
        EnsureActive();
        Role = role;
    }

    public void Deactivate(
        UserId deactivatedByUserId,
        DateTime deactivatedAtUtc,
        string reason)
    {
        if (Status == UserStatus.Inactive)
            throw new DomainException("El usuario ya se encuentra inactivo.");

        EnsureActorAndUtc(deactivatedByUserId, deactivatedAtUtc);

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("El motivo de desactivacion es requerido.");

        var normalizedReason = reason.Trim();

        if (normalizedReason.Length > 500)
            throw new DomainException("El motivo de desactivacion no puede superar 500 caracteres.");

        Status = UserStatus.Inactive;
        DeactivatedAtUtc = deactivatedAtUtc;
        DeactivatedByUserId = deactivatedByUserId;
        DeactivationReason = normalizedReason;
    }

    public void Reactivate()
    {
        if (Status == UserStatus.Active)
            throw new DomainException("El usuario ya se encuentra activo.");

        Status = UserStatus.Active;
    }

    public void RegisterSuccessfulLogin(DateTime occurredAtUtc)
    {
        if (occurredAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha de acceso debe estar expresada en UTC.");

        LastLoginAtUtc = occurredAtUtc;
    }

    private void EnsureActive()
    {
        if (Status != UserStatus.Active)
            throw new DomainException("El usuario se encuentra inactivo.");
    }

    private static void EnsureActorAndUtc(UserId userId, DateTime occurredAtUtc)
    {
        if (userId.Value == Guid.Empty)
            throw new DomainException("El usuario responsable es requerido.");

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha debe estar expresada en UTC.");
    }
}
