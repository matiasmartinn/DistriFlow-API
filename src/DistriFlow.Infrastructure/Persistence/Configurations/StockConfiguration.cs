using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("stocks");

        builder.HasKey(stock => stock.Id);

        builder.Property(stock => stock.Id)
            .HasConversion(
                id => id.Value,
                value => StockId.From(value))
            .ValueGeneratedNever();

        builder.Property(stock => stock.DistributorId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value));

        builder.Property(stock => stock.ProductId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => ProductId.From(value));

        builder.HasIndex(stock => stock.DistributorId);

        builder.HasIndex(stock => new { stock.DistributorId, stock.ProductId })
            .IsUnique()
            .HasDatabaseName("ix_stocks_distributor_id_product_id");

        builder.OwnsOne(stock => stock.Available, available =>
        {
            available.Property(value => value.Amount)
                .IsRequired()
                .HasColumnName("available")
                .HasPrecision(18, 3);

            available.Property(value => value.Unit)
                .IsRequired()
                .HasConversion(
                    unit => unit.Value,
                    value => SaleUnit.Create(value))
                .HasColumnName("sale_unit")
                .HasMaxLength(50);
        });

        builder.Navigation(stock => stock.Available)
            .IsRequired();
    }
}
