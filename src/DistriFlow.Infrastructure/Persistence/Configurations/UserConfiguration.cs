using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DistriFlow.Domain.Distributors.ValueObjects;
using DistriFlow.Domain.Users.ValueObjects;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .ValueGeneratedNever();

        builder.Property(user => user.DistributorId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value));

        builder.Property(user => user.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .HasMaxLength(320);

        builder.Property(user => user.PasswordHash)
            .IsRequired(false)
            .HasConversion(
                passwordHash => passwordHash!.Value,
                value => PasswordHash.From(value))
            .HasMaxLength(500);

        builder.Property(user => user.Role)
            .IsRequired()
            .HasConversion(
                role => role.Value,
                value => Role.From(value))
            .HasMaxLength(30);

        builder.Property(user => user.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(user => user.CreatedByUserId)
            .IsRequired(false)
            .HasConversion(
                id => id!.Value.Value,
                value => UserId.From(value));

        builder.Property(user => user.LastLoginAtUtc)
            .IsRequired(false);

        builder.Property(user => user.DeactivatedAtUtc)
            .IsRequired(false);

        builder.Property(user => user.DeactivatedByUserId)
            .IsRequired(false)
            .HasConversion(
                id => id!.Value.Value,
                value => UserId.From(value));

        builder.Property(user => user.DeactivationReason)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.HasIndex(user => user.DistributorId);

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");
    }
}
