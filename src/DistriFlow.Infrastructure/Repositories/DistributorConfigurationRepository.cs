using System.Data;
using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.Distributors;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class DistributorConfigurationRepository
    : Repository<DistributorConfiguration, DistributorId>,
      IDistributorConfigurationRepository
{
    public DistributorConfigurationRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<DistributorConfiguration?> GetByDistributorIdAsync(
        DistributorId distributorId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.AsNoTracking().FirstOrDefaultAsync(
            configuration => configuration.DistributorId == distributorId,
            cancellationToken);
    }

    public async Task<ReservedOrderNumber?> ReserveNextOrderNumberAsync(
        DistributorId distributorId,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var connection = Context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = Context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            UPDATE distributor_configurations
            SET next_order_number = next_order_number + 1,
                updated_at_utc = @updated_at_utc
            WHERE distributor_id = @distributor_id
            RETURNING order_number_prefix, next_order_number - 1;
            """;

        var distributorParameter = command.CreateParameter();
        distributorParameter.ParameterName = "distributor_id";
        distributorParameter.Value = distributorId.Value;
        command.Parameters.Add(distributorParameter);

        var updatedAtParameter = command.CreateParameter();
        updatedAtParameter.ParameterName = "updated_at_utc";
        updatedAtParameter.Value = updatedAtUtc;
        command.Parameters.Add(updatedAtParameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var prefix = reader.IsDBNull(0) ? null : reader.GetString(0);
        var number = reader.GetInt64(1);
        var formattedNumber = number.ToString("D6");

        return new ReservedOrderNumber(
            string.IsNullOrWhiteSpace(prefix)
                ? formattedNumber
                : $"{prefix}-{formattedNumber}");
    }

    public async Task<bool> TryUpdateAsync(
        DistributorConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await DbSet
            .Where(current =>
                current.DistributorId == configuration.DistributorId
                && current.NextOrderNumber <= configuration.NextOrderNumber)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        current => current.StockPolicy,
                        configuration.StockPolicy)
                    .SetProperty(
                        current => current.PriceFallbackPolicy,
                        configuration.PriceFallbackPolicy)
                    .SetProperty(
                        current => current.OrderNumberPrefix,
                        configuration.OrderNumberPrefix)
                    .SetProperty(
                        current => current.NextOrderNumber,
                        configuration.NextOrderNumber)
                    .SetProperty(
                        current => current.TimeZone,
                        configuration.TimeZone)
                    .SetProperty(
                        current => current.UpdatedAtUtc,
                        configuration.UpdatedAtUtc),
                cancellationToken);

        return affectedRows == 1;
    }
}
