using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Idempotency;
using DistriFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration
    : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .HasConversion(
                id => id.Value,
                value => IdempotencyRecordId.From(value))
            .ValueGeneratedNever();

        builder.Property(record => record.DistributorId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value));

        builder.Property(record => record.CreatedByUserId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => UserId.From(value));

        builder.Property(record => record.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(record => record.Operation)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(record => record.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(record => new { record.DistributorId, record.Key })
            .IsUnique()
            .HasDatabaseName("ix_idempotency_records_distributor_id_key");
    }
}
