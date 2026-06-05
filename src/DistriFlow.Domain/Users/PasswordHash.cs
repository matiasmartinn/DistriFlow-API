using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Users;

public sealed record PasswordHash
{
    public string Value { get; private init; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static PasswordHash From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("El hash de contraseña es requerido.");

        return new PasswordHash(value);
    }
}
