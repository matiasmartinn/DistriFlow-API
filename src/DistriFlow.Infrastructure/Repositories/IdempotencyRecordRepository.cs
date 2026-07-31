using System.Data;
using DistriFlow.Domain.Idempotency;
using DistriFlow.Domain.Distributors;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Idempotency.Enums;
using DistriFlow.Domain.Idempotency.ValueObjects;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class IdempotencyRecordRepository
    : Repository<IdempotencyRecord, IdempotencyRecordId>,
      IIdempotencyRecordRepository
{
    public IdempotencyRecordRepository(DistriFlowDbContext context)
        : base(context) { }

    public async Task<IdempotencyReservation> ReserveAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        var connection = Context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = Context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            INSERT INTO idempotency_records (
                id,
                distributor_id,
                created_by_user_id,
                key,
                operation,
                resource_id,
                created_at_utc)
            VALUES (
                @id,
                @distributor_id,
                @created_by_user_id,
                @key,
                @operation,
                @resource_id,
                @created_at_utc)
            ON CONFLICT (distributor_id, key) DO NOTHING
            RETURNING id;
            """;

        AddParameter(command, "id", record.Id.Value);
        AddParameter(command, "distributor_id", record.DistributorId.Value);
        AddParameter(command, "created_by_user_id", record.CreatedByUserId.Value);
        AddParameter(command, "key", record.Key);
        AddParameter(command, "operation", record.Operation.ToString());
        AddParameter(command, "resource_id", record.ResourceId);
        AddParameter(command, "created_at_utc", record.CreatedAtUtc);

        var inserted = await command.ExecuteScalarAsync(cancellationToken) is not null;

        if (inserted)
        {
            return new IdempotencyReservation(
                true,
                record.CreatedByUserId,
                record.Operation,
                record.ResourceId);
        }

        var existing = await DbSet
            .AsNoTracking()
            .FirstAsync(
                current => current.DistributorId == record.DistributorId
                    && current.Key == record.Key,
                cancellationToken);

        return new IdempotencyReservation(
            false,
            existing.CreatedByUserId,
            existing.Operation,
            existing.ResourceId);
    }

    public async Task<bool> TrySetResourceIdAsync(
        DistributorId distributorId,
        string key,
        IdempotentOperation operation,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await DbSet
            .Where(record => record.DistributorId == distributorId
                && record.Key == key
                && record.Operation == operation
                && record.ResourceId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    record => record.ResourceId,
                    resourceId),
                cancellationToken);

        return affectedRows == 1;
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
