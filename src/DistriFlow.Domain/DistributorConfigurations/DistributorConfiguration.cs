using DistriFlow.Domain.Common.Errors;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.DistributorConfigurations.Enums;
using DistriFlow.Domain.Distributors.ValueObjects;

namespace DistriFlow.Domain.DistributorConfigurations;

public sealed class DistributorConfiguration
{
    public DistributorId DistributorId { get; private set; }
    public StockPolicy StockPolicy { get; private set; }
    public PriceFallbackPolicy PriceFallbackPolicy { get; private set; }
    public string? OrderNumberPrefix { get; private set; }
    public long NextOrderNumber { get; private set; }
    public string TimeZone { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private DistributorConfiguration() { }

    private DistributorConfiguration(
        DistributorId distributorId,
        DateTime createdAtUtc)
    {
        DistributorId = distributorId;
        StockPolicy = StockPolicy.RequireAvailableStock;
        PriceFallbackPolicy = PriceFallbackPolicy.UseLastValidPrice;
        OrderNumberPrefix = "PED";
        NextOrderNumber = 1;
        TimeZone = "UTC";
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public static DistributorConfiguration CreateDefault(
        DistributorId distributorId,
        DateTime createdAtUtc)
    {
        if (distributorId.Value == Guid.Empty)
            throw new DomainException("La distribuidora de la configuración es requerida.");

        EnsureUtc(createdAtUtc);

        return new DistributorConfiguration(distributorId, createdAtUtc);
    }

    public void Update(
        StockPolicy stockPolicy,
        PriceFallbackPolicy priceFallbackPolicy,
        string? orderNumberPrefix,
        long nextOrderNumber,
        string timeZone,
        DateTime updatedAtUtc)
    {
        if (nextOrderNumber < NextOrderNumber)
        {
            throw new DomainException(
                "El próximo número de pedido no puede disminuir.");
        }

        if (nextOrderNumber < 1)
            throw new DomainException("El próximo número de pedido debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(timeZone))
            throw new DomainException("La zona horaria es requerida.");

        EnsureValidTimeZone(timeZone);
        EnsureUtc(updatedAtUtc);

        StockPolicy = stockPolicy;
        PriceFallbackPolicy = priceFallbackPolicy;
        OrderNumberPrefix = string.IsNullOrWhiteSpace(orderNumberPrefix)
            ? null
            : orderNumberPrefix.Trim();
        NextOrderNumber = nextOrderNumber;
        TimeZone = timeZone.Trim();
        UpdatedAtUtc = updatedAtUtc;
    }

    private static void EnsureValidTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException("La zona horaria es inválida.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new DomainException("La zona horaria es inválida.");
        }
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new DomainException("La fecha debe estar expresada en UTC.");
    }
}
