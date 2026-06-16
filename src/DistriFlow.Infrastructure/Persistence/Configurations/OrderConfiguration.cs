using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Orders;
using DistriFlow.Domain.PriceLists;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id)
            .HasConversion(
                id => id.Value,
                value => OrderId.From(value))
            .ValueGeneratedNever();

        builder.Property(order => order.DistributorId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value));

        builder.Property(order => order.CustomerId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => CustomerId.From(value));

        builder.Property(order => order.SalesRepId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => UserId.From(value));

        builder.Property(order => order.OrderNumber)
            .IsRequired();

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired();

        builder.Property(order => order.CreatedByUserId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => UserId.From(value));

        builder.Property(order => order.Origin)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(order => order.ConfirmedAtUtc)
            .IsRequired(false);

        builder.Property(order => order.ConfirmedByUserId)
            .IsRequired(false)
            .HasConversion(
                id => id!.Value.Value,
                value => UserId.From(value));

        builder.Property(order => order.CancelledAtUtc)
            .IsRequired(false);

        builder.Property(order => order.CancelledByUserId)
            .IsRequired(false)
            .HasConversion(
                id => id!.Value.Value,
                value => UserId.From(value));

        builder.Property(order => order.CancellationReason)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(order => order.RowVersion)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(order => order.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Ignore(order => order.Total);

        builder.HasIndex(order => order.DistributorId);
        builder.HasIndex(order => new { order.DistributorId, order.OrderNumber })
            .IsUnique()
            .HasDatabaseName("ix_orders_distributor_id_order_number");
        builder.HasIndex(order => new { order.DistributorId, order.CustomerId });
        builder.HasIndex(order => new { order.DistributorId, order.SalesRepId });

        builder.OwnsMany(order => order.OrderLines, lines =>
        {
            lines.ToTable("order_lines");

            lines.WithOwner()
                .HasForeignKey("OrderId");

            lines.Property<OrderId>("OrderId")
                .HasConversion(
                    id => id.Value,
                    value => OrderId.From(value))
                .HasColumnName("order_id");

            lines.Property(line => line.ProductId)
                .IsRequired()
                .HasConversion(
                    id => id.Value,
                    value => ProductId.From(value))
                .HasColumnName("product_id");

            lines.Property(line => line.ProductDescription)
                .IsRequired()
                .HasColumnName("product_description")
                .HasMaxLength(200);

            lines.Property(line => line.PriceListId)
                .IsRequired(false)
                .HasConversion(
                    id => id!.Value.Value,
                    value => PriceListId.From(value))
                .HasColumnName("price_list_id");

            lines.Property(line => line.PriceListVersion)
                .IsRequired(false)
                .HasColumnName("price_list_version");

            lines.Property(line => line.SourceUnitPrice)
                .IsRequired()
                .HasConversion(
                    price => price.Amount,
                    value => Money.Create(value))
                .HasColumnName("source_unit_price")
                .HasPrecision(18, 2);

            lines.Property(line => line.AppliedUnitPrice)
                .IsRequired()
                .HasConversion(
                    price => price.Amount,
                    value => Money.Create(value))
                .HasColumnName("applied_unit_price")
                .HasPrecision(18, 2);

            lines.Property(line => line.PriceOrigin)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnName("price_origin")
                .HasMaxLength(30);

            lines.Ignore(line => line.UnitPrice);
            lines.Ignore(line => line.Subtotal);

            lines.OwnsOne(line => line.Quantity, quantity =>
            {
                quantity.Property(value => value.Amount)
                    .IsRequired()
                    .HasColumnName("amount")
                    .HasPrecision(18, 3);

                quantity.Property(value => value.Unit)
                    .IsRequired()
                    .HasConversion(
                        unit => unit.Value,
                        value => SaleUnit.Create(value))
                    .HasColumnName("sale_unit")
                    .HasMaxLength(50);
            });

            lines.Navigation(line => line.Quantity)
                .IsRequired();

            lines.HasKey("OrderId", nameof(OrderLine.ProductId));
        });

        builder.Navigation(order => order.OrderLines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
