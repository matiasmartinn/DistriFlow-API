using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Products;
using DistriFlow.Domain.StockMovements;
using DistriFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration
    : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Id)
            .HasConversion(
                id => id.Value,
                value => StockMovementId.From(value))
            .ValueGeneratedNever();

        builder.Property(movement => movement.DistributorId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value));

        builder.Property(movement => movement.ProductId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => ProductId.From(value));

        builder.Property(movement => movement.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(movement => movement.Quantity)
            .IsRequired()
            .HasPrecision(18, 3);

        builder.Property(movement => movement.PreviousQuantity)
            .IsRequired()
            .HasPrecision(18, 3);

        builder.Property(movement => movement.ResultingQuantity)
            .IsRequired()
            .HasPrecision(18, 3);

        builder.Property(movement => movement.SaleUnit)
            .IsRequired()
            .HasConversion(
                unit => unit.Value,
                value => SaleUnit.Create(value))
            .HasMaxLength(50);

        builder.Property(movement => movement.ReferenceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(movement => movement.Reason)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(movement => movement.CreatedAtUtc)
            .IsRequired();

        builder.Property(movement => movement.CreatedByUserId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => UserId.From(value));

        builder.HasIndex(movement => movement.DistributorId);
        builder.HasIndex(movement => new
        {
            movement.DistributorId,
            movement.ProductId,
            movement.CreatedAtUtc
        });
        builder.HasIndex(movement => new
        {
            movement.DistributorId,
            movement.ReferenceType,
            movement.ReferenceId
        });
        builder.HasIndex(movement => new
        {
            movement.DistributorId,
            movement.ProductId,
            movement.Type,
            movement.ReferenceType,
            movement.ReferenceId
        })
            .IsUnique()
            .HasFilter("reference_id IS NOT NULL")
            .HasDatabaseName("ix_stock_movements_unique_business_reference");
    }
}
