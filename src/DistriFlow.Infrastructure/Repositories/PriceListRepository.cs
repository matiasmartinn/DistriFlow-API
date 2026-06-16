using System.Data;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class PriceListRepository
    : Repository<PriceList, PriceListId>,
      IPriceListRepository
{
    public PriceListRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<PriceList?> GetByIdAsync(
        DistributorId distributorId,
        PriceListId id,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(priceList => priceList.ProductPrices)
            .FirstOrDefaultAsync(
                priceList => priceList.DistributorId == distributorId
                    && priceList.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PriceList>> GetAllAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(priceList => priceList.ProductPrices)
            .Where(priceList => priceList.DistributorId == distributorId)
            .OrderBy(priceList => priceList.Name)
            .ThenByDescending(priceList => priceList.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceList>> GetEffectiveAsync(
        DistributorId distributorId,
        DateTime effectiveAtUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(priceList => priceList.ProductPrices)
            .Where(priceList => priceList.DistributorId == distributorId
                && priceList.Status == PriceListStatus.Active
                && priceList.ValidFromUtc <= effectiveAtUtc
                && (!priceList.ValidUntilUtc.HasValue
                    || effectiveAtUtc < priceList.ValidUntilUtc.Value))
            .OrderBy(priceList => priceList.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<PriceList?> GetEffectiveBySeriesIdAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        DateTime effectiveAtUtc,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .AsNoTracking()
            .Include(priceList => priceList.ProductPrices)
            .FirstOrDefaultAsync(
                priceList => priceList.DistributorId == distributorId
                    && priceList.SeriesId == seriesId
                    && priceList.Status == PriceListStatus.Active
                    && priceList.ValidFromUtc <= effectiveAtUtc
                    && (!priceList.ValidUntilUtc.HasValue
                        || effectiveAtUtc < priceList.ValidUntilUtc.Value),
                cancellationToken);
    }

    public async Task<IReadOnlyList<PriceList>> GetVersionsAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(priceList => priceList.ProductPrices)
            .Where(priceList => priceList.DistributorId == distributorId
                && priceList.SeriesId == seriesId)
            .OrderByDescending(priceList => priceList.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceListId?> GetSeriesIdAsync(
        DistributorId distributorId,
        PriceListId id,
        CancellationToken cancellationToken = default)
    {
        var priceList = await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                priceList => priceList.DistributorId == distributorId
                    && priceList.Id == id,
                cancellationToken);

        return priceList?.SeriesId;
    }

    public async Task<int> GetNextVersionNumberAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = await DbSet
            .Where(priceList => priceList.DistributorId == distributorId
                && priceList.SeriesId == seriesId)
            .MaxAsync(
                priceList => (int?)priceList.Version,
                cancellationToken);

        return (currentVersion ?? 0) + 1;
    }

    public Task<bool> HasOverlappingActiveVersionAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        PriceListId excludedId,
        DateTime validFromUtc,
        DateTime? validUntilUtc,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            priceList => priceList.DistributorId == distributorId
                && priceList.SeriesId == seriesId
                && priceList.Id != excludedId
                && priceList.Status == PriceListStatus.Active
                && priceList.ValidFromUtc.HasValue
                && (!validUntilUtc.HasValue
                    || priceList.ValidFromUtc.Value < validUntilUtc.Value)
                && (!priceList.ValidUntilUtc.HasValue
                    || validFromUtc < priceList.ValidUntilUtc.Value),
            cancellationToken);
    }

    public async Task LockSeriesAsync(
        DistributorId distributorId,
        PriceListId seriesId,
        CancellationToken cancellationToken = default)
    {
        var connection = Context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = Context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            SELECT pg_advisory_xact_lock(
                hashtext(@distributor_id),
                hashtext(@series_id));
            """;

        AddParameter(command, "distributor_id", distributorId.Value.ToString());
        AddParameter(command, "series_id", seriesId.Value.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
