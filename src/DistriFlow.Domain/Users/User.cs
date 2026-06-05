using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;

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

    private User() { }

    private User(
        UserId id,
        DistributorId distributorId,
        string name,
        Email email,
        PasswordHash passwordHash,
        Role role)
    {
        Id = id;
        DistributorId = distributorId;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        Status = UserStatus.Active;
    }

    public static User Create(
        DistributorId distributorId,
        string name,
        Email email,
        PasswordHash passwordHash,
        Role role)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del usuario es requerida.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del usuario es requerido.");

        return new User(
            UserId.New(),
            distributorId,
            name.Trim(),
            email,
            passwordHash,
            role);
    }

    public void ChangeRole(Role role)
    {
        Role = role;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
    }
}
