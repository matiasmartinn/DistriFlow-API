using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Users.ValueObjects;

public sealed record Role
{
    public const string AdministratorValue = "Administrator";
    public const string SalesRepValue = "SalesRep";

    public static readonly Role Administrator = new(AdministratorValue);
    public static readonly Role SalesRep = new(SalesRepValue);

    public string Value { get; private init; }

    private Role(string value)
    {
        Value = value;
    }

    public static Role From(string value)
    {
        return value switch
        {
            AdministratorValue => Administrator,
            SalesRepValue => SalesRep,
            _ => throw new DomainException("El rol del usuario es inválido.")
        };
    }

    public override string ToString() => Value;
}
