namespace DistriFlow.Domain.Common.ValueObjects;

public sealed record ContactInfo
{
    public Email Email { get; private init; } = null!;
    public PhoneNumber Phone { get; private init; } = null!;

    private ContactInfo() { }

    private ContactInfo(Email email, PhoneNumber phone)
    {
        Email = email;
        Phone = phone;
    }

    public static ContactInfo Create(string email, string phone)
    {
        return new ContactInfo(
            Email.Create(email),
            PhoneNumber.Create(phone));
    }
}
