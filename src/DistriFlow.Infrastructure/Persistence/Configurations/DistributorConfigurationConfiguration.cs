using DistriFlow.Domain.DistributorConfigurations;
using DistriFlow.Domain.Distributors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class DistributorConfigurationConfiguration
    : IEntityTypeConfiguration<
        DistriFlow.Domain.DistributorConfigurations.DistributorConfiguration>
{
    public void Configure(
        EntityTypeBuilder<
            DistriFlow.Domain.DistributorConfigurations.DistributorConfiguration> builder)
    {
        builder.ToTable("distributor_configurations");

        builder.HasKey(configuration => configuration.DistributorId);

        builder.Property(configuration => configuration.DistributorId)
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value))
            .ValueGeneratedNever();

        builder.Property(configuration => configuration.StockPolicy)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(configuration => configuration.PriceFallbackPolicy)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(configuration => configuration.OrderNumberPrefix)
            .IsRequired(false);

        builder.Property(configuration => configuration.NextOrderNumber)
            .IsRequired();

        builder.Property(configuration => configuration.TimeZone)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(configuration => configuration.CreatedAtUtc)
            .IsRequired();

        builder.Property(configuration => configuration.UpdatedAtUtc)
            .IsRequired();
    }
}
