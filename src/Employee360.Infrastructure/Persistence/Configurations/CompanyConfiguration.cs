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

        builder.Ignore(c => c.DomainEvents);
    }
}
