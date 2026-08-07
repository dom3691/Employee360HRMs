using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Company"/>.</summary>
public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.RCNumber)
            .HasMaxLength(64);

        builder.Property(c => c.TIN)
            .HasMaxLength(32);

        builder.Property(c => c.Address)
            .HasMaxLength(512);

        builder.Property(c => c.LogoUrl)
            .HasMaxLength(2048);

        builder.Property(c => c.DefaultCurrency)
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("NGN");

        builder.Property(c => c.TradingName).HasMaxLength(256);
        builder.Property(c => c.LegalName).HasMaxLength(256);
        builder.Property(c => c.Industry).HasMaxLength(128);
        builder.Property(c => c.Website).HasMaxLength(512);
        builder.Property(c => c.Phone).HasMaxLength(32);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.StreetAddress).HasMaxLength(256);
        builder.Property(c => c.City).HasMaxLength(128);
        builder.Property(c => c.State).HasMaxLength(128);
        builder.Property(c => c.Country).HasMaxLength(128);
        builder.Property(c => c.WorkingCalendarJson);
        builder.Property(c => c.EmailConfigurationJson);

        builder.Ignore(c => c.DomainEvents);
    }
}
