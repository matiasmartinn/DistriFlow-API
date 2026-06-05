using DistriFlow.Domain.Common.ValueObjects;
using DistriFlow.Domain.Customers;
using DistriFlow.Domain.Distributors;
using DistriFlow.Domain.PriceLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistriFlow.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Id)
            .HasConversion(
                id => id.Value,
                value => CustomerId.From(value))
            .ValueGeneratedNever();

        builder.Property(customer => customer.DistributorId)
            .IsRequired()
            .HasConversion(
                id => id.Value,
                value => DistributorId.From(value));

        builder.Property(customer => customer.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(customer => customer.PriceListId)
            .IsRequired(false)
            .HasConversion(
                id => id!.Value.Value,
                value => PriceListId.From(value));

        builder.Property(customer => customer.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(customer => customer.DistributorId);
        builder.HasIndex(customer => new { customer.DistributorId, customer.PriceListId });

        builder.OwnsOne(customer => customer.Contact, contact =>
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
        });

        builder.Navigation(customer => customer.Contact)
            .IsRequired();

        builder.OwnsOne(customer => customer.Address, address =>
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
