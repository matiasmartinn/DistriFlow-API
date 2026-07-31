using System.Data;
using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Stocks;
using DistriFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Products.ValueObjects;
using DistriFlow.Domain.Stocks.ValueObjects;

namespace DistriFlow.Infrastructure.Repositories;

public sealed class StockRepository
    : Repository<Stock, StockId>,
      IStockRepository
{
    public StockRepository(DistriFlowDbContext context)
        : base(context) { }

    public Task<Stock?> GetByProductIdAsync(
        DistributorId distributorId,
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            stock => stock.DistributorId == distributorId
                && stock.ProductId == productId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Stock>> GetByProductIdsAsync(
        DistributorId distributorId,
        IReadOnlyCollection<ProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(stock => stock.DistributorId == distributorId
                && productIds.Contains(stock.ProductId))
            .ToListAsync(cancellationToken);
    }

    public Task<StockChange?> TryDecreaseAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity quantity,
        bool allowNegative,
        CancellationToken cancellationToken = default)
    {
        return ExecuteChangeAsync(
            """
            UPDATE stocks
            SET available = available - @quantity,
                row_version = row_version + 1
            WHERE distributor_id = @distributor_id
              AND product_id = @product_id
              AND sale_unit = @sale_unit
              AND @quantity > 0
              AND (@allow_negative OR available >= @quantity)
            RETURNING available + @quantity, available, sale_unit, row_version;
            """,
            distributorId,
            productId,
            quantity,
            allowNegative,
            null,
            null,
            cancellationToken);
    }

    public Task<StockChange?> TryIncreaseAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity quantity,
        CancellationToken cancellationToken = default)
    {
        return ExecuteChangeAsync(
            """
            UPDATE stocks
            SET available = available + @quantity,
                row_version = row_version + 1
            WHERE distributor_id = @distributor_id
              AND product_id = @product_id
              AND sale_unit = @sale_unit
              AND @quantity > 0
            RETURNING available - @quantity, available, sale_unit, row_version;
            """,
            distributorId,
            productId,
            quantity,
            true,
            null,
            null,
            cancellationToken);
    }

    public Task<StockChange?> TryAdjustAsync(
        DistributorId distributorId,
        ProductId productId,
        Quantity expectedQuantity,
        Quantity resultingQuantity,
        long expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        return ExecuteChangeAsync(
            """
            UPDATE stocks
            SET available = @resulting_quantity,
                row_version = row_version + 1
            WHERE distributor_id = @distributor_id
              AND product_id = @product_id
              AND sale_unit = @sale_unit
              AND available = @quantity
              AND row_version = @expected_row_version
              AND available <> @resulting_quantity
            RETURNING @quantity, available, sale_unit, row_version;
            """,
            distributorId,
            productId,
            expectedQuantity,
            true,
            resultingQuantity.Amount,
            expectedRowVersion,
            cancellationToken);
    }

    private async Task<StockChange?> ExecuteChangeAsync(
        string commandText,
        DistributorId distributorId,
        ProductId productId,
        Quantity quantity,
        bool allowNegative,
        decimal? resultingQuantity,
        long? expectedRowVersion,
        CancellationToken cancellationToken)
    {
        var connection = Context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = Context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = commandText;

        AddParameter(command, "distributor_id", distributorId.Value);
        AddParameter(command, "product_id", productId.Value);
        AddParameter(command, "quantity", quantity.Amount);
        AddParameter(command, "sale_unit", quantity.Unit.Value);
        AddParameter(command, "allow_negative", allowNegative);

        if (resultingQuantity.HasValue)
            AddParameter(command, "resulting_quantity", resultingQuantity.Value);

        if (expectedRowVersion.HasValue)
            AddParameter(command, "expected_row_version", expectedRowVersion.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new StockChange(
            reader.GetDecimal(0),
            reader.GetDecimal(1),
            SaleUnit.Create(reader.GetString(2)),
            reader.GetInt64(3));
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
