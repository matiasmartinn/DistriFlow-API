using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Users;

namespace DistriFlow.Domain.PriceLists;

public sealed class PriceList
{
    private readonly List<ProductPrice> _productPrices = [];

    public PriceListId Id { get; private set; }
    public PriceListId SeriesId { get; private set; }
    public DistributorId DistributorId { get; private set; }
    public string Name { get; private set; } = null!;
    public PriceListStatus Status { get; private set; }
    public int Version { get; private set; }
    public DateTime? ValidFromUtc { get; private set; }
    public DateTime? ValidUntilUtc { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }
    public DateTime? DeactivatedAtUtc { get; private set; }
    public UserId CreatedByUserId { get; private set; }
    public UserId UpdatedByUserId { get; private set; }
    public long RowVersion { get; private set; }
    public IReadOnlyCollection<ProductPrice> ProductPrices => _productPrices.AsReadOnly();

    private PriceList() { }

    private PriceList(
        PriceListId id,
        PriceListId seriesId,
        DistributorId distributorId,
        string name,
        int version,
        UserId createdByUserId)
    {
        Id = id;
        SeriesId = seriesId;
        DistributorId = distributorId;
        Name = name;
        Version = version;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
        RowVersion = 1;
        Status = PriceListStatus.Draft;
    }

    public static PriceList Create(
        DistributorId distributorId,
        string name,
        UserId createdByUserId)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora de la lista de precios es requerida.");

        EnsureUserId(createdByUserId);

        var id = PriceListId.New();

        return new PriceList(
            id,
            id,
            distributorId,
            NormalizeName(name),
            1,
            createdByUserId);
    }

    public PriceList CreateNextVersion(int version, UserId createdByUserId)
    {
        EnsureUserId(createdByUserId);

        if (version <= Version)
            throw new DomainException("La nueva version debe ser posterior a la actual.");

        var nextVersion = new PriceList(
            PriceListId.New(),
            SeriesId,
            DistributorId,
            Name,
            version,
            createdByUserId);

        foreach (var productPrice in _productPrices)
        {
            nextVersion._productPrices.Add(
                ProductPrice.Create(productPrice.ProductId, productPrice.Price));
        }

        return nextVersion;
    }

    public void Rename(string name, UserId updatedByUserId)
    {
        EnsureDraft();
        Name = NormalizeName(name);
        Touch(updatedByUserId);
    }

    public void SetPrice(ProductId productId, Money price, UserId updatedByUserId)
    {
        EnsureDraft();
        EnsureProductId(productId);

        var productPrice = _productPrices.FirstOrDefault(x => x.ProductId == productId);

        if (productPrice is null)
        {
            _productPrices.Add(ProductPrice.Create(productId, price));
        }
        else
        {
            productPrice.ChangePrice(price);
        }

        Touch(updatedByUserId);
    }

    public void RemoveProduct(ProductId productId, UserId updatedByUserId)
    {
        EnsureDraft();
        EnsureProductId(productId);

        var productPrice = _productPrices.FirstOrDefault(x => x.ProductId == productId);

        if (productPrice is not null)
        {
            _productPrices.Remove(productPrice);
            Touch(updatedByUserId);
        }
    }

    public Money? FindPrice(ProductId productId)
    {
        EnsureProductId(productId);
        return _productPrices.FirstOrDefault(x => x.ProductId == productId)?.Price;
    }

    public bool IsEffectiveAt(DateTime dateTimeUtc)
    {
        EnsureUtc(dateTimeUtc, "La fecha de consulta debe estar expresada en UTC.");

        return Status == PriceListStatus.Active
            && ValidFromUtc.HasValue
            && ValidFromUtc.Value <= dateTimeUtc
            && (!ValidUntilUtc.HasValue || dateTimeUtc < ValidUntilUtc.Value);
    }

    public void Activate(
        DateTime validFromUtc,
        DateTime? validUntilUtc,
        DateTime activatedAtUtc,
        UserId updatedByUserId)
    {
        EnsureDraft();
        EnsureUtc(validFromUtc, "La fecha de inicio debe estar expresada en UTC.");
        EnsureUtc(activatedAtUtc, "La fecha de activacion debe estar expresada en UTC.");

        if (validUntilUtc.HasValue)
        {
            EnsureUtc(
                validUntilUtc.Value,
                "La fecha de fin debe estar expresada en UTC.");

            if (validUntilUtc.Value <= validFromUtc)
                throw new DomainException(
                    "La fecha de fin debe ser posterior a la fecha de inicio.");

            if (validUntilUtc.Value <= activatedAtUtc)
                throw new DomainException(
                    "No se puede activar una lista cuya vigencia ya finalizo.");
        }

        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        ActivatedAtUtc = activatedAtUtc;
        Status = PriceListStatus.Active;
        Touch(updatedByUserId);
    }

    public void Deactivate(DateTime deactivatedAtUtc, UserId updatedByUserId)
    {
        EnsureUtc(
            deactivatedAtUtc,
            "La fecha de inactivacion debe estar expresada en UTC.");

        if (Status != PriceListStatus.Active)
            throw new DomainException("Solo una lista activa puede inactivarse.");

        DeactivatedAtUtc = deactivatedAtUtc;
        Status = PriceListStatus.Inactive;
        Touch(updatedByUserId);
    }

    private void EnsureDraft()
    {
        if (Status != PriceListStatus.Draft)
            throw new DomainException(
                "Solo una lista de precios en borrador puede modificarse.");
    }

    private void Touch(UserId updatedByUserId)
    {
        EnsureUserId(updatedByUserId);
        UpdatedByUserId = updatedByUserId;
        RowVersion++;
    }

    private static void EnsureProductId(ProductId productId)
    {
        if (productId.Value == Guid.Empty)
            throw new DomainException("El producto del precio es requerido.");
    }

    private static void EnsureUserId(UserId userId)
    {
        if (userId.Value == Guid.Empty)
            throw new DomainException("El usuario responsable es requerido.");
    }

    private static void EnsureUtc(DateTime value, string message)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new DomainException(message);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la lista de precios es requerido.");

        return name.Trim();
    }
}
