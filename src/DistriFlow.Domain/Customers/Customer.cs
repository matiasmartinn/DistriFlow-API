using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Users;
using DistriFlow.Domain.Customers.Enums;
using DistriFlow.Domain.Customers.ValueObjects;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.PriceLists.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Domain.Customers;

public sealed class Customer
{
    public CustomerId Id { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public string Name { get; private set; } = null!;
    public ContactInfo Contact { get; private set; } = null!;
    public Address? Address { get; private set; }
    public PriceListId? PriceListId { get; private set; }
    public string? Zone { get; private set; }
    public TaxCondition TaxCondition { get; private set; }
    public DateTime? DeactivatedAtUtc { get; private set; }
    public UserId? DeactivatedByUserId { get; private set; }
    public string? DeactivationReason { get; private set; }
    public CustomerStatus Status { get; private set; }

    private Customer() { }

    private Customer(
        CustomerId id,
        DistributorId distributorId,
        string name,
        ContactInfo contact,
        Address? address,
        PriceListId? priceListId,
        string? zone,
        TaxCondition taxCondition)
    {
        Id = id;
        DistributorId = distributorId;
        Name = name;
        Contact = contact;
        Address = address;
        PriceListId = priceListId;
        Zone = NormalizeOptional(zone, 100, "La zona no puede superar 100 caracteres.");
        TaxCondition = taxCondition;
        Status = CustomerStatus.Active;
    }

    public static Customer Create(
        DistributorId distributorId,
        string name,
        ContactInfo contact,
        TaxCondition taxCondition,
        Address? address = null,
        PriceListId? priceListId = null,
        string? zone = null)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora del cliente es requerida.");

        if (priceListId.HasValue && priceListId.Value.Value == Guid.Empty)
            throw new DomainException("La lista de precios del cliente es invalida.");

        EnsureTaxCondition(taxCondition);

        return new Customer(
            CustomerId.New(),
            distributorId,
            NormalizeName(name),
            contact,
            address,
            priceListId,
            zone,
            taxCondition);
    }

    public void AssignPriceList(PriceListId priceListId)
    {
        EnsureActive();

        if (priceListId.Value == Guid.Empty)
            throw new DomainException("La lista de precios del cliente es requerida.");

        PriceListId = priceListId;
    }

    public void RemovePriceList()
    {
        EnsureActive();
        PriceListId = null;
    }

    public void UpdateContact(ContactInfo contact, Address? address)
    {
        EnsureActive();
        Contact = contact;
        Address = address;
    }

    public void UpdateCommercialInfo(string? zone, TaxCondition taxCondition, string? name = null)
    {
        EnsureActive();
        EnsureTaxCondition(taxCondition);

        var normalizedName = name is null ? Name : NormalizeName(name);
        Zone = NormalizeOptional(zone, 100, "La zona no puede superar 100 caracteres.");
        TaxCondition = taxCondition;
        Name = normalizedName;
    }

    public void Deactivate(
        UserId deactivatedByUserId,
        DateTime deactivatedAtUtc,
        string reason)
    {
        if (Status == CustomerStatus.Inactive)
            throw new DomainException("El cliente ya se encuentra inactivo.");

        EnsureDeactivation(deactivatedByUserId, deactivatedAtUtc, reason);

        DeactivatedByUserId = deactivatedByUserId;
        DeactivatedAtUtc = deactivatedAtUtc;
        DeactivationReason = reason.Trim();
        Status = CustomerStatus.Inactive;
    }

    public void Reactivate()
    {
        if (Status == CustomerStatus.Active)
            throw new DomainException("El cliente ya se encuentra activo.");

        Status = CustomerStatus.Active;
    }

    private void EnsureActive()
    {
        if (Status != CustomerStatus.Active)
            throw new DomainException("El cliente se encuentra inactivo.");
    }

    private static void EnsureTaxCondition(TaxCondition taxCondition)
    {
        if (!Enum.IsDefined(taxCondition))
            throw new DomainException("La condicion fiscal del cliente es invalida.");
    }

    private static void EnsureDeactivation(
        UserId userId,
        DateTime occurredAtUtc,
        string reason)
    {
        if (userId.Value == Guid.Empty)
            throw new DomainException("El usuario responsable es requerido.");

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha de inactivacion debe estar expresada en UTC.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("El motivo de inactivacion es requerido.");

        if (reason.Trim().Length > 500)
            throw new DomainException("El motivo de inactivacion no puede superar 500 caracteres.");
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del cliente es requerido.");

        var normalized = name.Trim();
        if (normalized.Length > 200)
            throw new DomainException("El nombre del cliente no puede superar 200 caracteres.");

        return normalized;
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength,
        string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
            throw new DomainException(errorMessage);

        return normalized;
    }
}
