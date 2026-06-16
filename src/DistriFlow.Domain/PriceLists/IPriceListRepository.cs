using DistriFlow.Domain.Distributors;

namespace DistriFlow.Domain.PriceLists;

public interface IPriceListRepository
{
    Task<PriceList?> GetByIdAsync(
        DistributorId distributorId,
        PriceListId id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceList>> GetAllAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceList>> GetEffectiveAsync(
        DistributorId distributorId,
        DateTime effectiveAtUtc,
        CancellationToken cancellationToken = default);

    Task<PriceList?> GetEffectiveBySeriesIdAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        DateTime effectiveAtUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceList>> GetVersionsAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        CancellationToken cancellationToken = default);

    Task<PriceListId?> GetSeriesIdAsync(
        DistributorId distributorId,
        PriceListId id,
        CancellationToken cancellationToken = default);

    Task<int> GetNextVersionNumberAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingActiveVersionAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        PriceListId excludedId,
        DateTime validFromUtc,
        DateTime? validUntilUtc,
        CancellationToken cancellationToken = default);

    Task LockSeriesAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PriceList priceList,
        CancellationToken cancellationToken = default);
}
