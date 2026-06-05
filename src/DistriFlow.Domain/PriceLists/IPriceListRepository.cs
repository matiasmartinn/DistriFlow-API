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

    Task AddAsync(
        PriceList priceList,
        CancellationToken cancellationToken = default);
}
