using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors.Enums;
using DistriFlow.Domain.Distributors.ValueObjects;

namespace DistriFlow.Domain.Distributors;

public sealed class Distributor
{
    public DistributorId Id { get; private set; }
    public string Name { get; private set; } = null!;
    public ContactInfo Contact { get; private set; } = null!;
    public Address? Address { get; private set; }
    public DistributorStatus Status { get; private set; }

    private Distributor() { }

    private Distributor(
        DistributorId id,
        string name,
        ContactInfo contact,
        Address? address)
    {
        Id = id;
        Name = name;
        Contact = contact;
        Address = address;
        Status = DistributorStatus.Active;
    }

    public static Distributor Register(
        string name,
        ContactInfo contact,
        Address? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la distribuidora es requerido.");

        return new Distributor(
            DistributorId.New(),
            name.Trim(),
            contact,
            address);
    }

    public void Suspend()
    {
        if (Status == DistributorStatus.Suspended)
            throw new DomainException("La distribuidora ya se encuentra suspendida.");

        Status = DistributorStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status == DistributorStatus.Active)
            throw new DomainException("La distribuidora ya se encuentra activa.");

        Status = DistributorStatus.Active;
    }
}
