using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Common.ValueObjects;

public sealed record Email
{
    public string Value { get; private init; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("El email es requerido.");

        var normalized = value.Trim().ToLowerInvariant();

        if (!normalized.Contains('@') || normalized.Split('@').Length != 2)
            throw new DomainException("El formato del email es inválido.");

        return new Email(normalized);
    }

    public override string ToString() => Value;
}
