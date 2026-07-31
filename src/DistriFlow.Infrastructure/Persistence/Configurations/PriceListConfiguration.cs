using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.PriceLists.Entities;
using DistriFlow.Domain.PriceLists.ValueObjects;
using DistriFlow.Domain.Products.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("price_lists");

        builder.HasKey(priceList => priceList.Id);

        builder.Property(priceList => priceList.Id)
            .HasConversion(
                id => id.Value,
                value => PriceListId.From(value))
            .ValueGeneratedNever();

        builder.Property(priceList => priceList.DistributorId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value));

        builder.Property(priceList => priceList.SeriesId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => PriceListId.From(value));

        builder.Property(priceList => priceList.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(priceList => priceList.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(priceList => priceList.Version)
            .IsRequired();

        builder.Property(priceList => priceList.ValidFromUtc)
            .IsRequired(false);

        builder.Property(priceList => priceList.ValidUntilUtc)
            .IsRequired(false);

        builder.Property(priceList => priceList.ActivatedAtUtc)
            .IsRequired(false);

        builder.Property(priceList => priceList.DeactivatedAtUtc)
            .IsRequired(false);

        builder.Property(priceList => priceList.CreatedByUserId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => UserId.From(value));

        builder.Property(priceList => priceList.UpdatedByUserId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => UserId.From(value));

        builder.Property(priceList => priceList.RowVersion)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(priceList => priceList.DistributorId);
        builder.HasIndex(priceList => new
            {
                priceList.DistributorId,
                priceList.SeriesId,
                priceList.Version
            })
            .IsUnique()
            .HasDatabaseName(
                "ix_price_lists_distributor_id_series_id_version");
        builder.HasIndex(priceList => new
            {
                priceList.DistributorId,
                priceList.SeriesId,
                priceList.Status,
                priceList.ValidFromUtc,
                priceList.ValidUntilUtc
            })
            .HasDatabaseName("ix_price_lists_effective_version");

        builder.OwnsMany(priceList => priceList.ProductPrices, productPrices =>
        {
            productPrices.ToTable("price_list_product_prices");

            productPrices.WithOwner()
                .HasForeignKey("PriceListId");

            productPrices.Property<PriceListId>("PriceListId")
                .HasConversion(
                    id => id.Value,
                    value => PriceListId.From(value))
                .HasColumnName("price_list_id");

            productPrices.Property(productPrice => productPrice.ProductId)
                .IsRequired()
                .HasConversion(
                    id => id.Value,
                    value => ProductId.From(value))
                .HasColumnName("product_id");

            productPrices.Property(productPrice => productPrice.Price)
                .IsRequired()
                .HasConversion(
                    price => price.Amount,
                    value => Money.Create(value))
                .HasColumnName("price")
                .HasPrecision(18, 2);

            productPrices.HasKey("PriceListId", nameof(ProductPrice.ProductId));
        });

        builder.Navigation(priceList => priceList.ProductPrices)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
