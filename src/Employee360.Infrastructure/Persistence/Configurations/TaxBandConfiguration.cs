using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

public sealed class TaxBandConfiguration : IEntityTypeConfiguration<TaxBand>
{
    public void Configure(EntityTypeBuilder<TaxBand> builder)
    {
        builder.ToTable("TaxBands");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.UpperBoundAnnual).HasPrecision(18, 2);
        builder.Property(b => b.Rate).HasPrecision(8, 6);
        builder.HasIndex(b => new { b.TaxYear, b.BandOrder })
            .IsUnique()
            .HasDatabaseName("IX_TaxBands_TaxYear_BandOrder");
        builder.Ignore(b => b.DomainEvents);
    }
}
