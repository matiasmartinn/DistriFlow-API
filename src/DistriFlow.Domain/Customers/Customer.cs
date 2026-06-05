using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.PriceLists;

namespace DistriFlow.Domain.Customers;

public sealed class Customer
{
    public CustomerId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public string Name { get; private set; } = null!;
    public ContactInfo Contact { get; private set; } = null!;
    public Address? Address { get; private set; }
    public PriceListId? PriceListId { get; private set; }
    public CustomerStatus Status { get; private set; }

    private Customer() { }

    private Customer(
        CustomerId id,
        DistributorId distributorId,
        string name,
        ContactInfo contact,
        Address? address,
        PriceListId? priceListId)
    {
        Id = id;
        DistributorId = distributorId;
        Name = name;
        Contact = contact;
        Address = address;
        PriceListId = priceListId;
        Status = CustomerStatus.Active;
    }

    public static Customer Create(
        DistributorId distributorId,
        string name,
        ContactInfo contact,
        Address? address = null,
        PriceListId? priceListId = null)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del cliente es requerida.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del cliente es requerido.");

        if (priceListId.HasValue && priceListId.Value.Value == Guid.Empty)
            throw new DomainException("La lista de precios del cliente es inválida.");

        return new Customer(
            CustomerId.New(),
            distributorId,
            name.Trim(),
            contact,
            address,
            priceListId);
    }

    public void AssignPriceList(PriceListId priceListId)
    {
        if (priceListId.Value == Guid.Empty)
            throw new DomainException("La lista de precios del cliente es requerida.");

        PriceListId = priceListId;
    }

    public void RemovePriceList()
    {
        PriceListId = null;
    }

    public void UpdateContact(ContactInfo contact, Address? address)
    {
        Contact = contact;
        Address = address;
    }

    public void Deactivate()
    {
        Status = CustomerStatus.Inactive;
    }
}
