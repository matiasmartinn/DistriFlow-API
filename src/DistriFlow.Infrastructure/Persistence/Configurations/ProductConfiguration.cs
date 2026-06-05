using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .HasConversion(
                id => id.Value,
                value => ProductId.From(value))
            .ValueGeneratedNever();

        builder.Property(product => product.DistributorId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value));

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(product => product.Sku)
            .IsRequired(false)
            .HasConversion(
                sku => sku!.Value,
                value => Sku.Create(value))
            .HasMaxLength(100);

        builder.Property(product => product.SaleUnit)
            .IsRequired()
            .HasConversion(
                saleUnit => saleUnit.Value,
                value => SaleUnit.Create(value))
            .HasMaxLength(50);

        builder.Property(product => product.BasePrice)
            .IsRequired()
            .HasConversion(
                price => price.Amount,
                value => Money.Create(value))
            .HasPrecision(18, 2);

        builder.Property(product => product.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(product => product.DistributorId);

        builder.HasIndex(product => new { product.DistributorId, product.Sku })
            .IsUnique()
            .HasFilter("sku IS NOT NULL")
            .HasDatabaseName("ix_products_distributor_id_sku");
    }
}
