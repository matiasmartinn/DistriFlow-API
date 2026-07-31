using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Distributors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DistriFlow.Domain.Distributors.ValueObjects;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class DistributorConfiguration : IEntityTypeConfiguration<Distributor>
{
    public void Configure(EntityTypeBuilder<Distributor> builder)
    {
        builder.ToTable("distributors");

        builder.HasKey(distributor => distributor.Id);

        builder.Property(distributor => distributor.Id)
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value))
            .ValueGeneratedNever();

        builder.Property(distributor => distributor.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(distributor => distributor.Name)
            .IsUnique()
            .HasDatabaseName("ix_distributors_name");

        builder.Property(distributor => distributor.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.OwnsOne(distributor => distributor.Contact, contact =>
        {
            contact.Property(value => value.Email)
                .HasConversion(
                    email => email.Value,
                    value => Email.Create(value))
                .IsRequired()
                .HasColumnName("contact_email")
                .HasMaxLength(320);

            contact.Property(value => value.Phone)
                .HasConversion(
                    phone => phone.Value,
                    value => PhoneNumber.Create(value))
                .IsRequired()
                .HasColumnName("contact_phone")
                .HasMaxLength(50);

            contact.HasIndex(value => value.Email)
                .IsUnique()
                .HasDatabaseName("ix_distributors_contact_email");
        });

        builder.Navigation(distributor => distributor.Contact)
            .IsRequired();

        builder.OwnsOne(distributor => distributor.Address, address =>
        {
            address.Property(value => value.Street)
                .IsRequired()
                .HasColumnName("street")
                .HasMaxLength(200);

            address.Property(value => value.CityId)
                .IsRequired()
                .HasColumnName("city_id");

            address.Property(value => value.ZipCode)
                .HasColumnName("zip_code")
                .HasMaxLength(20);
        });
    }
}
