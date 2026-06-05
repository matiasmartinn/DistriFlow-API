using DistriFlow.Domain.Common.Errors;

namespace DistriFlow.Domain.Common.ValueObjects;

public sealed record Address
{
    public string Street { get; private init; } = null!;
    public int CityId { get; private init; }
    public string? ZipCode { get; private init; }

    private Address() { }

    private Address(string street, int cityId, string? zipCode)
    {
        Street = street;
        CityId = cityId;
        ZipCode = zipCode;
    }

    public static Address Create(string street, int cityId, string? zipCode)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("La calle es requerida.");

        if (cityId <= 0)
            throw new DomainException("La ciudad es requerida.");

        return new Address(
            street.Trim(),
            cityId,
            string.IsNullOrWhiteSpace(zipCode) ? null : zipCode.Trim());
    }
}
