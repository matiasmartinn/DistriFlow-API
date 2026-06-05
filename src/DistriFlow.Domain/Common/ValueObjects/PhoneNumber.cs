using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Common.ValueObjects;

public sealed record PhoneNumber
{
    public string Value { get; private init; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("El teléfono es requerido.");

        return new PhoneNumber(value.Trim());
    }

    public override string ToString() => Value;
}
